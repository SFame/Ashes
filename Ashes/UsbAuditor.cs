// UsbAuditor — 외장/이동식 장치 연결 흔적 수집 (Ashes 내장)
// UsbHistoryAudit 콘솔 앱의 스캔 로직을 Ashes 라이브러리 클래스로 이식.
// 콘솔 대신 UsbAuditor.RunAndExport(outPath, onOutput) 로 호출한다.
// 관리자 권한 필요 (app.manifest 에서 requireAdministrator).
//
// 원본이 nullable enable 로 작성됐으므로 이 파일만 nullable 컨텍스트를 켠다
// (Ashes 프로젝트 기본은 Nullable disable).
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Ashes.UsbAudit
{

// ───────────────────────── 데이터 모델 ─────────────────────────

public sealed class Finding
{
    public string Source { get; init; } = "";     // Registry / SetupAPI / EventLog / LNK / ShadowCopy / Amcache
    public string Category { get; init; } = "";   // 세부 구분 (예: ControlSet001\Enum\USBSTOR)
    public string Location { get; init; } = "";   // 키 경로 / 파일 경로 / 채널명
    public string Name { get; init; } = "";       // 항목 이름
    public string Detail { get; init; } = "";     // 값 덤프, 로그 라인 등
    public DateTime? Timestamp { get; init; }     // 키 LastWrite, 로그 시각 등
}

public sealed class Report
{
    public string Host { get; set; } = Environment.MachineName;
    public string User { get; set; } = Environment.UserName;
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string Args { get; set; } = "";
    public List<string> Notes { get; } = new();
    public List<Finding> Findings { get; } = new();
}

public sealed class Options
{
    public string? OutPath;
    public bool Shadow;          // 볼륨 섀도 복사본 안의 과거 하이브까지 검사
    public bool OfflineUsers;    // 로그인하지 않은 사용자의 NTUSER.DAT까지 로드해서 검사
    public bool NoEvents;        // 이벤트 로그 생략
    public int MaxEvents = 20000;// 채널당 최근 N건
    public bool Verbose;         // 콘솔에 전체 출력
}

// ───────────────────────── 패턴 ─────────────────────────

static class Markers
{
    // 외장/이동식 장치 흔적으로 판단할 문자열
    public static readonly Regex Pattern = new(
        @"USBSTOR|USB\\VID_|USB#VID_|VID_[0-9A-Fa-f]{4}&PID_|WPDBUSENUM|WpdBusEnumRoot|_\?\?_USBSTOR|" +
        @"1394\\|SBP2\\|SD\\|SDBUS|MMC\\|UASPSTOR|STORAGE#RemovableMedia|Removable",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool Hit(string? s) => !string.IsNullOrEmpty(s) && Pattern.IsMatch(s);
}

// ───────────────────────── Win32 ─────────────────────────

static class Native
{
    // ── Prefetch MAM(XPRESS Huffman) 압축 해제 ──
    [DllImport("ntdll.dll")]
    public static extern int RtlGetCompressionWorkSpaceSize(
        ushort formatAndEngine, ref uint bufferWorkSpaceSize, ref uint fragmentWorkSpaceSize);

    [DllImport("ntdll.dll")]
    public static extern int RtlDecompressBufferEx(
        ushort formatAndEngine,
        byte[] uncompressedBuffer, uint uncompressedBufferSize,
        byte[] compressedBuffer, uint compressedBufferSize,
        ref uint finalUncompressedSize, byte[] workSpace);

    // ── 볼륨 시리얼 조회 (내장 볼륨 식별용) ──
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetVolumeInformation(
        string rootPathName, System.Text.StringBuilder volumeNameBuffer, int volumeNameSize,
        out uint volumeSerialNumber, out uint maximumComponentLength, out uint fileSystemFlags,
        System.Text.StringBuilder fileSystemNameBuffer, int fileSystemNameSize);

    public static string GetVolumeSerial(string root)
    {
        try
        {
            if (!root.EndsWith("\\")) root += "\\";
            if (GetVolumeInformation(root, null, 0, out uint serial, out _, out _, null, 0))
                return $"{(serial >> 16):X4}-{(serial & 0xFFFF):X4}";
        }
        catch { }
        return null;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    static extern int RegQueryInfoKey(
        SafeRegistryHandle hKey, IntPtr lpClass, IntPtr lpcchClass, IntPtr lpReserved,
        IntPtr lpcSubKeys, IntPtr lpcbMaxSubKeyLen, IntPtr lpcbMaxClassLen, IntPtr lpcValues,
        IntPtr lpcbMaxValueNameLen, IntPtr lpcbMaxValueLen, IntPtr lpcbSecurityDescriptor,
        out long lpftLastWriteTime);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int RegLoadKey(IntPtr hKey, string lpSubKey, string lpFile);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int RegUnLoadKey(IntPtr hKey, string lpSubKey);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool LookupPrivilegeValue(string? systemName, string name, out LUID luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAll, ref TOKEN_PRIVILEGES newState,
        uint bufferLength, IntPtr previousState, IntPtr returnLength);

    [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

    [StructLayout(LayoutKind.Sequential)] struct LUID { public uint LowPart; public int HighPart; }
    [StructLayout(LayoutKind.Sequential)] struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID Luid; public uint Attributes; }

    const uint TOKEN_ADJUST_PRIVILEGES = 0x20, TOKEN_QUERY = 0x8, SE_PRIVILEGE_ENABLED = 0x2;

    public static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));

    public static DateTime? LastWrite(RegistryKey key)
    {
        try
        {
            int rc = RegQueryInfoKey(key.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out long ft);
            return rc == 0 ? DateTime.FromFileTime(ft) : null;
        }
        catch { return null; }
    }

    public static bool EnablePrivilege(string name)
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr token)) return false;
        try
        {
            if (!LookupPrivilegeValue(null, name, out LUID luid)) return false;
            var tp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = SE_PRIVILEGE_ENABLED };
            if (!AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero)) return false;
            return Marshal.GetLastWin32Error() == 0;
        }
        finally { CloseHandle(token); }
    }
}

// ───────────────────────── 하이브 로드 헬퍼 ─────────────────────────

sealed class LoadedHive : IDisposable
{
    public string MountName { get; }
    public RegistryKey Root { get; }
    readonly string? _tempCopy;

    LoadedHive(string mountName, RegistryKey root, string? tempCopy) { MountName = mountName; Root = root; _tempCopy = tempCopy; }

    static int _seq;

    /// <summary>하이브 파일을 HKLM 아래 임시 이름으로 로드. copyFirst=true면 임시 복사본을 만들어 로드.</summary>
    public static LoadedHive? Load(string hivePath, bool copyFirst, Report report)
    {
        string? tmp = null;
        try
        {
            string src = hivePath;
            if (copyFirst)
            {
                tmp = Path.Combine(Path.GetTempPath(), $"UsbAudit_{Environment.ProcessId}_{Interlocked.Increment(ref _seq)}.hive");
                File.Copy(hivePath, tmp, true);
                src = tmp;
            }
            string mount = $"UsbAudit_{Environment.ProcessId}_{Interlocked.Increment(ref _seq)}";
            int rc = Native.RegLoadKey(Native.HKEY_LOCAL_MACHINE, mount, src);
            if (rc != 0)
            {
                report.Notes.Add($"[하이브 로드 실패] {hivePath} (Win32 오류 {rc})");
                if (tmp != null) TryDelete(tmp);
                return null;
            }
            var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(mount);
            if (root == null)
            {
                Native.RegUnLoadKey(Native.HKEY_LOCAL_MACHINE, mount);
                if (tmp != null) TryDelete(tmp);
                return null;
            }
            return new LoadedHive(mount, root, tmp);
        }
        catch (Exception ex)
        {
            report.Notes.Add($"[하이브 로드 실패] {hivePath}: {ex.Message}");
            if (tmp != null) TryDelete(tmp);
            return null;
        }
    }

