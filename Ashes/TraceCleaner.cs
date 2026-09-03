// TraceCleaner.cs
//
// DriveCleanup 가 커버하지 못하는 다섯 가지 흔적을 처리합니다.
//
//   1) 이벤트 로그 (Partition/Diagnostic, Storsvc/Diagnostic, Kernel-PnP,
//      System 의 WPD 관련 이벤트) — 채널을 wevtutil cl 로 비우면 System 로그에
//      EventID 104(로그 지움) 가 반드시 찍힙니다. 그걸 피하려고 채널 최대 크기를
//      순간적으로 축소했다가 복원해서, Windows 가 오래된 이벤트를 자동으로
//      밀어내게 유도합니다. 자동 회전은 감사 로그를 남기지 않습니다.
//
//   2) setupapi.dev.log (및 회전 파일 setupapi.dev.YYYYMMDD_*.log) — 텍스트 파일
//      이라 통째로 지우면 다음 세션에서 새 파일이 생기며 지워진 사실이 티가 납니다.
//      파일을 지우지 말고 "USBSTOR / WPDBUSENUM / STORAGE\\VOLUME 인용이 있는
//      섹션" 만 잘라내고 원래 타임스탬프를 복원합니다.
//
//   3) 현재 연결된 SATECHI/ASUS 인클로저 등 present 상태라 DriveCleanup 이
//      건너뛴 USB 저장장치 항목 — pnputil /remove-device 로 인스턴스 ID 지정해
//      강제 제거. 삭제 후 다시 꽂으면 새 인스턴스로 재등록되는데 저장장치의
//      경우 시리얼이 같으면 같은 인스턴스로 복원되므로 검증 시점에는 사라진
//      상태여야 합니다.
//
//   4) VolumeInfoCache (SOFTWARE 하이브의
//      Microsoft\\Windows Search\\VolumeInfoCache\\<letter>) 의 외장 드라이브
//      항목 — SYSTEM 컨텍스트가 필요해서 psexec 없이 SetNamedSecurityInfo 로
//      권한을 잠깐 잡고 지웁니다.
//
//   5) 사용자 하이브의 MountPoints2 잔여 GUID — 로그인 중인 사용자는 라이브
//      HKU 로, 아니면 오프라인 NTUSER.DAT 로 접근해 삭제.
//
// 실행 순서: 이벤트 로그 축소 → setupapi 편집 → pnputil → VolumeInfoCache →
// MountPoints2. 이벤트 로그를 가장 먼저 손대는 이유는, 이후 작업들이 새 이벤트를
// 유발할 수 있어서 그 이벤트들도 함께 밀려나도록 하기 위함입니다.
//
// 관리자 권한 필수. app.manifest 가 이미 requireAdministrator 라 별도 처리 없음.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Ashes
{
    /// <summary>
    /// DriveCleanup 미커버 영역을 담당하는 클래스입니다.
    /// UI 는 <see cref="RunAsync"/> 만 호출하면 되고, 진행 상황은
    /// <paramref name="onOutput"/> 콜백으로 흘러옵니다.
    /// </summary>
    public static class TraceCleaner
    {
        public sealed class Options
        {
            /// <summary>true 면 실제 삭제 없이 무엇을 할지만 보고합니다.</summary>
            public bool DryRun { get; set; }

            /// <summary>이벤트 로그 축소로 밀어낼 채널 목록.</summary>
            public bool EventLogs { get; set; } = true;

            /// <summary>setupapi.dev.log 편집.</summary>
            public bool SetupApi { get; set; } = true;

            /// <summary>연결된 채로 남은 USB 저장장치 강제 제거.</summary>
            public bool ConnectedDevices { get; set; } = true;

            /// <summary>Windows Search VolumeInfoCache 정리.</summary>
            public bool VolumeInfoCache { get; set; } = true;

            /// <summary>MountPoints2 잔여 GUID 정리.</summary>
            public bool MountPoints2 { get; set; } = true;

            /// <summary>Enum\USB / DeviceContainers 의 저장장치 잔여 노드 정리
            /// (DriveCleanup 이 present 라서 못 지운 것, pnputil 이 연결 끊겨 못 지운 것).</summary>
            public bool RegistryUsb { get; set; } = true;

            /// <summary>Amcache.hve 의 저장장치 기록 정리.</summary>
            public bool Amcache { get; set; } = true;

            /// <summary>점프리스트(AutomaticDestinations/CustomDestinations) 중
            /// 외장 볼륨을 참조하는 파일 삭제.</summary>
            public bool JumpLists { get; set; } = true;

            /// <summary>Prefetch 전체 삭제. 외장 볼륨 참조(.pf)와 삭제 도구 실행 흔적을
            /// 모두 제거한다. 단, 이후 프로그램 실행 시 다시 생성된다.</summary>
            public bool Prefetch { get; set; } = true;

            /// <summary>Shellbags 에서 외장 드라이브(E:/F: 등) 노드가 발견되면, 그
            /// 노드가 속한 "내 PC" 컨테이너 하위를 초기화한다.</summary>
            public bool Shellbags { get; set; } = true;
        }

        public sealed class Result
        {
            public int EventChannelsProcessed;
            public int SetupApiSectionsRemoved;
            public int DevicesRemoved;
            public int VolumeCacheEntriesRemoved;
            public int MountPointsRemoved;
            public int RegistryUsbRemoved;
            public int AmcacheRemoved;
            public int JumpListsRemoved;
            public int PrefetchRemoved;
            public int ShellbagsRemoved;
            public List<string> Warnings { get; } = new();
        }

        public static async Task<Result> RunAsync(
            Options opt,
            Action<string> onOutput,
            CancellationToken ct = default)
        {
            var r = new Result();
            void Log(string s) => onOutput?.Invoke(s);

            Log("== Ashes 확장 정리 시작 =="
                + (opt.DryRun ? " (미리보기)" : ""));

            // 이벤트 로그를 가장 먼저 처리해서, 이후 단계들이 발생시키는
            // 새 이벤트도 함께 밀려나도록 한다.
            if (opt.EventLogs)
            {
                Log("");
                Log("[1/10] 이벤트 로그 정리 (채널 clear)");
                try { r.EventChannelsProcessed = await EventLogTrick.RunAsync(opt.DryRun, Log, ct); }
                catch (Exception ex) { r.Warnings.Add("EventLogs: " + ex.Message); Log("   [실패] " + ex.Message); }
            }

            if (opt.SetupApi)
            {
                Log("");
                Log("[2/10] setupapi 로그 편집");
                try { r.SetupApiSectionsRemoved = SetupApiEditor.Run(opt.DryRun, Log); }
                catch (Exception ex) { r.Warnings.Add("SetupApi: " + ex.Message); Log("   [실패] " + ex.Message); }
            }

            if (opt.ConnectedDevices)
            {
                Log("");
                Log("[3/10] 연결된 USB 저장장치 강제 제거 (pnputil)");
                try { r.DevicesRemoved = await ConnectedDeviceRemover.RunAsync(opt.DryRun, Log, ct); }
                catch (Exception ex) { r.Warnings.Add("ConnectedDevices: " + ex.Message); Log("   [실패] " + ex.Message); }
            }

            if (opt.VolumeInfoCache)
            {
                Log("");
                Log("[4/10] Windows Search VolumeInfoCache 정리");
                try { r.VolumeCacheEntriesRemoved = VolumeCacheCleaner.Run(opt.DryRun, Log); }
                catch (Exception ex) { r.Warnings.Add("VolumeInfoCache: " + ex.Message); Log("   [실패] " + ex.Message); }
            }

            if (opt.MountPoints2)
            {
                Log("");
                Log("[5/10] MountPoints2 잔여 GUID 정리");
                try { r.MountPointsRemoved = MountPointsCleaner.Run(opt.DryRun, Log); }
                catch (Exception ex) { r.Warnings.Add("MountPoints2: " + ex.Message); Log("   [실패] " + ex.Message); }
            }

            if (opt.RegistryUsb)
            {
                Log("");
                Log("[6/10] Enum\\USB / DeviceContainers 저장장치 잔여 노드 정리");
                try { r.RegistryUsbRemoved = RegistryUsbCleaner.Run(opt.DryRun, Log); }
                catch (Exception ex) { r.Warnings.Add("RegistryUsb: " + ex.Message); Log("   [실패] " + ex.Message); }
            }

            if (opt.Amcache)
            {
                Log("");
                Log("[7/10] Amcache 저장장치 기록 정리");
                try { r.AmcacheRemoved = AmcacheCleaner.Run(opt.DryRun, Log); }
                catch (Exception ex) { r.Warnings.Add("Amcache: " + ex.Message); Log("   [실패] " + ex.Message); }
            }

            if (opt.JumpLists)
            {
                Log("");
                Log("[8/10] 점프리스트 외장 볼륨 참조 파일 삭제");
                try { r.JumpListsRemoved = JumpListCleaner.Run(opt.DryRun, Log); }
                catch (Exception ex) { r.Warnings.Add("JumpLists: " + ex.Message); Log("   [실패] " + ex.Message); }
            }

            if (opt.Prefetch)
            {
                Log("");
                Log("[9/10] Prefetch 전체 삭제");
                try { r.PrefetchRemoved = PrefetchCleaner.Run(opt.DryRun, Log); }
                catch (Exception ex) { r.Warnings.Add("Prefetch: " + ex.Message); Log("   [실패] " + ex.Message); }
            }

            if (opt.Shellbags)
            {
                Log("");
                Log("[10/10] Shellbags 외장 드라이브 컨테이너 초기화");
                try { r.ShellbagsRemoved = ShellbagsCleaner.Run(opt.DryRun, Log); }
                catch (Exception ex) { r.Warnings.Add("Shellbags: " + ex.Message); Log("   [실패] " + ex.Message); }
            }

            Log("");
            Log("== 완료 ==");
            Log($"   이벤트 채널 {r.EventChannelsProcessed}개, setupapi 섹션 {r.SetupApiSectionsRemoved}개,"
                + $" 장치 {r.DevicesRemoved}개, VolumeInfoCache {r.VolumeCacheEntriesRemoved}개,"
                + $" MountPoints2 {r.MountPointsRemoved}개, Enum\\USB {r.RegistryUsbRemoved}개,"
                + $" Amcache {r.AmcacheRemoved}개, 점프리스트 {r.JumpListsRemoved}개,"
                + $" Prefetch {r.PrefetchRemoved}개, Shellbags {r.ShellbagsRemoved}개");
            if (r.Warnings.Count > 0)
            {
                Log("   경고:");
                foreach (var w in r.Warnings) Log("     · " + w);
            }
            return r;
        }

        // ═══════════════════════════════════════════════════════════════════
        // 1) 이벤트 로그: 채널 최대 크기를 순간적으로 축소해서 오래된 이벤트를
        //    자동 회전시킨다. wevtutil cl 과 달리 EventID 104 를 안 남긴다.
        // ═══════════════════════════════════════════════════════════════════

        static class EventLogTrick
        {
            // 축소 대상 채널. Partition/Diagnostic·Storsvc·Kernel-PnP 는 진단
            // 로그라 통째로 회전시켜도 실무 영향 없음. System 은 회전 방식으로
            // 부분 밀어내기만 시도 (모든 이벤트가 밀리면 안 되므로 최소 크기를
            // 크게 잡음).
            //
            // [설계 변경] 채널 크기 축소 트릭은 이 환경에서 무효였다:
            //  - /ms: 로 최대 크기를 줄여도 회전은 "다음 이벤트 기록 시점"에만
            //    일어나므로 기존 이벤트가 즉시 밀리지 않는다.
            //  - 줄인 직후 원복하면 회전할 틈 자체가 없다.
            //  - System 등 핵심 채널은 /ms: 변경이 rc=87 로 거부된다.
            // 따라서 wevtutil cl 로 채널을 통째로 비운다. 이렇게 하면 System 로그에
            // EventID 104(로그 지움)가 남지만, 위협 모델상 "언제 정리했는가"는
            // 노출돼도 무방하다고 판단하여 확실한 제거를 택한다.
            static readonly string[] Targets =
            {
                "Microsoft-Windows-Partition/Diagnostic",
                "Microsoft-Windows-Storsvc/Diagnostic",
                "Microsoft-Windows-Kernel-PnP/Configuration",
                "Microsoft-Windows-Kernel-PnP/Device Management",
                "Microsoft-Windows-DriverFrameworks-UserMode/Operational",
                "Microsoft-Windows-WPD-MTPClassDriver/Operational",
                "Microsoft-Windows-Storage-ClassPnP/Operational",
                "Microsoft-Windows-Ntfs/Operational",
                // System 은 통째로 clear. 부팅/서비스 등 정상 이벤트도 함께 사라진다.
                "System",
            };

            public static async Task<int> RunAsync(bool dryRun, Action<string> log, CancellationToken ct)
            {
                int processed = 0;
                foreach (var channel in Targets)
                {
                    ct.ThrowIfCancellationRequested();

                    // 채널 존재 여부 확인 (gl 실패 = 채널 없음)
                    var (glRc, _, _) = await Proc.RunAsync("wevtutil.exe", $"gl \"{channel}\"", ct);
                    if (glRc != 0) { log($"   · {channel}: 채널 없음, skip"); continue; }

                    if (dryRun)
                    {
                        // 현재 이벤트 수를 세서 미리보기에 표시.
                        long count = await CountAsync(channel, ct);
                        log($"   · {channel}: {count}건 → clear 예정");
                        processed++;
                        continue;
                    }

                    var (rc, _, err) = await Proc.RunAsync("wevtutil.exe", $"cl \"{channel}\"", ct);
                    if (rc == 0) { log($"   · {channel}: clear 완료"); processed++; }
                    else log($"   · {channel}: clear 실패 (rc={rc}) {err.Trim()}");
                }
                return processed;
            }

            static async Task<long> CountAsync(string channel, CancellationToken ct)
            {
                // wevtutil qe ... /c:1 은 개수를 안 주므로 gli(로그 정보)에서 numberOfLogRecords 사용.
                var (rc, stdout, _) = await Proc.RunAsync("wevtutil.exe", $"gli \"{channel}\"", ct);
                if (rc != 0) return -1;
                foreach (var line in stdout.Split('\n'))
                {
                    var t = line.Trim();
                    if (t.StartsWith("numberOfLogRecords:", StringComparison.OrdinalIgnoreCase)
                        && long.TryParse(t[(t.IndexOf(':') + 1)..].Trim(), out var n))
                        return n;
                }
                return -1;
            }

        }

        // ═══════════════════════════════════════════════════════════════════
        // 2) setupapi.dev.log — 대상 섹션만 잘라내고 파일 타임스탬프 복원
        // ═══════════════════════════════════════════════════════════════════

        static class SetupApiEditor
        {
            static readonly Regex SectionStart = new(@"^>>>\s+\[", RegexOptions.Compiled);
            static readonly Regex SectionEnd   = new(@"^<<<\s+", RegexOptions.Compiled);

            // 저장장치임이 명확한 문자열. 이런 섹션은 무조건 대상이고, 동시에
            // 여기서 저장장치의 VID/PID 를 "자동 수집" 하는 근거가 된다.
            static readonly Regex Target = new(
                @"USBSTOR|WPDBUSENUM|STORAGE\\VOLUME|_\?\?_USBSTOR|UASPSTOR",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // 텍스트에서 VID/PID 쌍을 뽑는 정규식. USBSTOR 인스턴스 문자열
            // (예: _??_USBSTOR#Disk&Ven__USB...#... 안에 직접 안 나올 수 있으나,
            //  같은 섹션의 부모 참조나 다른 섹션에서 USB\VID_xxxx&PID_xxxx 로 나온다)
            static readonly Regex VidPid = new(
                @"VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // USB 인스턴스 상위 노드 헤더인지 (USB\VID_...&PID_...)
            static readonly Regex UsbNode = new(
                @"USB\\VID_[0-9A-Fa-f]{4}&PID_[0-9A-Fa-f]{4}",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // UASP 외장 인클로저는 USB 가 아니라 SCSI\DISK&VEN_...&PROD_... 로 열거된다.
            // (조사 결과: 외장 SSD 들이 SCSI\DISK 로 남음.) setupapi 의 "Delete Device"
            // 섹션에 SCSI\DISK 헤더가 있으면 그건 제거된 적 있는 디스크이므로 외장으로
            // 본다. 내장 시스템 디스크는 제거되지 않아 Delete Device 로 안 남는다.
            // 그래도 안전을 위해 내장 컨트롤러 벤더(NVMe 등)는 명시적으로 제외한다.
            static readonly Regex ScsiDisk = new(
                @"SCSI\\DISK&VEN_([^&]*)&PROD_",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            static readonly Regex ScsiInternalVendor = new(
                @"VEN_(NVME|SAMSUNG|INTEL|WDC|SK_?HYNIX|MICRON|KIOXIA|SEAGATE|TOSHIBA)\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            static readonly Regex DeleteDeviceHeader = new(
                @"^>>>\s+\[Delete Device",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            public static int Run(bool dryRun, Action<string> log)
            {
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                var files = new List<string>();
                foreach (var dir in new[] { Path.Combine(winDir, "INF"), winDir })
                {
                    if (!Directory.Exists(dir)) continue;
                    try { files.AddRange(Directory.EnumerateFiles(dir, "setupapi*.log")); } catch { }
                }
                if (files.Count == 0) { log("   · setupapi 로그 파일 없음"); return 0; }

                // ── 1패스: 저장장치 VID/PID 집합을 구성 ──
                // (a) 로그 안: USBSTOR/WPDBUSENUM/STORAGE\VOLUME 이 등장하는 줄의 VID/PID
                // (b) 시스템: 현재/과거 저장장치로 등록된 VID/PID (StorageDeviceOracle)
                // 둘을 합쳐서, 로그에 설치 기록이 이미 지워진 장치도 시스템 근거로 잡는다.
                var storageVidPid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in files)
                {
                    string[] lines;
                    try { lines = File.ReadAllLines(file, Encoding.UTF8); } catch { continue; }
                    foreach (var line in lines)
                    {
                        if (!Target.IsMatch(line)) continue;
                        foreach (Match m in VidPid.Matches(line))
                            storageVidPid.Add($"{m.Groups[1].Value}:{m.Groups[2].Value}".ToUpperInvariant());
                    }
                }
                // 시스템 근거 병합 (usbflags / 레지스트리 기반, 하드코딩 아님)
                foreach (var vp in StorageDeviceOracle.Instance.StorageVidPids)
                    storageVidPid.Add(vp);

                if (storageVidPid.Count > 0)
                    log($"   · 저장장치 VID/PID 집합({storageVidPid.Count}): {string.Join(", ", storageVidPid)}");

                // ── 2패스: 섹션 단위로 제거 ──
                int totalRemoved = 0;
                foreach (var file in files)
                {
                    int removed = ProcessFile(file, storageVidPid, dryRun, log);
                    totalRemoved += removed;
                }
                return totalRemoved;
            }

            // 한 줄에 저장장치 VID/PID 가 들어있는지 (집합 기준 + 시리얼 휴리스틱)
            static bool MatchesStorageVidPid(string line, HashSet<string> storageVidPid)
            {
                foreach (Match m in VidPid.Matches(line))
                {
                    string key = $"{m.Groups[1].Value}:{m.Groups[2].Value}".ToUpperInvariant();
                    if (storageVidPid.Contains(key)) return true;
                }
                // vmusb 로 감싼 헤더는 VID/PID 가 VMware(0E0F) 라 위에서 안 걸린다.
                // 헤더 시리얼이 USBSTOR 로 확인된 저장장치 시리얼과 같으면 대상.
                var serials = StorageDeviceOracle.Instance.StorageSerials;
                if (serials.Count > 0)
                {
                    var hm = Regex.Match(line, @"\\([0-9A-Za-z]{6,})[\]\\#]");
                    if (hm.Success && serials.Contains(hm.Groups[1].Value)) return true;
                }
                // 집합에 근거가 없더라도, 헤더 시리얼이 저장장치 특유의 형태면 대상.
                // (USB Mass Storage 는 iSerialNumber 를 필수로 노출 → 12자+ 영숫자
                //  또는 MSFT30… 같은 브리지 칩 시리얼, 볼륨 GUID 형태)
                return StorageSerialHeuristic.LooksLikeStorageSerial(line);
            }

            // SCSI\DISK 외장(UASP) 인클로저의 Delete Device 헤더인지.
            // 내장 시스템 디스크(NVMe/Samsung 등)는 제외한다.
            static bool IsExternalScsiDelete(string headerLine)
            {
                if (!DeleteDeviceHeader.IsMatch(headerLine)) return false;
                if (!ScsiDisk.IsMatch(headerLine)) return false;
                if (ScsiInternalVendor.IsMatch(headerLine)) return false; // 내장 디스크 제외
                return true;
            }

            static int ProcessFile(string path, HashSet<string> storageVidPid, bool dryRun, Action<string> log)
            {
                string[] lines;
                DateTime origCreation, origWrite, origAccess;
                FileAttributes origAttrs;
                try
                {
                    origCreation = File.GetCreationTime(path);
                    origWrite    = File.GetLastWriteTime(path);
                    origAccess   = File.GetLastAccessTime(path);
                    origAttrs    = File.GetAttributes(path);
                    lines        = File.ReadAllLines(path, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    log($"   · {Path.GetFileName(path)}: 읽기 실패 {ex.Message}");
                    return 0;
                }

                var kept = new List<string>(lines.Length);
                int removedSections = 0;
                int i = 0;

                // 파일 상단의 헤더(로그 시작 시각 등) 는 첫 섹션 헤더 전까지 그대로 유지.
                while (i < lines.Length && !SectionStart.IsMatch(lines[i]))
                { kept.Add(lines[i]); i++; }

                while (i < lines.Length)
                {
                    if (!SectionStart.IsMatch(lines[i])) { kept.Add(lines[i]); i++; continue; }

                    // 섹션 헤더 발견. 끝을 찾는다.
                    int start = i;
                    int end = start;
                    while (end < lines.Length)
                    {
                        if (end > start && SectionStart.IsMatch(lines[end])) break;
                        if (SectionEnd.IsMatch(lines[end])) { end++; break; }
                        end++;
                    }
                    // [start, end) 가 한 섹션.
                    // 헤더에 명확한 저장장치 문자열(USBSTOR 등)이 있으면 대상.
                    // 그렇지 않아도, 헤더가 USB 상위 노드(USB\VID_...)이고 그 VID/PID 가
                    // 1패스에서 저장장치로 확인된 것이면 대상. 본문에 USBSTOR 계열이
                    // 인용된 경우도 대상. 그리고 SCSI\DISK 외장(UASP) Delete Device 도 대상.
                    bool hitInHeader = Target.IsMatch(lines[start])
                        || (UsbNode.IsMatch(lines[start]) && MatchesStorageVidPid(lines[start], storageVidPid))
                        || IsExternalScsiDelete(lines[start]);
                    bool hitInBody = false;
                    if (!hitInHeader)
                    {
                        for (int k = start + 1; k < end; k++)
                        {
                            if (Target.IsMatch(lines[k])) { hitInBody = true; break; }
                        }
                    }

                    if (hitInHeader || hitInBody)
                    {
                        removedSections++;
                    }
                    else
                    {
                        for (int k = start; k < end; k++) kept.Add(lines[k]);
                    }
                    i = end;
                }

                if (removedSections == 0)
                {
                    log($"   · {Path.GetFileName(path)}: 대상 섹션 없음");
                    return 0;
                }

                log($"   · {Path.GetFileName(path)}: 섹션 {removedSections}개 제거");

                if (dryRun) return removedSections;

                try
                {
                    // 읽기 전용이면 잠깐 해제
                    if ((origAttrs & FileAttributes.ReadOnly) != 0)
                        File.SetAttributes(path, origAttrs & ~FileAttributes.ReadOnly);

                    // 원자적 교체: 임시 파일에 쓴 뒤 이동.
                    string tmp = path + ".ashes.tmp";
                    File.WriteAllLines(tmp, kept, new UTF8Encoding(false));
                    File.Delete(path);
                    File.Move(tmp, path);

                    // 파일 타임스탬프 복원 — "최근에 편집됨" 흔적을 안 남긴다.
                    File.SetCreationTime(path, origCreation);
                    File.SetLastWriteTime(path, origWrite);
                    File.SetLastAccessTime(path, origAccess);
                    File.SetAttributes(path, origAttrs);
                }
                catch (Exception ex)
                {
                    log($"       쓰기 실패: {ex.Message}");
                }
                return removedSections;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 3) 현재 연결된 USB 저장장치를 pnputil 로 강제 제거
        // ═══════════════════════════════════════════════════════════════════

        static class ConnectedDeviceRemover
        {
            // USB 인스턴스 ID 를 뽑는 정규식. pnputil 출력 라벨 텍스트에 의존하지
            // 않고 값 형태로 찾는다.
            static readonly Regex InstanceLine = new(
                @"(USB\\VID_[0-9A-Fa-f]{4}&PID_[0-9A-Fa-f]{4}[^\s]*)",
                RegexOptions.Compiled);

            // 저장장치 관련 클래스 GUID (하드코딩이지만 이건 Windows 고정 상수라
            // PC/장치별로 바뀌지 않는다 — 장치 식별자가 아니라 장치 "종류"다).
            static readonly string[] StorageClassGuids =
            {
                "{4d36e967-e325-11ce-bfc1-08002be10318}", // DiskDrive
                "{eec5ad98-8080-425f-922a-dabf3de3f69a}", // WPD
                "{71a27cdd-812a-11d0-bec7-08002be2092f}", // Volume
                "{4d36e97b-e325-11ce-bfc1-08002be10318}", // SCSIAdapter (UASP 브리지)
                "{533c5b84-ec70-11d2-9505-00c04f79deaf}", // VolumeSnapshot (보조)
            };

            // pnputil /enum-devices 한 장치 블록에서 클래스 GUID 를 뽑는 정규식.
            static readonly Regex ClassGuidLine = new(
                @"\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}",
                RegexOptions.Compiled);

            public static async Task<int> RunAsync(bool dryRun, Action<string> log, CancellationToken ct)
            {
                // 1) 연결된 전체 장치 목록에서 USB 인스턴스 ID 를 모은다.
                var (rc, stdout, err) = await Proc.RunAsync("pnputil.exe", "/enum-devices /connected", ct);
                if (rc != 0)
                {
                    (rc, stdout, err) = await Proc.RunAsync("pnputil.exe", "/enum-devices", ct);
                    if (rc != 0) { log($"   · pnputil enum 실패 (rc={rc}): {err.Trim()}"); return 0; }
                }

                var usbInstances = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match m in InstanceLine.Matches(stdout))
                {
                    string inst = m.Groups[1].Value.Trim().TrimEnd(',', ')', ']', '.', '"');
                    usbInstances.Add(inst);
                }
                if (usbInstances.Count == 0) { log("   · 연결된 USB 장치 없음"); return 0; }

                // 2) 각 USB 인스턴스가 저장장치 클래스인지 개별 조회해서 판별한다.
                //    (VID/PID 하드코딩 없이, 장치가 스스로 등록한 클래스로 판단)
                var toRemove = new List<string>();
                foreach (var inst in usbInstances)
                {
                    ct.ThrowIfCancellationRequested();
                    if (await IsStorageDeviceAsync(inst, ct))
                        toRemove.Add(inst);
                }

                if (toRemove.Count == 0)
                {
                    log($"   · 연결된 USB 저장장치 없음 (USB 장치 {usbInstances.Count}개 확인, 저장장치 0개)");
                    return 0;
                }

                int removed = 0;
                foreach (var inst in toRemove)
                {
                    ct.ThrowIfCancellationRequested();
                    log($"   · {inst}");
                    if (dryRun) { removed++; continue; }

                    var (rc2, _, err2) = await Proc.RunAsync("pnputil.exe",
                        $"/remove-device \"{inst}\" /force", ct);
                    if (rc2 == 0) { removed++; continue; }

                    (rc2, _, err2) = await Proc.RunAsync("pnputil.exe",
                        $"/remove-device \"{inst}\"", ct);
                    if (rc2 == 0) removed++;
                    else log($"       제거 실패 (rc={rc2}): {err2.Trim()}");
                }
                return removed;
            }

            // 특정 USB 인스턴스가 저장장치인지: 그 장치 또는 하위 자식 장치가
            // DiskDrive/WPD/Volume 클래스로 등록됐는지로 판단.
            static async Task<bool> IsStorageDeviceAsync(string instanceId, CancellationToken ct)
            {
                // pnputil /enum-devices /instanceid "..." 로 단일 장치 상세를 받는다.
                var (rc, stdout, _) = await Proc.RunAsync("pnputil.exe",
                    $"/enum-devices /instanceid \"{instanceId}\"", ct);
                if (rc != 0 || string.IsNullOrEmpty(stdout))
                {
                    // 구버전 pnputil 은 /instanceid 미지원 → 스택 조회로 대체.
                    return await HasStorageChildAsync(instanceId, ct);
                }

                foreach (Match m in ClassGuidLine.Matches(stdout))
                {
                    string guid = m.Value.ToLowerInvariant();
                    foreach (var sg in StorageClassGuids)
                        if (guid.Equals(sg, StringComparison.OrdinalIgnoreCase)) return true;
                }
                // 클래스가 USB(복합) 로 나오는 인클로저는 자식 장치를 확인.
                return await HasStorageChildAsync(instanceId, ct);
            }

            // 자식 장치 중 USBSTOR/디스크가 있는지 확인 (pnputil /enum-devices 전체에서
            // 이 인스턴스를 부모로 갖는 USBSTOR 항목이 있는지).
            static async Task<bool> HasStorageChildAsync(string parentInstanceId, CancellationToken ct)
            {
                var (rc, stdout, _) = await Proc.RunAsync("pnputil.exe", "/enum-devices /relations /instanceid \"" + parentInstanceId + "\"", ct);
                if (rc == 0 && !string.IsNullOrEmpty(stdout))
                {
                    if (stdout.IndexOf("USBSTOR", StringComparison.OrdinalIgnoreCase) >= 0
                     || stdout.IndexOf("WPD", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4) Windows Search VolumeInfoCache — SOFTWARE 하이브의 SYSTEM 소유 키
        // ═══════════════════════════════════════════════════════════════════

        static class VolumeCacheCleaner
        {
            const string Path = @"SOFTWARE\Microsoft\Windows Search\VolumeInfoCache";

            public static int Run(bool dryRun, Action<string> log)
            {
                using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var root = hklm.OpenSubKey(Path, writable: false);
                if (root == null) { log("   · VolumeInfoCache 없음"); return 0; }

                var toDelete = new List<string>();
                // 서브키 이름은 "C:" 형태(콜론 포함)다. 시스템 드라이브도 "C:" 로 맞춘다.
                string systemDrive = (Environment.GetEnvironmentVariable("SystemDrive") ?? "C:").TrimEnd('\\');
                if (!systemDrive.EndsWith(":")) systemDrive += ":";

                foreach (var name in root.GetSubKeyNames())
                {
                    // 드라이브 문자 서브키는 "C:", "D:" 처럼 [문자][콜론] 형태.
                    string letters = name.TrimEnd(':');
                    if (letters.Length != 1 || !char.IsLetter(letters[0])) continue;

                    // 시스템 드라이브는 건드리지 않음.
                    if (name.Equals(systemDrive, StringComparison.OrdinalIgnoreCase)) continue;

                    using var sub = root.OpenSubKey(name);
                    if (sub == null) continue;

                    // DriveType 은 REG_DWORD 지만 값 형이 다를 수 있어 방어적으로 파싱.
                    int driveType = ReadInt(sub, "DriveType");
                    string label = sub.GetValue("VolumeLabel") as string ?? "";

                    // 라벨이 있으면(SATECHI, ROG STRIX Arion 등) 외장으로 보고 삭제.
                    // 라벨이 없는 고정 내장 파티션(D:)은 남긴다.
                    // 이동식(DriveType 2)은 라벨 유무와 무관하게 삭제.
                    bool isExternal = driveType == 2 || !string.IsNullOrEmpty(label);
                    if (isExternal)
                    {
                        toDelete.Add(name);
                        log($"   · {name}: type={driveType}, label=\"{label}\"");
                    }
                    else
                    {
                        log($"   · {name}: type={driveType}, 라벨 없음 → 내장으로 보고 유지");
                    }
                }

                if (toDelete.Count == 0) { log("   · 삭제 대상 없음"); return 0; }
                if (dryRun) return toDelete.Count;

                // SOFTWARE 하이브의 이 키는 SYSTEM 소유. 관리자라도 바로 못 씀 →
                // 소유권을 잠깐 관리자로 잡고, 권한을 부여 후 삭제, 다시 원복.
                int deleted = 0;
                using (RegistryPrivilege.EnableTakeOwnership())
                using (var writable = hklm.OpenSubKey(Path, RegistryKeyPermissionCheck.ReadWriteSubTree,
                                                     RegistryRights.TakeOwnership | RegistryRights.ReadKey | RegistryRights.WriteKey))
                {
                    foreach (var name in toDelete)
                    {
                        try
                        {
                            // 하위 트리째 삭제
                            using (var subKey = writable?.OpenSubKey(name, RegistryKeyPermissionCheck.ReadWriteSubTree,
                                RegistryRights.TakeOwnership | RegistryRights.ChangePermissions | RegistryRights.ReadKey | RegistryRights.WriteKey | RegistryRights.Delete))
                            {
                                if (subKey != null) GrantAdminFullControl(subKey);
                            }
                            writable?.DeleteSubKeyTree(name, throwOnMissingSubKey: false);
                            deleted++;
                        }
                        catch (Exception ex)
                        {
                            log($"       {name} 삭제 실패: {ex.Message}");
                        }
                    }
                }
                return deleted;
            }

            static int ReadInt(RegistryKey key, string name)
            {
                object v = key.GetValue(name);
                return v switch
                {
                    int i => i,
                    long l => (int)l,
                    string s when int.TryParse(s, out var n) => n,
                    _ => 0
                };
            }

            static void GrantAdminFullControl(RegistryKey key)
            {
                var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                var sec = key.GetAccessControl();
                sec.SetOwner(admins);
                sec.AddAccessRule(new RegistryAccessRule(admins,
                    RegistryRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                key.SetAccessControl(sec);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 5) 사용자 하이브 MountPoints2 잔여 GUID
        // ═══════════════════════════════════════════════════════════════════

        static class MountPointsCleaner
        {
            const string SubPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2";

            public static int Run(bool dryRun, Action<string> log)
            {
                int total = 0;

                // 라이브 HKU: 로그인 중인 모든 사용자.
                using (var hku = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64))
                {
                    foreach (var sid in hku.GetSubKeyNames())
                    {
                        if (!sid.StartsWith("S-1-5-21-", StringComparison.OrdinalIgnoreCase)) continue;
                        if (sid.EndsWith("_Classes", StringComparison.OrdinalIgnoreCase)) continue;
                        total += CleanFor(hku, sid, $"HKU\\{sid}", dryRun, log);
                    }
                }

                // 오프라인 사용자 하이브 (로그인 안 한 계정)
                string usersDir = Path.Combine(
                    (Environment.GetEnvironmentVariable("SystemDrive") ?? "C:").TrimEnd('\\') + "\\", "Users");
                if (Directory.Exists(usersDir))
                {
                    foreach (var dir in Directory.EnumerateDirectories(usersDir))
                    {
                        string hive = Path.Combine(dir, "NTUSER.DAT");
                        if (!File.Exists(hive)) continue;

                        using var loaded = LoadedHive.TryLoad(hive);
                        if (loaded == null) continue; // 잠긴 사용자는 위 HKU 에서 이미 처리됨
                        total += CleanFor(loaded.Root, "", $"{Path.GetFileName(dir)} (offline)", dryRun, log);
                    }
                }
                return total;
            }

            static int CleanFor(RegistryKey baseKey, string sidOrEmpty, string label, bool dryRun, Action<string> log)
            {
                string mpPath = string.IsNullOrEmpty(sidOrEmpty) ? SubPath : $"{sidOrEmpty}\\{SubPath}";
                using var mp = baseKey.OpenSubKey(mpPath, writable: !dryRun);
                if (mp == null) return 0;

                var toDelete = new List<string>();
                foreach (var name in mp.GetSubKeyNames())
                {
                    // 볼륨 GUID 형태만 대상. 드라이브 문자(A~Z)는 현재 존재하는
                    // 볼륨과 대응할 수 있어 건드리지 않음.
                    // ##?# 로 시작하는 네트워크/장치 마운트 중 USBSTOR/WPD 는 지운다.
                    if (name.StartsWith("{") && name.EndsWith("}"))
                        toDelete.Add(name);
                    else if (name.Contains("USBSTOR", StringComparison.OrdinalIgnoreCase)
                          || name.Contains("WPD", StringComparison.OrdinalIgnoreCase))
                        toDelete.Add(name);
                }

                if (toDelete.Count == 0) return 0;
                log($"   · {label}: {toDelete.Count}개 항목");
                if (dryRun) return toDelete.Count;

                int deleted = 0;
                foreach (var name in toDelete)
                {
                    try { mp.DeleteSubKeyTree(name, throwOnMissingSubKey: false); deleted++; }
                    catch (Exception ex) { log($"       {name}: {ex.Message}"); }
                }
                return deleted;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 저장장치 판별 근거: 시스템에서 "저장장치로 등록된 VID/PID" 를 모은다.
        // 하드코딩된 장치 ID 가 아니라, 레지스트리에 실제로 남은 근거로 판단.
        // ═══════════════════════════════════════════════════════════════════

        sealed class StorageDeviceOracle
        {
            public static readonly StorageDeviceOracle Instance = new();
            public HashSet<string> StorageVidPids { get; } = new(StringComparer.OrdinalIgnoreCase);
            // USBSTOR 로 확인된 저장장치의 시리얼(인스턴스 ID). VMware vmusb 로 감싼
            // 노드는 Service=vmusb 라 저장장치로 안 보이지만, 인스턴스 ID 의 시리얼은
            // 원본 저장장치와 동일하다. 이 시리얼로 vmusb 잔여를 매칭한다.
            public HashSet<string> StorageSerials { get; } = new(StringComparer.OrdinalIgnoreCase);

            StorageDeviceOracle()
            {
                try { Collect(); } catch { /* best-effort */ }
            }

            // 인스턴스 ID 마지막 토큰(시리얼)을 정규화해서 뽑는다.
            // "USB\Vid_0E0F&Pid_0001\MSFT30012346601" → "MSFT30012346601"
            // "USBSTOR\Disk&Ven_...\040189cd...&0" → "040189cd..." (뒤 &0 제거)
            public static string ExtractSerial(string instanceOrName)
            {
                if (string.IsNullOrEmpty(instanceOrName)) return null;
                string last = instanceOrName;
                int slash = last.LastIndexOf('\\');
                if (slash >= 0) last = last[(slash + 1)..];
                // USBSTOR 인스턴스는 "<serial>&0" 형태로 끝나는 경우가 많다.
                int amp = last.IndexOf('&');
                if (amp > 0) last = last[..amp];
                last = last.Trim();
                // 너무 짧거나 순수 포트 인덱스(예: "6&10d27781&0&4") 형태는 제외.
                return last.Length >= 6 ? last : null;
            }

            void Collect()
            {
                using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                // (1) USBSTOR 서브트리에 남은 장치의 부모 USB VID/PID.
                //     USBSTOR 자체는 이미 지워졌을 수 있으나, 남아 있으면 확실한 근거.
                //     또한 Enum\USB 하위에서 Service 가 USBSTOR/disk 인 노드도 근거.
                foreach (var set in ControlSets(hklm))
                {
                    // Enum\USB 를 훑어 Service=USBSTOR 또는 하위 STORAGE 클래스인 것.
                    using var usb = hklm.OpenSubKey($@"SYSTEM\{set}\Enum\USB");
                    if (usb != null)
                    {
                        foreach (var vidpid in usb.GetSubKeyNames())
                        {
                            var vp = ParseVidPid(vidpid);
                            if (vp == null) continue;
                            using var node = usb.OpenSubKey(vidpid);
                            if (node == null) continue;
                            foreach (var inst in node.GetSubKeyNames())
                            {
                                using var ik = node.OpenSubKey(inst);
                                string svc = ik?.GetValue("Service") as string ?? "";
                                string cls = ik?.GetValue("Class") as string ?? "";
                                string compat = "";
                                if (ik?.GetValue("CompatibleIDs") is string[] cids) compat = string.Join(";", cids);
                                if (svc.Equals("USBSTOR", StringComparison.OrdinalIgnoreCase)
                                 || svc.Equals("disk", StringComparison.OrdinalIgnoreCase)
                                 || svc.Equals("UASPStor", StringComparison.OrdinalIgnoreCase)
                                 || cls.Equals("DiskDrive", StringComparison.OrdinalIgnoreCase)
                                 || cls.Equals("WPD", StringComparison.OrdinalIgnoreCase)
                                 || compat.IndexOf("USBSTOR", StringComparison.OrdinalIgnoreCase) >= 0
                                 || compat.IndexOf("Class_08", StringComparison.OrdinalIgnoreCase) >= 0) // USB Mass Storage 클래스 08h
                                {
                                    StorageVidPids.Add(vp);
                                    var s = ExtractSerial(inst);
                                    if (s != null) StorageSerials.Add(s);
                                    break;
                                }
                            }
                        }
                    }

                    // USBSTOR 트리가 남아 있으면 그 인스턴스 시리얼도 근거로 수집.
                    using var usbstor = hklm.OpenSubKey($@"SYSTEM\{set}\Enum\USBSTOR");
                    if (usbstor != null)
                    {
                        foreach (var disk in usbstor.GetSubKeyNames())
                        using (var dk = usbstor.OpenSubKey(disk))
                        {
                            foreach (var inst in dk?.GetSubKeyNames() ?? Array.Empty<string>())
                            {
                                var s = ExtractSerial(inst);
                                if (s != null) StorageSerials.Add(s);
                            }
                        }
                    }
                }

                // (2) usbflags: 저장장치 특유 플래그가 붙은 경우 근거로 쓸 수 있으나
                //     usbflags 만으로 저장장치를 특정하긴 어렵다. 생략.
            }

            static IEnumerable<string> ControlSets(RegistryKey hklm)
            {
                using var sys = hklm.OpenSubKey("SYSTEM");
                if (sys == null) yield break;
                foreach (var n in sys.GetSubKeyNames())
                    if (n.StartsWith("ControlSet", StringComparison.OrdinalIgnoreCase))
                        yield return n;
            }

            static string ParseVidPid(string s)
            {
                var m = Regex.Match(s, @"VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
                return m.Success ? $"{m.Groups[1].Value}:{m.Groups[2].Value}".ToUpperInvariant() : null;
            }
        }

        static class StorageSerialHeuristic
        {
            // USB Mass Storage 는 iSerialNumber 를 노출하도록 사양에 요구된다.
            // setupapi 헤더의 인스턴스 경로 마지막 토큰이 저장장치 시리얼 형태인지 본다.
            //  - MSFT30… (여러 브리지 칩이 쓰는 접두)
            //  - 20자 이상 순수 16진수 (USBSTOR 해시형 시리얼)
            //  - 볼륨 GUID {8-4-4-4-12}
            static readonly Regex Msft30 = new(@"\\MSFT30[0-9A-Za-z]+\]", RegexOptions.Compiled);
            static readonly Regex LongHex = new(@"\\[0-9A-Fa-f]{20,}[#\]]", RegexOptions.Compiled);
            static readonly Regex VolGuid = new(@"\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}", RegexOptions.Compiled);

            public static bool LooksLikeStorageSerial(string line)
            {
                // 헤더 라인에 한해 판단 (>>> [ ... ]). 그 외 줄은 오탐 위험이 커서 제외.
                if (!line.TrimStart().StartsWith(">>>")) return false;
                // 명백한 비저장 장치(HID/입력/오디오/허브)는 제외.
                if (Regex.IsMatch(line, @"HID\\|&MI_0[0-9]|input|audio|hub", RegexOptions.IgnoreCase)
                    && !line.Contains("USBSTOR", StringComparison.OrdinalIgnoreCase))
                    return false;
                return Msft30.IsMatch(line) || LongHex.IsMatch(line) || VolGuid.IsMatch(line);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 6) Enum\USB / DeviceContainers 의 저장장치 잔여 노드 삭제
        //    (present 라 DriveCleanup 이 못 지운 것, 연결 끊겨 pnputil 이 못 지운 것)
        // ═══════════════════════════════════════════════════════════════════

        static class RegistryUsbCleaner
        {
            public static int Run(bool dryRun, Action<string> log)
            {
                var storageVidPid = new HashSet<string>(StorageDeviceOracle.Instance.StorageVidPids, StringComparer.OrdinalIgnoreCase);
                var storageSerials = StorageDeviceOracle.Instance.StorageSerials;
                if (storageVidPid.Count > 0)
                    log($"   · 저장장치 VID/PID 근거: {string.Join(", ", storageVidPid)}");
                if (storageSerials.Count > 0)
                    log($"   · 저장장치 시리얼 근거: {string.Join(", ", storageSerials)}");

                int deleted = 0;
                using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                // ── 1패스: 지울 대상 VID/PID 를 확정한다 ──
                // Enum\USB 를 훑어, 인스턴스 중 하나라도
                //   (a) 저장장치 시리얼과 일치하거나
                //   (b) Service=vmusb (VMware 가 물리 USB 를 VM 에 넘긴 흔적)
                // 이면 그 VID/PID 전체를 대상으로 삼는다.
                // (b)는 시리얼 형태를 따지지 않는다. vmusb 노드는 그 자체가 "이 호스트에
                // USB 를 꽂아 VM 에 넘겼다"는 흔적이고, 지워도 다음에 VM 에 다시 attach 하면
                // 재생성되므로 실질적 손해가 없다. 포트 형태 시리얼(6&...) 노드도 이걸로 잡힌다.
                var targetVidPid = new HashSet<string>(storageVidPid, StringComparer.OrdinalIgnoreCase);
                foreach (var set in ControlSets(hklm))
                {
                    using var usb = hklm.OpenSubKey($@"SYSTEM\{set}\Enum\USB", writable: false);
                    if (usb == null) continue;
                    foreach (var vidpid in usb.GetSubKeyNames())
                    {
                        var vp = ParseVidPid(vidpid);
                        if (vp == null || targetVidPid.Contains(vp)) continue;
                        using var node = usb.OpenSubKey(vidpid);
                        if (node == null) continue;
                        foreach (var inst in node.GetSubKeyNames())
                        {
                            using var ik = node.OpenSubKey(inst);
                            string svc = ik?.GetValue("Service") as string ?? "";
                            string serial = StorageDeviceOracle.ExtractSerial(inst);
                            bool bySerial = serial != null && storageSerials.Contains(serial);
                            bool byVmusb = svc.Equals("vmusb", StringComparison.OrdinalIgnoreCase);
                            if (bySerial || byVmusb) { targetVidPid.Add(vp); break; }
                        }
                    }
                }

                // 주의: DeviceClasses 링크 이름의 시리얼 형태로 근거를 넓히면 HID 장치
                // (키보드/마우스/게임패드/캡처카드)도 긴 영숫자 시리얼을 가져서 오탐한다.
                // 그래서 근거 수집은 Enum\USB 의 Service=vmusb + 시리얼 형태로만 한정한다.
                // Enum\USB 노드가 이전 실행에서 지워졌다면, 아래 "고아 정리"로 대상 없이도
                // vmusb 흔적만 별도 처리한다.

                if (targetVidPid.Count == 0)
                {
                    log("   · 삭제 대상 저장장치 VID/PID 없음");
                    return 0;
                }
                log($"   · 삭제 대상 VID/PID: {string.Join(", ", targetVidPid)}");

                // ── 2패스: 세 위치에서 대상 VID/PID 제거 ──
                foreach (var set in ControlSets(hklm))
                {
                    // (1) Enum\USB\<VID/PID> 노드 전체
                    string usbPath = $@"SYSTEM\{set}\Enum\USB";
                    using (var usb = hklm.OpenSubKey(usbPath, writable: false))
                    {
                        if (usb != null)
                            foreach (var vidpid in usb.GetSubKeyNames())
                            {
                                var vp = ParseVidPid(vidpid);
                                if (vp == null || !targetVidPid.Contains(vp)) continue;
                                log($"   · {set}\\Enum\\USB\\{vidpid}");
                                if (dryRun) { deleted++; continue; }
                                if (DeleteKeyTreeWithOwnership(hklm, $@"{usbPath}\{vidpid}", log)) deleted++;
                            }
                    }

                    // (2) DeviceClasses\{...}\##?#USB#VID...#... 심볼릭 링크
                    string dclPath = $@"SYSTEM\{set}\Control\DeviceClasses";
                    using (var dcl = hklm.OpenSubKey(dclPath, writable: false))
                    {
                        if (dcl != null)
                            foreach (var iface in dcl.GetSubKeyNames())
                            {
                                using var ik = dcl.OpenSubKey(iface);
                                if (ik == null) continue;
                                foreach (var link in ik.GetSubKeyNames())
                                {
                                    // 링크 이름 예: ##?#USB#Vid_0E0F&Pid_0001#MSFT30...#{guid}
                                    var vp = ParseVidPid(link);
                                    if (vp == null || !targetVidPid.Contains(vp)) continue;
                                    log($"   · {set}\\...\\DeviceClasses\\{iface[..Math.Min(iface.Length,20)]}…\\{link[..Math.Min(link.Length,40)]}…");
                                    if (dryRun) { deleted++; continue; }
                                    if (DeleteKeyTreeWithOwnership(hklm, $@"{dclPath}\{iface}\{link}", log)) deleted++;
                                }
                            }
                    }

                    // (3) DeviceContainers\{cid}: BaseContainers 값 데이터에 대상 VID/PID 참조
                    string dcPath = $@"SYSTEM\{set}\Control\DeviceContainers";
                    using (var dc = hklm.OpenSubKey(dcPath, writable: false))
                    {
                        if (dc != null)
                            foreach (var cid in dc.GetSubKeyNames())
                            {
                                if (!ContainerRefsTarget(dc, cid, targetVidPid)) continue;
                                log($"   · {set}\\Control\\DeviceContainers\\{cid}");
                                if (dryRun) { deleted++; continue; }
                                if (DeleteKeyTreeWithOwnership(hklm, $@"{dcPath}\{cid}", log)) deleted++;
                            }
                    }
                }

                return deleted;
            }

            // DeviceContainers\{cid}\BaseContainers 하위 값의 "데이터"에서 대상 VID/PID 참조 여부.
            // (값 이름이 아니라 값 데이터에 USB\Vid_xxxx&Pid_xxxx\... 가 들어있다)
            static bool ContainerRefsTarget(RegistryKey dc, string cid, HashSet<string> targetVidPid)
            {
                try
                {
                    using var bc = dc.OpenSubKey($@"{cid}\BaseContainers");
                    if (bc == null) return false;
                    foreach (var inner in bc.GetSubKeyNames())
                    {
                        using var ik = bc.OpenSubKey(inner);
                        if (ik == null) continue;
                        foreach (var valName in ik.GetValueNames())
                        {
                            object v = ik.GetValue(valName);
                            string data = v switch
                            {
                                string s => s,
                                string[] arr => string.Join(";", arr),
                                _ => v?.ToString() ?? ""
                            };
                            var vp = ParseVidPid(data);
                            if (vp != null && targetVidPid.Contains(vp)) return true;
                            // 값 이름에도 있을 수 있으니 보조로 확인
                            var vp2 = ParseVidPid(valName);
                            if (vp2 != null && targetVidPid.Contains(vp2)) return true;
                        }
                    }
                }
                catch { }
                return false;
            }

            static IEnumerable<string> ControlSets(RegistryKey hklm)
            {
                using var sys = hklm.OpenSubKey("SYSTEM");
                if (sys == null) yield break;
                foreach (var n in sys.GetSubKeyNames())
                    if (n.StartsWith("ControlSet", StringComparison.OrdinalIgnoreCase))
                        yield return n;
            }

            static string ParseVidPid(string s)
            {
                var m = Regex.Match(s, @"VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
                return m.Success ? $"{m.Groups[1].Value}:{m.Groups[2].Value}".ToUpperInvariant() : null;
            }

            // Enum 하위 키는 SYSTEM/TrustedInstaller 소유라 관리자도 바로 못 지운다.
            // 소유권을 잡고 권한을 부여한 뒤 삭제한다.
            static bool DeleteKeyTreeWithOwnership(RegistryKey hklm, string subPath, Action<string> log)
            {
                try
                {
                    using (RegistryPrivilege.EnableTakeOwnership())
                    {
                        // Enum 하위는 소유자가 SYSTEM 이고 관리자에게 WRITE_DAC 도 없다.
                        // 순서가 중요하다: (1) 소유권을 관리자로 가져오고 → (2) 그 다음에야
                        // DACL 에 FullControl 을 추가할 수 있다. 한 번에 GetAccessControl 하면
                        // 읽기 권한조차 없어 실패한다. 트리 삭제를 위해 재귀적으로 처리한다.
                        TakeOwnershipRecursive(subPath, log);

                        int slash = subPath.LastIndexOf('\\');
                        string parentPath = subPath[..slash];
                        string leaf = subPath[(slash + 1)..];
                        using var parent = hklm.OpenSubKey(parentPath, RegistryKeyPermissionCheck.ReadWriteSubTree,
                            RegistryRights.ReadKey | RegistryRights.WriteKey | RegistryRights.Delete);
                        if (parent == null) return false;
                        parent.DeleteSubKeyTree(leaf, throwOnMissingSubKey: false);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    log($"       삭제 실패 {subPath}: {ex.Message}");
                    return false;
                }
            }

            // subPath(HKLM 상대) 와 그 하위 전체에 대해 소유권을 관리자로 잡고
            // FullControl DACL 을 부여한다.
            static void TakeOwnershipRecursive(string subPath, Action<string> log)
            {
                // 1단계: 소유자 설정 (WRITE_OWNER 만 필요)
                if (!SetOwnerToAdmins(subPath)) return;
                // 2단계: 이제 소유자가 됐으니 DACL 수정 가능 (WRITE_DAC)
                GrantFullControlDacl(subPath);

                // 하위 서브키 재귀
                try
                {
                    using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                        .OpenSubKey(subPath, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey | RegistryRights.EnumerateSubKeys);
                    if (key == null) return;
                    foreach (var child in key.GetSubKeyNames())
                        TakeOwnershipRecursive(subPath + "\\" + child, log);
                }
                catch { /* 하위 열거 실패는 무시하고 상위 삭제 시도 */ }
            }

            static bool SetOwnerToAdmins(string subPath)
            {
                // WRITE_OWNER 로 키를 연다.
                IntPtr hKey;
                int rc = Native.RegOpenKeyEx(Native.HKEY_LOCAL_MACHINE, subPath, 0, Native.WRITE_OWNER, out hKey);
                if (rc != 0) return false;
                try
                {
                    var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                    byte[] sid = new byte[admins.BinaryLength];
                    admins.GetBinaryForm(sid, 0);

                    // 자기 참조 상대 SD 를 만들어 owner 만 설정.
                    var sd = new Native.SECURITY_DESCRIPTOR();
                    Native.InitializeSecurityDescriptor(ref sd, 1);
                    IntPtr pSid = Marshal.AllocHGlobal(sid.Length);
                    try
                    {
                        Marshal.Copy(sid, 0, pSid, sid.Length);
                        Native.SetSecurityDescriptorOwner(ref sd, pSid, false);
                        rc = Native.RegSetKeySecurity(hKey, Native.OWNER_SECURITY_INFORMATION, ref sd);
                        return rc == 0;
                    }
                    finally { Marshal.FreeHGlobal(pSid); }
                }
                finally { Native.RegCloseKey(hKey); }
            }

            static void GrantFullControlDacl(string subPath)
            {
                try
                {
                    using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                        .OpenSubKey(subPath, RegistryKeyPermissionCheck.ReadWriteSubTree,
                            RegistryRights.ReadPermissions | RegistryRights.ChangePermissions);
                    if (key == null) return;
                    var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                    var sec = key.GetAccessControl(AccessControlSections.Access);
                    sec.AddAccessRule(new RegistryAccessRule(admins,
                        RegistryRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                    key.SetAccessControl(sec);
                }
                catch { /* best-effort */ }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 7) Amcache.hve 의 저장장치 기록 삭제
        //    실행 중 하이브는 잠겨 있으므로 오프라인 로드로 특정 키만 제거.
        // ═══════════════════════════════════════════════════════════════════

        static class AmcacheCleaner
        {
            public static int Run(bool dryRun, Action<string> log)
            {
                string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string hivePath = Path.Combine(win, "appcompat", "Programs", "Amcache.hve");
                if (!File.Exists(hivePath)) { log("   · Amcache.hve 없음"); return 0; }

                using var loaded = LoadedHive.TryLoad(hivePath);
                if (loaded == null)
                {
                    log("   · Amcache.hve 로드 실패 (잠겨 있거나 권한 부족). 관리자 권한 확인 필요.");
                    return 0;
                }

                int deleted = 0;

                // InventoryDeviceContainer: PrimaryCategory=storage 인 컨테이너 삭제.
                deleted += CleanContainer(loaded.Root, dryRun, log);
                // InventoryDevicePnp: USBSTOR/disk/WPD 관련 노드 삭제.
                deleted += CleanPnp(loaded.Root, dryRun, log);

                return deleted;
            }

            static int CleanContainer(RegistryKey root, bool dryRun, Action<string> log)
            {
                using var cont = root.OpenSubKey(@"Root\InventoryDeviceContainer", writable: !dryRun);
                if (cont == null) return 0;
                var toDelete = new List<string>();
                foreach (var cid in cont.GetSubKeyNames())
                {
                    using var ck = cont.OpenSubKey(cid);
                    string cats = ck?.GetValue("Categories") as string ?? "";
                    string primary = ck?.GetValue("PrimaryCategory") as string ?? "";
                    if (cats.IndexOf("storage", StringComparison.OrdinalIgnoreCase) >= 0
                     || primary.IndexOf("storage", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string model = ck?.GetValue("ModelName") as string ?? "";
                        toDelete.Add(cid);
                        log($"   · Container {cid} (\"{model}\")");
                    }
                }
                if (dryRun) return toDelete.Count;
                int n = 0;
                foreach (var cid in toDelete)
                {
                    try { cont.DeleteSubKeyTree(cid, throwOnMissingSubKey: false); n++; }
                    catch (Exception ex) { log($"       {cid}: {ex.Message}"); }
                }
                return n;
            }

            static int CleanPnp(RegistryKey root, bool dryRun, Action<string> log)
            {
                using var pnp = root.OpenSubKey(@"Root\InventoryDevicePnp", writable: !dryRun);
                if (pnp == null) return 0;
                var storage = StorageDeviceOracle.Instance.StorageVidPids;
                var toDelete = new List<string>();
                foreach (var name in pnp.GetSubKeyNames())
                {
                    using var k = pnp.OpenSubKey(name);
                    if (k == null) continue;
                    string busId = k.GetValue("BusReportedDescription") as string ?? "";
                    string classGuid = k.GetValue("ClassGuid") as string ?? "";
                    string cls = k.GetValue("Class") as string ?? "";
                    string parentId = k.GetValue("ParentId") as string ?? "";
                    string container = k.GetValue("ContainerId") as string ?? "";

                    bool isStorage =
                        cls.Equals("DiskDrive", StringComparison.OrdinalIgnoreCase) ||
                        cls.Equals("WPD", StringComparison.OrdinalIgnoreCase) ||
                        name.IndexOf("usbstor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("disk", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        MatchesStorage(parentId, storage) ||
                        MatchesStorage(name, storage);

                    if (isStorage) { toDelete.Add(name); log($"   · Pnp {name[..Math.Min(name.Length, 60)]}"); }
                }
                if (dryRun) return toDelete.Count;
                int n = 0;
                foreach (var name in toDelete)
                {
                    try { pnp.DeleteSubKeyTree(name, throwOnMissingSubKey: false); n++; }
                    catch (Exception ex) { log($"       {name}: {ex.Message}"); }
                }
                return n;
            }

            static bool MatchesStorage(string s, HashSet<string> storage)
            {
                if (string.IsNullOrEmpty(s)) return false;
                var m = Regex.Match(s, @"VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
                return m.Success && storage.Contains($"{m.Groups[1].Value}:{m.Groups[2].Value}".ToUpperInvariant());
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 8) 점프리스트: 외장 볼륨을 참조하는 파일을 통째로 삭제
        //    AutomaticDestinations / CustomDestinations 안의 LNK 스트림을 파싱해,
        //    외장 볼륨(REMOVABLE, 또는 시스템 드라이브 외 드라이브 문자) 참조가
        //    하나라도 있으면 그 점프리스트 파일 전체를 삭제한다.
        //    (선택적 스트림 제거는 DestList 재작성이 필요해 파일 손상 위험이 크므로,
        //     통째 삭제 방식을 택함. 삭제된 파일은 Windows 가 다시 생성한다.)
        // ═══════════════════════════════════════════════════════════════════

        static class JumpListCleaner
        {
            // LNK 헤더 시그니처(=CLSID). 점프리스트 내부 스트림은 이걸로 시작한다.
            static readonly byte[] LnkClsid =
            {
                0x4C,0x00,0x00,0x00,0x01,0x14,0x02,0x00,0x00,0x00,0x00,0x00,
                0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46
            };

            public static int Run(bool dryRun, Action<string> log)
            {
                string sysDrive = (Environment.GetEnvironmentVariable("SystemDrive") ?? "C:").TrimEnd('\\').ToUpperInvariant();
                string usersDir = Path.Combine(sysDrive + "\\", "Users");
                if (!Directory.Exists(usersDir)) { log("   · Users 폴더 없음"); return 0; }

                int deleted = 0;
                foreach (var userDir in Directory.EnumerateDirectories(usersDir))
                {
                    foreach (var sub in new[] { "AutomaticDestinations", "CustomDestinations" })
                    {
                        string dir = Path.Combine(userDir, @"AppData\Roaming\Microsoft\Windows\Recent", sub);
                        if (!Directory.Exists(dir)) continue;

                        string pattern = sub == "AutomaticDestinations"
                            ? "*.automaticDestinations-ms" : "*.customDestinations-ms";
                        IEnumerable<string> files;
                        try { files = Directory.EnumerateFiles(dir, pattern); } catch { continue; }

                        foreach (var file in files)
                        {
                            byte[] data;
                            try { data = File.ReadAllBytes(file); } catch { continue; }

                            if (!ReferencesExternalVolume(data, sysDrive)) continue;

                            string user = Path.GetFileName(userDir);
                            log($"   · [{user}] {sub}\\{Path.GetFileName(file)}");
                            if (dryRun) { deleted++; continue; }

                            try
                            {
                                var attrs = File.GetAttributes(file);
                                if ((attrs & FileAttributes.ReadOnly) != 0)
                                    File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);
                                File.Delete(file);
                                deleted++;
                            }
                            catch (Exception ex) { log($"       삭제 실패: {ex.Message}"); }
                        }
                    }
                }
                if (deleted == 0) log("   · 외장 볼륨 참조 점프리스트 없음");
                return deleted;
            }

            // 파일 바이트에서 LNK 스트림을 카빙해, 외장 볼륨 참조가 있는지 검사.
            static bool ReferencesExternalVolume(byte[] buf, string sysDrive)
            {
                int off = 0;
                while (true)
                {
                    int idx = IndexOf(buf, LnkClsid, off);
                    if (idx < 0) break;
                    off = idx + 4;
                    if (StreamHasExternalVolume(buf, idx, sysDrive)) return true;
                }
                return false;
            }

            // 하나의 LNK 스트림(idx 시작)에서 VolumeID/LocalBasePath 를 읽어 외장인지 판정.
            static bool StreamHasExternalVolume(byte[] buf, int start, string sysDrive)
            {
                try
                {
                    if (start + 76 > buf.Length) return false;
                    uint flags = BitConverter.ToUInt32(buf, start + 20);
                    int pos = start + 76;

                    // HasLinkTargetIDList
                    if ((flags & 0x01) != 0)
                    {
                        if (pos + 2 > buf.Length) return false;
                        ushort idsize = BitConverter.ToUInt16(buf, pos);
                        pos += 2 + idsize;
                    }

                    // HasLinkInfo
                    if ((flags & 0x02) == 0) return false;
                    if (pos + 28 > buf.Length) return false;
                    int liStart = pos;
                    uint liSize = BitConverter.ToUInt32(buf, liStart);
                    if (liStart + liSize > buf.Length || liSize < 28) return false;

                    uint liFlags = BitConverter.ToUInt32(buf, liStart + 8);
                    uint volOff = BitConverter.ToUInt32(buf, liStart + 12);
                    uint lbpOff = BitConverter.ToUInt32(buf, liStart + 16);

                    // VolumeIDAndLocalBasePath
                    if ((liFlags & 0x01) != 0 && volOff != 0 && liStart + volOff + 8 <= buf.Length)
                    {
                        uint driveType = BitConverter.ToUInt32(buf, (int)(liStart + volOff + 4));
                        // DRIVE_REMOVABLE = 2 → 외장 이동식.
                        if (driveType == 2) return true;
                    }

                    // LocalBasePath: 시스템 드라이브 외 드라이브 문자로 시작하면 외장으로 간주.
                    if (lbpOff != 0 && liStart + lbpOff < buf.Length)
                    {
                        int p = (int)(liStart + lbpOff);
                        int end = p;
                        while (end < buf.Length && buf[end] != 0) end++;
                        string path = Encoding.Latin1.GetString(buf, p, end - p);
                        if (path.Length >= 2 && path[1] == ':')
                        {
                            string drv = path.Substring(0, 2).ToUpperInvariant();
                            if (drv != sysDrive) return true;
                        }
                    }
                }
                catch { }
                return false;
            }

            static int IndexOf(byte[] hay, byte[] needle, int start)
            {
                int last = hay.Length - needle.Length;
                for (int i = Math.Max(0, start); i <= last; i++)
                {
                    int j = 0;
                    while (j < needle.Length && hay[i + j] == needle[j]) j++;
                    if (j == needle.Length) return i;
                }
                return -1;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 9) Prefetch 전체 삭제
        //    외장 볼륨 참조(.pf)와 삭제 도구 실행 흔적을 모두 제거한다.
        //    선택 삭제가 아니라 전체 삭제인 이유: 조사에서 우리를 특정한 결정적
        //    증거가 외장 게임 실행이 아니라 "삭제 도구(DriveCleanup/sdelete/wevtutil…)
        //    실행 흔적"이었기 때문. 외장 참조 .pf 만 지우면 도구 흔적이 남는다.
        //    주의: Prefetch 는 시스템 기능이라 이후 프로그램 실행 시 다시 생성된다.
        //    (완전 차단은 서비스 비활성화가 필요하나, 그 자체가 특이 상태가 되므로
        //     여기서는 하지 않는다.)
        // ═══════════════════════════════════════════════════════════════════

        static class PrefetchCleaner
        {
            public static int Run(bool dryRun, Action<string> log)
            {
                string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string dir = Path.Combine(win, "Prefetch");
                if (!Directory.Exists(dir)) { log("   · Prefetch 폴더 없음"); return 0; }

                string[] files;
                try { files = Directory.GetFiles(dir, "*.pf"); }
                catch (Exception ex) { log($"   · Prefetch 목록 실패: {ex.Message}"); return 0; }

                if (files.Length == 0) { log("   · Prefetch .pf 파일 없음 (이미 비었거나 비활성)"); return 0; }

                log($"   · Prefetch {files.Length}개 .pf 삭제 예정");
                if (dryRun) return files.Length;

                int deleted = 0;
                foreach (var f in files)
                {
                    try
                    {
                        var attrs = File.GetAttributes(f);
                        if ((attrs & FileAttributes.ReadOnly) != 0)
                            File.SetAttributes(f, attrs & ~FileAttributes.ReadOnly);
                        File.Delete(f);
                        deleted++;
                    }
                    catch { /* 사용 중이거나 권한 문제인 개별 파일은 건너뜀 */ }
                }
                log($"   · {deleted}/{files.Length}개 삭제 완료"
                    + (deleted < files.Length ? " (일부는 사용 중/권한으로 실패)" : ""));
                return deleted;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // 10) Shellbags: 외장 드라이브 노드가 있는 "내 PC" 컨테이너 하위 초기화 (옵션 B)
        //     BagMRU 트리를 순회해, 어떤 노드의 shell item 이 외장 드라이브
        //     (시스템·데이터 드라이브 외의 문자)면, 그 노드의 부모(내 PC 컨테이너)
        //     하위를 통째로 비운다. C:/D: 폴더 보기설정도 함께 초기화되지만
        //     외장 폴더 트리(Game\SOMISOFT\NC 등)가 확실히 사라진다.
        //     UsrClass 와 NTUSER 양쪽 하이브를 처리한다.
        // ═══════════════════════════════════════════════════════════════════

        static class ShellbagsCleaner
        {
            public static int Run(bool dryRun, Action<string> log)
            {
                string sysDrive = (Environment.GetEnvironmentVariable("SystemDrive") ?? "C:").TrimEnd('\\').ToUpperInvariant();
                int total = 0;

                // 라이브 로그인 사용자: HKU\<sid> (NTUSER) + HKU\<sid>_Classes (UsrClass)
                using (var hku = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64))
                {
                    foreach (var sid in SubKeys(hku))
                    {
                        if (!sid.StartsWith("S-1-5-21-", StringComparison.OrdinalIgnoreCase)) continue;
                        if (sid.EndsWith("_Classes", StringComparison.OrdinalIgnoreCase)) continue;

                        // NTUSER 측
                        total += CleanHive(hku, $@"{sid}\Software\Microsoft\Windows\Shell\BagMRU",
                                           $"HKU\\{sid} (NTUSER)", sysDrive, dryRun, log);
                        // UsrClass 측
                        total += CleanHive(hku, $@"{sid}_Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU",
                                           $"HKU\\{sid}_Classes (UsrClass)", sysDrive, dryRun, log);
                    }
                }
                if (total == 0) log("   · 외장 드라이브 Shellbag 없음");
                return total;
            }

            // BagMRU 루트에서 외장 드라이브 노드를 찾아, 그 부모 컨테이너 하위를 비운다.
            static int CleanHive(RegistryKey baseKey, string bagMruPath, string label,
                                 string sysDrive, bool dryRun, Action<string> log)
            {
                using var root = Open(baseKey, bagMruPath, writable: false);
                if (root == null) return 0;

                // 외장 드라이브 노드를 가진 "부모 컨테이너"들을 수집.
                var containers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                FindExternalContainers(root, "", sysDrive, containers);
                if (containers.Count == 0) return 0;

                int cleaned = 0;
                foreach (var rel in containers)
                {
                    string containerPath = string.IsNullOrEmpty(rel) ? bagMruPath : bagMruPath + "\\" + rel;
                    log($"   · [{label}] BagMRU\\{rel} 하위 초기화");
                    if (dryRun) { cleaned++; continue; }

                    try
                    {
                        using var container = Open(baseKey, containerPath, writable: true);
                        if (container == null) continue;
                        // 컨테이너 하위 서브키(드라이브/폴더 노드) 전부 삭제.
                        foreach (var sub in SubKeys(container).ToList())
                        {
                            try { container.DeleteSubKeyTree(sub, throwOnMissingSubKey: false); } catch { }
                        }
                        // MRUListEx 를 비워, 남은 값 인덱스 참조를 끊는다.
                        try { container.SetValue("MRUListEx", new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, RegistryValueKind.Binary); } catch { }
                        // 드라이브 항목 값(0,1,2,3…)도 제거.
                        foreach (var vn in container.GetValueNames().ToList())
                        {
                            if (int.TryParse(vn, out _))
                                try { container.DeleteValue(vn, false); } catch { }
                        }
                        cleaned++;
                    }
                    catch (Exception ex) { log($"       실패: {ex.Message}"); }
                }
                return cleaned;
            }

            // 재귀로 BagMRU 트리를 돌며, shell item 이 외장 드라이브인 노드를 만나면
            // 그 "부모 경로"를 컨테이너로 기록한다.
            static void FindExternalContainers(RegistryKey node, string rel, string sysDrive,
                                               HashSet<string> containers)
            {
                foreach (var valName in ValueNames(node))
                {
                    if (!int.TryParse(valName, out _)) continue;
                    if (node.GetValue(valName) is not byte[] b) continue;
                    if (b.Length >= 3 && (b[2] & 0x70) == 0x20) // volume/drive shell item
                    {
                        string txt = Encoding.Latin1.GetString(b, 3, Math.Min(23, b.Length - 3));
                        int z = txt.IndexOf('\0'); if (z >= 0) txt = txt[..z];
                        if (txt.Length >= 2 && txt[1] == ':')
                        {
                            string drv = txt[..2].ToUpperInvariant();
                            if (drv != sysDrive && drv != "D:")
                            {
                                // 이 드라이브 노드가 속한 컨테이너 = 현재 rel (부모)
                                containers.Add(rel);
                            }
                        }
                    }
                }
                foreach (var sub in SubKeys(node))
                {
                    using var sk = Open(node, sub, writable: false);
                    if (sk != null)
                        FindExternalContainers(sk, string.IsNullOrEmpty(rel) ? sub : rel + "\\" + sub, sysDrive, containers);
                }
            }

            static RegistryKey Open(RegistryKey root, string path, bool writable)
            {
                try { return root.OpenSubKey(path, writable); } catch { return null; }
            }
            static IEnumerable<string> SubKeys(RegistryKey k)
            {
                try { return k.GetSubKeyNames(); } catch { return Array.Empty<string>(); }
            }
            static IEnumerable<string> ValueNames(RegistryKey k)
            {
                try { return k.GetValueNames(); } catch { return Array.Empty<string>(); }
            }
        }

        static class Proc
        {
            static Proc() { try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { } }

            public static async Task<(int rc, string stdout, string stderr)> RunAsync(
                string exe, string args, CancellationToken ct)
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    UseShellExecute = false, CreateNoWindow = true,
                };
                // wevtutil / pnputil 출력이 OEM 또는 UTF-16 일 수 있어 바이트로 받아서 판별.
                using var p = Process.Start(psi) ?? throw new InvalidOperationException(exe + " 실행 실패");
                using var outMs = new MemoryStream();
                using var errMs = new MemoryStream();
                var t1 = p.StandardOutput.BaseStream.CopyToAsync(outMs, ct);
                var t2 = p.StandardError.BaseStream.CopyToAsync(errMs, ct);
                await p.WaitForExitAsync(ct);
                await Task.WhenAll(t1, t2);
                return (p.ExitCode, DecodeConsole(outMs.ToArray()), DecodeConsole(errMs.ToArray()));
            }

            static string DecodeConsole(byte[] b)
            {
                if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE) return Encoding.Unicode.GetString(b, 2, b.Length - 2);
                if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) return Encoding.UTF8.GetString(b, 3, b.Length - 3);
                try { return new UTF8Encoding(false, true).GetString(b); }
                catch (DecoderFallbackException)
                {
                    try { return Encoding.GetEncoding((int)GetOEMCP()).GetString(b); }
                    catch { return Encoding.Default.GetString(b); }
                }
            }

            [DllImport("kernel32.dll")] static extern uint GetOEMCP();
        }

        sealed class LoadedHive : IDisposable
        {
            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            static extern int RegLoadKey(IntPtr hKey, string subKey, string file);
            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            static extern int RegUnLoadKey(IntPtr hKey, string subKey);
            static readonly IntPtr HKLM = new(unchecked((int)0x80000002));

            static int _seq;
            public string Mount { get; }
            public RegistryKey Root { get; }
            LoadedHive(string mount, RegistryKey root) { Mount = mount; Root = root; }

            public static LoadedHive TryLoad(string hivePath)
            {
                using (RegistryPrivilege.EnableBackupRestore()) { }
                string mount = $"Ashes_{Environment.ProcessId}_{Interlocked.Increment(ref _seq)}";
                int rc = RegLoadKey(HKLM, mount, hivePath);
                if (rc != 0) return null;
                var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                                      .OpenSubKey(mount, writable: true);
                if (root == null) { RegUnLoadKey(HKLM, mount); return null; }
                return new LoadedHive(mount, root);
            }

            public void Dispose()
            {
                try { Root.Dispose(); } catch { }
                try { RegUnLoadKey(HKLM, Mount); } catch { }
            }
        }

        static class Native
        {
            public static readonly IntPtr HKEY_LOCAL_MACHINE = new(unchecked((int)0x80000002));
            public const int WRITE_OWNER = 0x00080000;
            public const int OWNER_SECURITY_INFORMATION = 0x00000001;

            [StructLayout(LayoutKind.Sequential)]
            public struct SECURITY_DESCRIPTOR
            {
                public byte Revision;
                public byte Sbz1;
                public ushort Control;
                public IntPtr Owner;
                public IntPtr Group;
                public IntPtr Sacl;
                public IntPtr Dacl;
            }

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int RegOpenKeyEx(IntPtr hKey, string subKey, int options, int samDesired, out IntPtr phkResult);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern int RegCloseKey(IntPtr hKey);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern int RegSetKeySecurity(IntPtr hKey, int securityInformation, ref SECURITY_DESCRIPTOR pSecurityDescriptor);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool InitializeSecurityDescriptor(ref SECURITY_DESCRIPTOR pSecurityDescriptor, uint dwRevision);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool SetSecurityDescriptorOwner(ref SECURITY_DESCRIPTOR pSecurityDescriptor, IntPtr pOwner, bool bOwnerDefaulted);
        }

        static class RegistryPrivilege
        {
            [DllImport("advapi32.dll", SetLastError = true)]
            static extern bool OpenProcessToken(IntPtr h, uint access, out IntPtr token);
            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            static extern bool LookupPrivilegeValue(string sys, string name, out LUID luid);
            [DllImport("advapi32.dll", SetLastError = true)]
            static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TP newState, uint bufLen, IntPtr prev, IntPtr retLen);
            [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
            [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

            [StructLayout(LayoutKind.Sequential)] struct LUID { public uint Lo; public int Hi; }
            [StructLayout(LayoutKind.Sequential)] struct TP { public uint Count; public LUID Luid; public uint Attr; }
            const uint TOKEN_ADJUST = 0x20, TOKEN_QUERY = 0x8, SE_ENABLED = 0x2;

            public static IDisposable EnableTakeOwnership()
            {
                Enable("SeTakeOwnershipPrivilege");
                Enable("SeRestorePrivilege");
                return new Dummy();
            }
            public static IDisposable EnableBackupRestore()
            {
                Enable("SeBackupPrivilege");
                Enable("SeRestorePrivilege");
                return new Dummy();
            }
            static void Enable(string name)
            {
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST | TOKEN_QUERY, out var tok)) return;
                try
                {
                    if (!LookupPrivilegeValue(null, name, out var luid)) return;
                    var tp = new TP { Count = 1, Luid = luid, Attr = SE_ENABLED };
                    AdjustTokenPrivileges(tok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                }
                finally { CloseHandle(tok); }
            }
            sealed class Dummy : IDisposable { public void Dispose() { } }
        }
    }
}