    public void Dispose()
    {
        try { Root.Dispose(); } catch { }
        try { Native.RegUnLoadKey(Native.HKEY_LOCAL_MACHINE, MountName); } catch { }
        if (_tempCopy != null) TryDelete(_tempCopy);
    }

    static void TryDelete(string p) { try { File.Delete(p); } catch { } }
}

// ───────────────────────── 레지스트리 헬퍼 ─────────────────────────

static class Reg
{
    public static RegistryKey? Open(RegistryKey root, string path)
    {
        try { return root.OpenSubKey(path, false); } catch { return null; }
    }

    public static IEnumerable<string> SubKeys(RegistryKey key)
    {
        try { return key.GetSubKeyNames(); } catch { return Array.Empty<string>(); }
    }

    public static string DumpValues(RegistryKey key, int max = 16)
    {
        var sb = new StringBuilder();
        string[] names;
        try { names = key.GetValueNames(); } catch { return ""; }
        int n = 0;
        foreach (var name in names)
        {
            if (n++ >= max) { sb.Append("; …"); break; }
            object? v;
            try { v = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames); } catch { continue; }
            string text = v switch
            {
                null => "(null)",
                string s => s,
                string[] arr => string.Join(" | ", arr),
                byte[] b => BinaryToText(b),
                _ => v.ToString() ?? ""
            };
            if (text.Length > 200) text = text[..200] + "…";
            if (sb.Length > 0) sb.Append("; ");
            sb.Append(string.IsNullOrEmpty(name) ? "(Default)" : name).Append('=').Append(text);
        }
        return sb.ToString();
    }

    public static string BinaryToText(byte[] b)
    {
        // UTF-16 문자열로 보이면 디코딩, 아니면 hex
        if (b.Length >= 4 && b.Length % 2 == 0 && b[1] == 0 && b[3] == 0)
        {
            try
            {
                var s = Encoding.Unicode.GetString(b).TrimEnd('\0');
                if (s.All(c => !char.IsControl(c) || c == '\0')) return s;
            }
            catch { }
        }
        return "0x" + Convert.ToHexString(b, 0, Math.Min(b.Length, 32)) + (b.Length > 32 ? "…" : "");
    }
}

// ───────────────────────── 수집기 ─────────────────────────

sealed class Auditor
{
    readonly Report _r;
    readonly Options _o;
    readonly HashSet<string> _usbVolumeGuids = new(StringComparer.OrdinalIgnoreCase); // MountedDevices에서 USB로 확인된 볼륨 GUID
    readonly string _windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    readonly string _sysDrive = (Environment.GetEnvironmentVariable("SystemDrive") ?? "C:").TrimEnd('\\');

    static readonly string[] DeviceClassGuids =
    {
        "{53f56307-b6bf-11d0-94f2-00a0c91efb8b}", // GUID_DEVINTERFACE_DISK
        "{53f5630d-b6bf-11d0-94f2-00a0c91efb8b}", // GUID_DEVINTERFACE_VOLUME
        "{a5dcbf10-6530-11d2-901f-00c04fb951ed}", // GUID_DEVINTERFACE_USB_DEVICE
        "{6ac27878-a6fa-4155-ba85-f98f491d4f33}", // GUID_DEVINTERFACE_WPD
        "{2accfe60-c130-11d2-b082-00a0c91efb8b}", // GUID_DEVINTERFACE_STORAGEPORT
        "{10497b1b-ba51-44e5-8318-a65c837b6661}", // GUID_DEVINTERFACE_CDROM
    };

    public Auditor(Report r, Options o) { _r = r; _o = o; }

    void Add(string source, string category, string location, string name, string detail, DateTime? ts = null)
        => _r.Findings.Add(new Finding { Source = source, Category = category, Location = location, Name = name, Detail = detail, Timestamp = ts });

    // ═══════════════ 1. SYSTEM 하이브 ═══════════════

    public void ScanSystemHive(RegistryKey sysRoot, string source)
    {
        int? current = null;
        using (var sel = Reg.Open(sysRoot, "Select"))
            if (sel?.GetValue("Current") is int c) current = c;

        var sets = Reg.SubKeys(sysRoot).Where(n => n.StartsWith("ControlSet", StringComparison.OrdinalIgnoreCase)).ToList();
        if (sets.Count == 0) _r.Notes.Add($"[{source}] ControlSet 키를 찾지 못했습니다.");

        foreach (var set in sets)
        {
            bool isCurrent = current.HasValue && set.Equals($"ControlSet{current.Value:D3}", StringComparison.OrdinalIgnoreCase);
            string tag = isCurrent ? $"{set}(현재)" : $"{set}(비활성)";

            ScanTree(sysRoot, $@"{set}\Enum\USBSTOR", source, $@"{tag}\Enum\USBSTOR", 2, requireHit: false);
            ScanTree(sysRoot, $@"{set}\Enum\USB", source, $@"{tag}\Enum\USB", 2, requireHit: false,
                skip: n => n.Contains("ROOT_HUB", StringComparison.OrdinalIgnoreCase) || n.Contains("VID_0000", StringComparison.OrdinalIgnoreCase));
            ScanTree(sysRoot, $@"{set}\Enum\SCSI", source, $@"{tag}\Enum\SCSI (내장 디스크 포함 가능, 검토 필요)", 2, requireHit: false);
            ScanTree(sysRoot, $@"{set}\Enum\SWD\WPDBUSENUM", source, $@"{tag}\Enum\SWD\WPDBUSENUM", 1, requireHit: false);
            ScanTree(sysRoot, $@"{set}\Enum\WpdBusEnumRoot", source, $@"{tag}\Enum\WpdBusEnumRoot", 2, requireHit: false);
            ScanTree(sysRoot, $@"{set}\Enum\STORAGE\Volume", source, $@"{tag}\Enum\STORAGE\Volume", 1, requireHit: true);
            ScanTree(sysRoot, $@"{set}\Enum\STORAGE\RemovableMedia", source, $@"{tag}\Enum\STORAGE\RemovableMedia", 1, requireHit: false);
            ScanTree(sysRoot, $@"{set}\Enum\1394", source, $@"{tag}\Enum\1394", 2, requireHit: false);
            ScanTree(sysRoot, $@"{set}\Enum\SD", source, $@"{tag}\Enum\SD", 2, requireHit: false);
            ScanTree(sysRoot, $@"{set}\Control\usbflags", source, $@"{tag}\Control\usbflags", 1, requireHit: false);

            foreach (var guid in DeviceClassGuids)
                ScanTree(sysRoot, $@"{set}\Control\DeviceClasses\{guid}", source, $@"{tag}\Control\DeviceClasses\{guid}", 1, requireHit: true);

            // 디스크 클래스: MatchingDeviceId 에 usbstor 가 남는 경우
            ScanTree(sysRoot, $@"{set}\Control\Class\{{4d36e967-e325-11ce-bfc1-08002be10318}}", source, $@"{tag}\Control\Class\DiskDrive", 1, requireHit: true);

            // 서비스 Enum 목록
            ScanValues(sysRoot, $@"{set}\Services\Disk\Enum", source, $@"{tag}\Services\Disk\Enum", requireHit: true);
            ScanValues(sysRoot, $@"{set}\Services\USBSTOR\Enum", source, $@"{tag}\Services\USBSTOR\Enum", requireHit: false);
            ScanValues(sysRoot, $@"{set}\Services\WUDFRd\Enum", source, $@"{tag}\Services\WUDFRd\Enum", requireHit: true);
            ScanValues(sysRoot, $@"{set}\Services\UASPStor\Enum", source, $@"{tag}\Services\UASPStor\Enum", requireHit: false);

            // DeviceContainers: BaseContainers 하위에 USB 인스턴스 경로가 남음
            using var dc = Reg.Open(sysRoot, $@"{set}\Control\DeviceContainers");
            if (dc != null)
            {
                foreach (var cid in Reg.SubKeys(dc))
                {
                    using var ck = Reg.Open(dc, cid);
                    if (ck == null) continue;
                    using var bc = Reg.Open(ck, "BaseContainers");
                    if (bc == null) continue;
                    foreach (var inner in Reg.SubKeys(bc))
                    {
                        using var ik = Reg.Open(bc, inner);
                        if (ik == null) continue;
                        var names = string.Join(" | ", ik.GetValueNames());
                        if (Markers.Hit(names))
                            Add(source, $@"{tag}\Control\DeviceContainers", $@"{set}\Control\DeviceContainers\{cid}\BaseContainers\{inner}", cid, names, Native.LastWrite(ik));
                    }
                }
            }
        }

        // MountedDevices
        using var md = Reg.Open(sysRoot, "MountedDevices");
        if (md == null) _r.Notes.Add($"[{source}] MountedDevices 키가 없습니다.");
        else
        {
            foreach (var name in md.GetValueNames())
            {
                if (md.GetValue(name) is not byte[] b) continue;
                string text = Reg.BinaryToText(b);
                if (!Markers.Hit(text)) continue;
                Add(source, "MountedDevices", "MountedDevices", name, text, Native.LastWrite(md));
                var m = Regex.Match(name, @"Volume(\{[0-9a-f\-]{36}\})", RegexOptions.IgnoreCase);
                if (m.Success) _usbVolumeGuids.Add(m.Groups[1].Value);
            }
        }
    }

    void ScanTree(RegistryKey root, string path, string source, string category, int depth, bool requireHit, Func<string, bool>? skip = null)
    {
        using var k = Reg.Open(root, path);
        if (k == null) return;
        Walk(k, path, depth);

        void Walk(RegistryKey key, string keyPath, int d)
        {
            foreach (var sub in Reg.SubKeys(key))
            {
                if (skip != null && skip(sub)) continue;
                using var sk = Reg.Open(key, sub);
                if (sk == null) continue;
                string full = keyPath + "\\" + sub;
                bool hasChildren = false;
                try { hasChildren = sk.SubKeyCount > 0; } catch { }
                if (d > 1 && hasChildren) { Walk(sk, full, d - 1); continue; }

                string dump = Reg.DumpValues(sk);
                if (requireHit && !Markers.Hit(full) && !Markers.Hit(dump)) continue;
                Add(source, category, full, sub, dump, Native.LastWrite(sk));
            }
        }
    }

    void ScanValues(RegistryKey root, string path, string source, string category, bool requireHit)
    {
        using var k = Reg.Open(root, path);
        if (k == null) return;
        foreach (var name in k.GetValueNames())
        {
            object? v; try { v = k.GetValue(name); } catch { continue; }
            string text = v switch { string s => s, string[] a => string.Join(" | ", a), byte[] b => Reg.BinaryToText(b), null => "", _ => v.ToString()! };
            if (requireHit && !Markers.Hit(text)) continue;
            if (name == "Count" || name == "NextInstance" || name == "INITSTARTFAILED") continue;
            Add(source, category, path, name, text, Native.LastWrite(k));
        }
    }

    // ═══════════════ 2. SOFTWARE 하이브 ═══════════════

    public void ScanSoftwareHive(RegistryKey swRoot, string source)
    {
        ScanTree(swRoot, @"Microsoft\Windows Portable Devices\Devices", source, "WindowsPortableDevices", 1, requireHit: false);
        ScanTree(swRoot, @"Microsoft\Windows NT\CurrentVersion\EMDMgmt", source, "EMDMgmt (ReadyBoost 볼륨 기록)", 1, requireHit: false);
        ScanTree(swRoot, @"Microsoft\Windows Search\VolumeInfoCache", source, "WindowsSearch\\VolumeInfoCache", 1, requireHit: false);
    }

    // ═══════════════ 3. 사용자 하이브 (MountPoints2 등) ═══════════════

    public void ScanUserHive(RegistryKey userRoot, string userLabel, string source)
    {
        using var mp = Reg.Open(userRoot, @"Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2");
        if (mp != null)
        {
            foreach (var sub in Reg.SubKeys(mp))
            {
                using var sk = Reg.Open(mp, sub);
                if (sk == null) continue;
                string detail;
                if (sub.StartsWith('{'))
                    detail = _usbVolumeGuids.Contains(sub)
                        ? "볼륨 GUID — MountedDevices에서 USB 장치로 확인됨"
                        : "볼륨 GUID — MountedDevices에 대응 항목 없음 (삭제되었거나 다른 볼륨)";
                else if (sub.Length == 1 && char.IsLetter(sub[0]))
                {
                    if (string.Equals(sub + ":", _sysDrive, StringComparison.OrdinalIgnoreCase)) continue;
                    detail = "드라이브 문자 마운트 기록";
                }
                else if (sub.StartsWith("##")) continue; // 네트워크 공유
                else detail = "";
                Add(source, $"MountPoints2 [{userLabel}]", $@"{userLabel}\...\Explorer\MountPoints2\{sub}", sub, detail, Native.LastWrite(sk));
            }
        }

    }

    public void ScanLiveUsers(string source)
    {
        using var hku = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
        foreach (var sid in Reg.SubKeys(hku))
        {
            if (sid.EndsWith("_Classes", StringComparison.OrdinalIgnoreCase)) continue;
            if (!sid.StartsWith("S-1-5-21-", StringComparison.OrdinalIgnoreCase)) continue; // 일반 사용자 계정만
            using var uk = Reg.Open(hku, sid);
            if (uk != null) ScanUserHive(uk, sid, source);
        }
    }

    public void ScanOfflineUsers(string source)
    {
        string usersDir = Path.Combine(_sysDrive + "\\", "Users");
        if (!Directory.Exists(usersDir)) return;
        foreach (var dir in Directory.EnumerateDirectories(usersDir))
        {
            string hive = Path.Combine(dir, "NTUSER.DAT");
            if (!File.Exists(hive)) continue;
            using var loaded = LoadedHive.Load(hive, copyFirst: true, _r);
            if (loaded == null) continue; // 로그인 중인 사용자는 잠겨 있음 → 라이브 HKU에서 이미 검사됨
            ScanUserHive(loaded.Root, Path.GetFileName(dir) + " (offline)", source);
        }
    }

    // ═══════════════ 4. Amcache ═══════════════

    public void ScanAmcacheHive(RegistryKey amRoot, string source)
    {
        using var pnp = Reg.Open(amRoot, @"Root\InventoryDevicePnp");
        if (pnp != null)
            foreach (var sub in Reg.SubKeys(pnp))
            {
                using var sk = Reg.Open(pnp, sub);
                if (sk == null) continue;
                string dump = Reg.DumpValues(sk);
                if (Markers.Hit(sub) || Markers.Hit(dump))
                    Add(source, "Amcache\\InventoryDevicePnp", @"Root\InventoryDevicePnp\" + sub, sub, dump, Native.LastWrite(sk));
            }

        using var cont = Reg.Open(amRoot, @"Root\InventoryDeviceContainer");
        if (cont != null)
            foreach (var sub in Reg.SubKeys(cont))
            {
                using var sk = Reg.Open(cont, sub);
                if (sk == null) continue;
                string dump = Reg.DumpValues(sk);
                if (Markers.Hit(dump) || dump.Contains("Storage", StringComparison.OrdinalIgnoreCase) || dump.Contains("USB", StringComparison.OrdinalIgnoreCase))
                    Add(source, "Amcache\\InventoryDeviceContainer", @"Root\InventoryDeviceContainer\" + sub, sub, dump, Native.LastWrite(sk));
            }
    }

    public void ScanLiveAmcache()
    {
        string hive = Path.Combine(_windows, "appcompat", "Programs", "Amcache.hve");
        if (!File.Exists(hive)) { _r.Notes.Add("Amcache.hve 파일이 없습니다."); return; }
        using var loaded = LoadedHive.Load(hive, copyFirst: true, _r);
        if (loaded == null)
        {
            _r.Notes.Add("Amcache.hve는 시스템이 잠그고 있어 라이브 복사에 실패했습니다. --shadow 옵션으로 섀도 복사본에서 검사하세요.");
            return;
        }
        ScanAmcacheHive(loaded.Root, "Amcache(live)");
    }

    // ═══════════════ 5. setupapi 로그 ═══════════════

    public void ScanSetupApiLogs()
    {
        var files = new List<string>();
        foreach (var dir in new[] { Path.Combine(_windows, "INF"), _windows })
        {
            if (!Directory.Exists(dir)) continue;
            try { files.AddRange(Directory.EnumerateFiles(dir, "setupapi*.log")); } catch { }
        }
        if (files.Count == 0) { _r.Notes.Add("setupapi 로그 파일이 하나도 없습니다 (삭제되었을 가능성)."); return; }

        var tsRegex = new Regex(@"Section start (\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}\.\d+)", RegexOptions.Compiled);
        foreach (var f in files)
        {
            int hits = 0;
            try
            {
                string? pendingHeader = null; int pendingLine = 0;
                int lineNo = 0;
                foreach (var line in File.ReadLines(f, Encoding.UTF8))
                {
                    lineNo++;
                    if (pendingHeader != null)
                    {
                        var m = tsRegex.Match(line);
                        DateTime? ts = null;
                        if (m.Success && DateTime.TryParseExact(m.Groups[1].Value, "yyyy/MM/dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) ts = dt;
                        Add("SetupAPI", Path.GetFileName(f), $"{f}:{pendingLine}", "Device Install", pendingHeader.Trim(), ts);
                        hits++;
                        pendingHeader = null;
                        continue;
                    }
                    // ">>>  [Device Install (Hardware initiated) - USBSTOR\Disk&Ven_...]" 형태의 섹션 헤더만 수집
                    if (line.StartsWith(">>>  [", StringComparison.Ordinal) && Markers.Hit(line)) { pendingHeader = line; pendingLine = lineNo; }
                }
            }
            catch (Exception ex) { _r.Notes.Add($"setupapi 로그 읽기 실패 {f}: {ex.Message}"); }
            if (hits == 0) _r.Notes.Add($"{Path.GetFileName(f)}: 외장 장치 관련 라인 0건");
        }
    }

    // ═══════════════ 6. 이벤트 로그 (wevtutil) ═══════════════

    static readonly (string Channel, string Note)[] Channels =
    {
        ("Microsoft-Windows-DriverFrameworks-UserMode/Operational", "USB 장치 연결/해제 (2003/2100/2102 등)"),
        ("Microsoft-Windows-Kernel-PnP/Configuration", "PnP 장치 구성 (400/410/420/430)"),
        ("Microsoft-Windows-Partition/Diagnostic", "디스크 연결 (1006, ParentId에 USB 경로)"),
        ("Microsoft-Windows-Storsvc/Diagnostic", "저장 장치 서비스 (1001)"),
        ("Microsoft-Windows-Storage-ClassPnP/Operational", "저장 장치 클래스 드라이버"),
        ("Microsoft-Windows-Ntfs/Operational", "NTFS 볼륨 마운트"),
        ("Microsoft-Windows-WPD-MTPClassDriver/Operational", "MTP/WPD 장치"),
        ("System", "UserPnp 20001/20003, 서비스 시작 등"),
    };

    public void ScanEventLogs()
    {
        XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
        foreach (var (channel, note) in Channels)
        {
            string xml;
            try { xml = ProcUtil.Run("wevtutil.exe", $"qe \"{channel}\" /c:{_o.MaxEvents} /rd:true /f:xml", out int rc, out string err);
                  if (rc != 0) { _r.Notes.Add($"[EventLog] {channel}: wevtutil 실패 ({err.Trim()})"); continue; } }
            catch (Exception ex) { _r.Notes.Add($"[EventLog] {channel}: {ex.Message}"); continue; }

            var chunks = Regex.Split(xml, @"(?=<Event\s)").Where(c => c.TrimStart().StartsWith("<Event", StringComparison.Ordinal)).ToList();
            if (chunks.Count == 0) { _r.Notes.Add($"[EventLog] {channel}: 이벤트 0건 — 로그가 비활성화되었거나 삭제되었을 가능성 ({note})"); continue; }

            int hits = 0;
            foreach (var chunk in chunks)
            {
                if (!Markers.Hit(chunk)) continue;
                int end = chunk.LastIndexOf("</Event>", StringComparison.Ordinal);
                string one = end > 0 ? chunk[..(end + 8)] : chunk;
                string id = "?", provider = "?", data; DateTime? ts = null;
                try
                {
                    var e = XElement.Parse(one);
                    var sys = e.Element(ns + "System");
                    id = sys?.Element(ns + "EventID")?.Value ?? "?";
                    provider = sys?.Element(ns + "Provider")?.Attribute("Name")?.Value ?? "?";
                    var t = sys?.Element(ns + "TimeCreated")?.Attribute("SystemTime")?.Value;
                    if (t != null && DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dt)) ts = dt.ToLocalTime();
                    var ed = e.Element(ns + "EventData");
                    IEnumerable<XElement> leaves = ed != null
                        ? ed.Elements(ns + "Data")
                        : (e.Element(ns + "UserData")?.Descendants().Where(x => !x.HasElements) ?? Enumerable.Empty<XElement>());
                    data = string.Join("; ", leaves.Select(d => $"{d.Attribute("Name")?.Value ?? d.Name.LocalName}={d.Value}"));
                }
                catch { data = Regex.Replace(one, @"\s+", " "); }
                if (data.Length > 600) data = data[..600] + "…";
                Add("EventLog", channel, channel, $"EventID {id} ({provider})", data, ts);
                hits++;
            }
            if (hits == 0) _r.Notes.Add($"[EventLog] {channel}: 최근 {chunks.Count}건 중 외장 장치 관련 0건");
        }

        // 로그 삭제 흔적
        foreach (var (ch, eid) in new[] { ("System", 104), ("Security", 1102) })
        {
            try
            {
                string xml = ProcUtil.Run("wevtutil.exe", $"qe {ch} /q:\"*[System[(EventID={eid})]]\" /c:50 /rd:true /f:xml", out int rc, out _);
                if (rc != 0) continue;
                foreach (Match m in Regex.Matches(xml, @"SystemTime='([^']+)'"))
                {
                    DateTime? ts = DateTime.TryParse(m.Groups[1].Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dt) ? dt.ToLocalTime() : null;
                    Add("EventLog", "로그 삭제 흔적", ch, $"EventID {eid}", ch == "System" ? "이벤트 로그가 지워짐 (104)" : "보안 로그가 지워짐 (1102)", ts);
                }
            }
            catch { }
        }
    }

    // ═══════════════ 7. LNK (바로 가기) 볼륨 정보 ═══════════════

    public void ScanLnkFiles()
    {
        string usersDir = Path.Combine(_sysDrive + "\\", "Users");
        if (!Directory.Exists(usersDir)) return;
        var ansi = ProcUtil.AnsiEncoding;
        foreach (var userDir in Directory.EnumerateDirectories(usersDir))
        {
            string user = Path.GetFileName(userDir);
            var dirs = new[]
            {
                Path.Combine(userDir, @"AppData\Roaming\Microsoft\Windows\Recent"),
                Path.Combine(userDir, @"AppData\Roaming\Microsoft\Office\Recent"),
                Path.Combine(userDir, "Desktop"),
            };
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) continue;
                IEnumerable<string> files;
                try { files = Directory.EnumerateFiles(d, "*.lnk", SearchOption.TopDirectoryOnly); } catch { continue; }
                foreach (var f in files)
                {
                    try
                    {
                        var info = LnkParser.Parse(File.ReadAllBytes(f), ansi);
                        if (info == null) continue;
                        bool external = info.DriveType == 2 /* removable */
                            || (info.DriveType == 3 && info.LocalBasePath.Length >= 2 && !info.LocalBasePath.StartsWith(_sysDrive, StringComparison.OrdinalIgnoreCase));
                        if (!external) continue;
                        string typeName = info.DriveType switch { 2 => "이동식", 3 => "고정(시스템 외 드라이브)", 5 => "CD-ROM", _ => info.DriveType.ToString() };
                        Add("LNK", $"Recent/Desktop [{user}]", f, Path.GetFileName(f),
                            $"DriveType={typeName}; Serial={info.Serial:X8}; Label={info.Label}; Path={info.LocalBasePath}",
                            File.GetLastWriteTime(f));
                    }
                    catch { }
                }
            }
        }
    }

    // ═══════════════ 7b. 점프리스트 (AutomaticDestinations / CustomDestinations) ═══════════════
    // 포렌식 조사가 외장 볼륨 증거를 실제로 찾아낸 핵심 위치. 파일 내부의 LNK
    // 스트림을 CLSID 로 카빙해, 외장 볼륨(REMOVABLE 또는 시스템 드라이브 외)을
    // 참조하는 스트림에서 볼륨 시리얼/레이블/경로를 추출한다.

    static readonly byte[] LnkClsidSig =
    {
        0x4C,0x00,0x00,0x00,0x01,0x14,0x02,0x00,0x00,0x00,0x00,0x00,
        0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46
    };

    public void ScanJumpLists()
    {
        string usersDir = Path.Combine(_sysDrive + "\\", "Users");
        if (!Directory.Exists(usersDir)) return;
        var ansi = ProcUtil.AnsiEncoding;

        foreach (var userDir in Directory.EnumerateDirectories(usersDir))
        {
            string user = Path.GetFileName(userDir);
            foreach (var (sub, pattern) in new[]
            {
                ("AutomaticDestinations", "*.automaticDestinations-ms"),
                ("CustomDestinations", "*.customDestinations-ms"),
            })
            {
                string dir = Path.Combine(userDir, @"AppData\Roaming\Microsoft\Windows\Recent", sub);
                if (!Directory.Exists(dir)) continue;
                IEnumerable<string> files;
                try { files = Directory.EnumerateFiles(dir, pattern); } catch { continue; }

                foreach (var f in files)
                {
                    byte[] data;
                    try { data = File.ReadAllBytes(f); } catch { continue; }

                    int streamNo = 0;
                    int off = 0;
                    while (true)
                    {
                        int idx = IndexOf(data, LnkClsidSig, off);
                        if (idx < 0) break;
                        off = idx + 4;
                        streamNo++;

                        var info = LnkParser.ParseAt(data, idx, ansi);
                        if (info == null) continue;
                        bool external = info.DriveType == 2
                            || (info.DriveType == 3 && info.LocalBasePath.Length >= 2
                                && !info.LocalBasePath.StartsWith(_sysDrive, StringComparison.OrdinalIgnoreCase));
                        if (!external) continue;

                        string typeName = info.DriveType switch { 2 => "이동식", 3 => "고정(시스템 외)", 5 => "CD-ROM", _ => info.DriveType.ToString() };
                        Add("JumpList", $"{sub} [{user}]", f, Path.GetFileName(f),
                            $"Stream#{streamNo}; DriveType={typeName}; Serial={info.Serial:X8}; Label={info.Label}; Path={info.LocalBasePath}",
                            File.GetLastWriteTime(f));
                    }
                }
            }
        }
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

    // ═══════════════ 7c. Shellbags (UsrClass + NTUSER BagMRU) ═══════════════
    // 폴더 보기 설정. 외장 드라이브 노드(E:, F: 등)가 남는다. 볼륨 시리얼은
    // 대개 없지만 "어떤 드라이브 문자를 열었는가" 는 드러난다.

    public void ScanShellbags(RegistryKey userRoot, string userLabel, string source)
    {
        // UsrClass 측: Software\Classes\Local Settings\...\Shell\BagMRU
        WalkBagMru(userRoot, @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU",
                   userLabel, source, "UsrClass");
        // NTUSER 측: Software\Microsoft\Windows\Shell\BagMRU
        WalkBagMru(userRoot, @"Software\Microsoft\Windows\Shell\BagMRU",
                   userLabel, source, "NTUSER");
    }

    void WalkBagMru(RegistryKey root, string path, string userLabel, string source, string hive)
    {
        using var key = Reg.Open(root, path);
        if (key == null) return;
        Walk(key, path, 0);

        void Walk(RegistryKey k, string keyPath, int depth)
        {
            if (depth > 12) return;
            foreach (var valName in SafeValueNames(k))
            {
                if (!int.TryParse(valName, out _)) continue;
                if (k.GetValue(valName) is not byte[] b) continue;
                // shell item 에서 드라이브 문자 추출 (type 0x2x = volume)
                if (b.Length >= 3 && (b[2] & 0x70) == 0x20)
                {
                    string txt = Encoding.Latin1.GetString(b, 3, Math.Min(23, b.Length - 3));
                    int z = txt.IndexOf('\0'); if (z >= 0) txt = txt[..z];
                    if (txt.Length >= 2 && txt[1] == ':')
                    {
                        string drv = txt[..Math.Min(3, txt.Length)].ToUpperInvariant();
                        string sysd = _sysDrive.ToUpperInvariant();
                        // 시스템 드라이브(C:) 와 데이터 D: 는 제외하고 외장 후보만
                        if (!drv.StartsWith(sysd) && !drv.StartsWith("D:"))
                            Add(source, $"Shellbag[{hive}] [{userLabel}]", keyPath, drv,
                                $"드라이브 노드={drv}", Native.LastWrite(k));
                    }
                }
            }
            foreach (var sub in Reg.SubKeys(k))
            {
                using var sk = Reg.Open(k, sub);
                if (sk != null) Walk(sk, keyPath + "\\" + sub, depth + 1);
            }
        }
    }

    static IEnumerable<string> SafeValueNames(RegistryKey k)
    {
        try { return k.GetValueNames(); } catch { return Array.Empty<string>(); }
    }

    // ═══════════════ 7d. MRU 계열 ═══════════════
    // ComDlg32 열기/저장 대화상자, TypedPaths, WordWheelQuery 등에서 외장 경로 흔적.

    public void ScanMru(RegistryKey userRoot, string userLabel, string source)
    {
        // 값 데이터(PIDL/문자열)에 드라이브 문자가 들어있는지 훑는다.
        string[] keys =
        {
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU",
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\LastVisitedPidlMRU",
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths",
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\WordWheelQuery",
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU",
        };
        foreach (var path in keys)
        {
            using var key = Reg.Open(userRoot, path);
            if (key == null) continue;
            ScanMruKey(key, path, userLabel, source, 0);
        }
    }

    void ScanMruKey(RegistryKey key, string path, string userLabel, string source, int depth)
    {
        if (depth > 4) return;
        string sysd = _sysDrive.ToUpperInvariant();
        foreach (var valName in SafeValueNames(key))
        {
            object v; try { v = key.GetValue(valName); } catch { continue; }
            string text = v switch
            {
                string s => s,
                byte[] b => ExtractPaths(b),
                _ => ""
            };
            if (string.IsNullOrEmpty(text)) continue;
            // 시스템/데이터 외 드라이브 문자 참조만
            foreach (Match m in Regex.Matches(text, @"([D-Zd-z]):\\"))
            {
                string drv = (m.Groups[1].Value + ":").ToUpperInvariant();
                if (drv.StartsWith(sysd) || drv == "D:") continue;
                Add(source, $"MRU [{userLabel}]", path, valName,
                    $"경로 참조={m.Value}…", Native.LastWrite(key));
                break;
            }
        }
        foreach (var sub in Reg.SubKeys(key))
        {
            using var sk = Reg.Open(key, sub);
            if (sk != null) ScanMruKey(sk, path + "\\" + sub, userLabel, source, depth + 1);
        }
    }

    // 바이너리 PIDL 등에서 UTF-16/ANSI 경로 문자열을 뽑아 이어붙인다.
    static string ExtractPaths(byte[] b)
    {
        var sb = new StringBuilder();
        // UTF-16LE 경로 후보
        try
        {
            string u = Encoding.Unicode.GetString(b);
            foreach (Match m in Regex.Matches(u, @"[A-Za-z]:\\[^\x00]{0,120}"))
                sb.Append(m.Value).Append(' ');
        }
        catch { }
        // ANSI 경로 후보
        try
        {
            string a = Encoding.Latin1.GetString(b);
            foreach (Match m in Regex.Matches(a, @"[A-Za-z]:\\[ -~]{0,120}"))
                sb.Append(m.Value).Append(' ');
        }
        catch { }
        return sb.ToString();
    }

    public void ScanActivityForLiveUsers(string source)
    {
        using var hku = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
        foreach (var sid in Reg.SubKeys(hku))
        {
            if (!sid.StartsWith("S-1-5-21-", StringComparison.OrdinalIgnoreCase)) continue;
            if (sid.EndsWith("_Classes", StringComparison.OrdinalIgnoreCase)) continue;
            using var uk = Reg.Open(hku, sid);
            if (uk == null) continue;
            ScanShellbags(uk, sid, source);
            ScanMru(uk, sid, source);
            // UsrClass 는 별도 하이브(HKU\<sid>_Classes)로 로드돼 있기도 하다.
            using var classes = Reg.Open(hku, sid + "_Classes");
            if (classes != null)
                WalkBagMru(classes, @"Local Settings\Software\Microsoft\Windows\Shell\BagMRU",
                           sid, source, "UsrClass");
        }
    }

    // ═══════════════ 7e. Prefetch ═══════════════
    // 조사가 우리를 특정한 결정적 위치. .pf 는 MAM(XPRESS Huffman) 압축이라
    // ntdll RtlDecompressBufferEx 로 해제한 뒤, 내부의 \VOLUME{...-시리얼}\경로
    // 문자열에서 외장 볼륨 참조를 찾는다. 또한 파일명이 삭제/암호화 도구면
    // (DriveCleanup/sdelete/wevtutil…) 그 실행 흔적도 보고한다.

    static readonly Regex PrefetchInterest = new(
        @"DRIVECLEANUP|USBDEVIEW|USBOBLIVION|SDELETE|CIPHER|WEVTUTIL|VSSADMIN|PNPUTIL|DEVCON|" +
        @"BDEUNLOCK|BITLOCKER|MANAGE-BDE|FVENOTIFY|VERACRYPT|TRUECRYPT|DISKPART|FREEFILESYNC|" +
        @"ROBOCOPY|TERACOPY|FASTCOPY|RUFUS|ETCHER|CCLEANER|BLEACHBIT|ERASER|PRIVAZER|DBAN|" +
        @"IMDISK|DAEMON|WINCDEMU|OSFMOUNT|VHDATTACH",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public void ScanPrefetch()
    {
        string dir = Path.Combine(_windows, "Prefetch");
        if (!Directory.Exists(dir)) { _r.Notes.Add("Prefetch 폴더 없음 또는 접근 불가"); return; }
        string[] files;
        try { files = Directory.GetFiles(dir, "*.pf"); }
        catch (Exception ex) { _r.Notes.Add("Prefetch 목록 실패: " + ex.Message); return; }
        if (files.Length == 0) { _r.Notes.Add("Prefetch .pf 파일 0건 (비활성 또는 이미 삭제됨)"); return; }

        int ok = 0, fail = 0;
        foreach (var f in files)
        {
            string name = Path.GetFileName(f);

            // 파일명만으로 의미있는 삭제/암호화 도구
            if (PrefetchInterest.IsMatch(name))
                Add("PrefetchInterest", "Prefetch(도구 실행)", f, name,
                    "삭제/암호화/마운트 도구 실행 흔적", File.GetLastWriteTime(f));

            byte[] raw;
            try { raw = File.ReadAllBytes(f); } catch { fail++; continue; }
            byte[] data = MamDecompress(raw);
            if (data == null) { fail++; continue; }
            ok++;

            // 내부 UTF-16LE 볼륨 경로: \VOLUME{01da...-<시리얼8hex>}\...
            string text;
            try { text = Encoding.Unicode.GetString(data); } catch { continue; }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches(text, @"\\VOLUME\{[0-9a-fA-F]{16}-([0-9a-fA-F]{8})\}(\\[^\x00]{0,80})?", RegexOptions.IgnoreCase))
            {
                string serialHex = m.Groups[1].Value.ToUpperInvariant();
                string serial = serialHex.Length == 8 ? $"{serialHex[..4]}-{serialHex[4..]}" : serialHex;
                // 시스템/데이터 볼륨 시리얼은 제외 (내장). 나머지는 외장 후보.
                if (_internalVolSerials.Contains(serial)) continue;
                string path = m.Groups[2].Success ? m.Groups[2].Value : "";
                string key = serial + path;
                if (!seen.Add(key)) continue;
                Add("Prefetch", "Prefetch(외장 볼륨 참조)", f, name,
                    $"VolumeSerial={serial}; Path={path}", File.GetLastWriteTime(f));
            }
        }
        _r.Notes.Add($"Prefetch 파싱 성공 {ok} / 실패 {fail} / 전체 {files.Length}");
    }

    // 내장 볼륨 시리얼(C:/D: 등) 을 미리 수집해 Prefetch 외장 판별에 쓴다.
    readonly HashSet<string> _internalVolSerials = new(StringComparer.OrdinalIgnoreCase);

    public void CollectInternalVolumeSerials()
    {
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (!d.IsReady) continue;
                // 고정 디스크만 내장으로 간주 (이동식/네트워크 제외)
                if (d.DriveType != DriveType.Fixed) continue;
                // 볼륨 시리얼은 Win32 API 로 얻는다.
                string serial = Native.GetVolumeSerial(d.RootDirectory.FullName);
                if (!string.IsNullOrEmpty(serial)) _internalVolSerials.Add(serial);
            }
        }
        catch { }
    }

    // ── MAM(XPRESS Huffman) 압축 해제 ──
    static byte[] MamDecompress(byte[] buf)
    {
        // 압축 안 된 구형 .pf (SCCA) 는 그대로 반환.
        if (buf.Length >= 8 && buf[0] == (byte)'M' && buf[1] == (byte)'A' && buf[2] == (byte)'M' && buf[3] == 0x04)
        {
            uint outSize = BitConverter.ToUInt32(buf, 4);
            if (outSize == 0 || outSize > 64 * 1024 * 1024) return null;
            const ushort FMT = 4; // COMPRESSION_FORMAT_XPRESS_HUFF
            uint ws = 0, frag = 0;
            if (Native.RtlGetCompressionWorkSpaceSize(FMT, ref ws, ref frag) != 0) return null;
            byte[] work = new byte[ws];
            byte[] outBuf = new byte[outSize];
            byte[] src = new byte[buf.Length - 8];
            Array.Copy(buf, 8, src, 0, src.Length);
            uint final = 0;
            int st = Native.RtlDecompressBufferEx(FMT, outBuf, (uint)outBuf.Length, src, (uint)src.Length, ref final, work);
            if (st != 0) return null;
            byte[] result = new byte[final];
            Array.Copy(outBuf, result, (int)final);
            return result;
        }
        // 헤더가 SCCA(비압축)면 그대로.
        if (buf.Length >= 8 && buf[4] == (byte)'S' && buf[5] == (byte)'C' && buf[6] == (byte)'C' && buf[7] == (byte)'A')
            return buf;
        return null;
    }

    // ═══════════════ 8. 볼륨 섀도 복사본 ═══════════════

    public void ScanShadowCopies()
    {
        // vssadmin 은 "항목 없음" 같은 메시지를 stdout 에 쓰고 0이 아닌 코드로 끝나기도 하므로 종료 코드와 무관하게 출력을 파싱한다.
        string listing, err = "";
        try { listing = ProcUtil.Run("vssadmin.exe", "list shadows", out _, out err); }
        catch (Exception ex) { _r.Notes.Add($"vssadmin 실행 실패: {ex.Message}"); return; }

        var devs = Regex.Matches(listing, @"\\\\\?\\GLOBALROOT\\Device\\HarddiskVolumeShadowCopy\d+", RegexOptions.IgnoreCase)
                        .Select(m => m.Value).Distinct().ToList();
        if (devs.Count == 0)
        {
            string msg = (listing + " " + err).Trim();
            msg = Regex.Replace(msg, @"\s+", " ");
            _r.Notes.Add("볼륨 섀도 복사본이 없습니다. vssadmin 출력: " + (msg.Length > 200 ? msg[..200] + "…" : msg));
            return;
        }
        _r.Notes.Add($"섀도 복사본 {devs.Count}개 검사: {string.Join(", ", devs.Select(d => d[(d.LastIndexOf('\\') + 1)..]))}");

        foreach (var dev in devs)
        {
            string label = dev[(dev.LastIndexOf('\\') + 1)..];
            string src = $"ShadowCopy[{label}]";
            var winRel = _windows.Length > 3 ? _windows[3..] : "Windows"; // "C:\Windows" → "Windows"

            TryHive(Path.Combine(dev, winRel, @"System32\config\SYSTEM"), h => ScanSystemHive(h, src + "/SYSTEM"));
            TryHive(Path.Combine(dev, winRel, @"System32\config\SOFTWARE"), h => ScanSoftwareHive(h, src + "/SOFTWARE"));
            TryHive(Path.Combine(dev, winRel, @"appcompat\Programs\Amcache.hve"), h => ScanAmcacheHive(h, src + "/Amcache"));

            void TryHive(string path, Action<RegistryKey> scan)
            {
                if (!File.Exists(path)) { _r.Notes.Add($"[{src}] 없음: {path}"); return; }
                using var loaded = LoadedHive.Load(path, copyFirst: true, _r);
                if (loaded == null) return;
                try { scan(loaded.Root); } catch (Exception ex) { _r.Notes.Add($"[{src}] 스캔 오류 {path}: {ex.Message}"); }
            }
        }
    }
}

// ───────────────────────── LNK 파서 (최소 구현) ─────────────────────────

sealed record LnkInfo(uint DriveType, uint Serial, string Label, string LocalBasePath);

static class LnkParser
{
    public static LnkInfo? Parse(byte[] b, Encoding ansi) => ParseAt(b, 0, ansi);

    // base 오프셋에서 시작하는 LNK 구조를 파싱한다 (점프리스트 내부 스트림용).
    public static LnkInfo? ParseAt(byte[] b, int baseOffset, Encoding ansi)
    {
        if (baseOffset < 0 || baseOffset + 0x4C > b.Length) return null;
        if (BitConverter.ToUInt32(b, baseOffset) != 0x4C) return null;
        uint flags = BitConverter.ToUInt32(b, baseOffset + 0x14);
        int pos = baseOffset + 0x4C;
        if ((flags & 0x1) != 0) // HasLinkTargetIDList
        {
            if (pos + 2 > b.Length) return null;
            pos += 2 + BitConverter.ToUInt16(b, pos);
        }
        if ((flags & 0x2) == 0) return null; // HasLinkInfo
        if (pos + 28 > b.Length) return null;
        uint liFlags = BitConverter.ToUInt32(b, pos + 8);
        uint volOff = BitConverter.ToUInt32(b, pos + 12);
        uint baseOff = BitConverter.ToUInt32(b, pos + 16);
        if ((liFlags & 0x1) == 0) return null; // VolumeIDAndLocalBasePath
        int vp = pos + (int)volOff;
        if (vp < 0 || vp + 16 > b.Length) return null;
        uint driveType = BitConverter.ToUInt32(b, vp + 4);
        uint serial = BitConverter.ToUInt32(b, vp + 8);
        uint labelOff = BitConverter.ToUInt32(b, vp + 12);
        string label = ReadZ(b, vp + (int)labelOff, ansi);
        string basePath = ReadZ(b, pos + (int)baseOff, ansi);
        return new LnkInfo(driveType, serial, label, basePath);
    }

    static string ReadZ(byte[] b, int start, Encoding enc)
    {
        if (start < 0 || start >= b.Length) return "";
        int end = start;
        while (end < b.Length && b[end] != 0) end++;
        try { return enc.GetString(b, start, end - start); } catch { return ""; }
    }
}

// ───────────────────────── 프로세스 헬퍼 ─────────────────────────

static class ProcUtil
{
    static ProcUtil() { try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { } }

    public static Encoding AnsiEncoding
    {
        get { try { return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage); } catch { return Encoding.UTF8; } }
    }

    static Encoding OemEncoding
    {
        get { try { return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage); } catch { return Encoding.UTF8; } }
    }

    public static string Run(string exe, string args, out int exitCode, out string stderr)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true,
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"{exe} 실행 실패");
        var errSb = new StringBuilder();
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) errSb.AppendLine(e.Data); };
        p.BeginErrorReadLine();
        using var ms = new MemoryStream();
        p.StandardOutput.BaseStream.CopyTo(ms);
        p.WaitForExit();
        exitCode = p.ExitCode;
        stderr = errSb.ToString();
        return Decode(ms.ToArray());
    }

    static string Decode(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        try { return new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { return OemEncoding.GetString(bytes); }
    }
}

// ───────────────────────── 진입점 ─────────────────────────

// ───────────────────────── 파사드: Ashes 내장용 ─────────────────────────
// UsbHistoryAudit 콘솔 앱의 스캔 로직을 그대로 사용하되, 콘솔 출력 대신
// 문자열 콜백(onOutput)으로 로그를 흘리고 결과를 JSON 파일로 저장한다.
// "--all -v --out name.json" 에 해당하는 동작을 RunAndExport 하나로 제공.
public static class UsbAuditor
{
    /// <summary>
    /// 외장/이동식 장치 흔적을 스캔해서 JSON 파일로 저장한다.
    /// --all(섀도 복사본 + 오프라인 사용자 하이브) + 전체 항목 수집에 해당.
    /// </summary>
    /// <param name="outPath">저장할 JSON 파일 경로</param>
    /// <param name="onOutput">진행 로그 콜백 (UI 로그창에 연결)</param>
    /// <returns>수집된 흔적 개수</returns>
    public static int RunAndExport(string outPath, Action<string> onOutput)
    {
        void Log(string s) => onOutput?.Invoke(s);

        var opt = new Options { Shadow = true, OfflineUsers = true, Verbose = true, OutPath = outPath };
        var report = new Report { Args = "--all -v --out " + outPath };
        var sw = Stopwatch.StartNew();

        if (!OperatingSystem.IsWindows())
        {
            Log("[오류] Windows 전용 기능입니다.");
            return 0;
        }

        bool bk = Native.EnablePrivilege("SeBackupPrivilege");
        bool rs = Native.EnablePrivilege("SeRestorePrivilege");
        if (!bk || !rs)
            report.Notes.Add("SeBackup/SeRestore 권한 활성화 실패 — 관리자 권한 확인 필요. 오프라인 하이브/섀도 복사본 로드가 실패할 수 있습니다.");

        var a = new Auditor(report, opt);

        Log("== 외장 드라이브 흔적 분석 시작 ==");

        Step("레지스트리 SYSTEM 하이브", () =>
        {
            using var sys = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SYSTEM");
            if (sys != null) a.ScanSystemHive(sys, "Registry(live)");
        });
        Step("레지스트리 SOFTWARE 하이브", () =>
        {
            using var sw2 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE");
            if (sw2 != null) a.ScanSoftwareHive(sw2, "Registry(live)");
        });
        Step("사용자 하이브 (로그인 중)", () => a.ScanLiveUsers("Registry(live)"));
        Step("사용자 하이브 (오프라인 NTUSER.DAT)", () => a.ScanOfflineUsers("Registry(offline)"));
        Step("Amcache", () => a.ScanLiveAmcache());
        Step("setupapi 로그", () => a.ScanSetupApiLogs());
        Step("이벤트 로그", () => a.ScanEventLogs());
        Step("LNK 파일", () => a.ScanLnkFiles());
        Step("점프리스트", () => a.ScanJumpLists());
        Step("Shellbags / MRU (활동 계층)", () => a.ScanActivityForLiveUsers("Activity(live)"));
        Step("Prefetch", () => { a.CollectInternalVolumeSerials(); a.ScanPrefetch(); });
        Step("볼륨 섀도 복사본", () => a.ScanShadowCopies());

        sw.Stop();

        // 요약을 로그로 출력
        Log("");
        Log("──────── 요약 ────────");
        Log($"호스트: {report.Host}   실행: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}   소요: {sw.Elapsed.TotalSeconds:F1}s");
        Log($"총 흔적: {report.Findings.Count}건   판정: {(report.Findings.Count == 0 ? "CLEAN (흔적 없음)" : "FOUND (흔적 남음)")}");
        foreach (var g in report.Findings.GroupBy(f => f.Source).OrderBy(g => g.Key))
        {
            Log($"■ {g.Key}: {g.Count()}건");
            foreach (var c in g.GroupBy(f => f.Category).OrderBy(c => c.Key))
                Log($"   - {c.Key}: {c.Count()}건");
        }
        if (report.Notes.Count > 0)
        {
            Log("──────── 참고 ────────");
            foreach (var n in report.Notes) Log("  · " + n);
        }

        // JSON 저장
        try
        {
            string json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(outPath, json, new UTF8Encoding(false));
            Log("");
            Log($"[저장 완료] {outPath}");
        }
        catch (Exception ex)
        {
            Log($"[저장 실패] {ex.Message}");
        }

        return report.Findings.Count;

        void Step(string name, Action act)
        {
            int before = report.Findings.Count;
            try { act(); Log($"[OK] {name} — {report.Findings.Count - before}건"); }
            catch (Exception ex) { Log($"[!!] {name} — 오류: {ex.Message}"); report.Notes.Add($"[{name}] 오류: {ex}"); }
        }
    }
}
}
