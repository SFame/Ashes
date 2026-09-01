using System;
using System.Threading;
using System.Windows.Forms;

namespace Ashes
{
    public partial class DriveCleanupForm : Form
    {
        private CancellationTokenSource _cts;

        public DriveCleanupForm()
        {
            InitializeComponent();

            btnScan.Click += async (s, e) => await RunAsync(testMode: true).ConfigureAwait(false);
            btnClean.Click += BtnClean_Click;
            btnCancel.Click += (s, e) => _cts?.Cancel();

            // 확장 정리 (이벤트 로그, setupapi, 연결된 장치, VolumeInfoCache, MountPoints2)
            btnExtScan.Click += async (s, e) => await RunExtendedAsync(dryRun: true).ConfigureAwait(false);
            btnExtRun.Click  += BtnExtRun_Click;

            // 현재 외장 드라이브 흔적 분석 → JSON 파일로 내보내기
            btnAudit.Click += BtnAudit_Click;

            // "전체" and the per-type boxes are mutually exclusive: ticking 전체
            // clears the specific ones, and ticking any specific one clears 전체.
            chkAll.CheckedChanged += ChkAll_CheckedChanged;
            foreach (var box in TypeBoxes())
                box.CheckedChanged += TypeBox_CheckedChanged;

            Load += (s, e) =>
            {
                if (!DriveCleanupRunner.IsArchitectureSupported)
                {
                    AppendLog("[지원 안 함] 이 기능은 x86/x64에서만 사용할 수 있습니다 (ARM 미지원).");
                    btnScan.Enabled = false;
                    btnClean.Enabled = false;
                    grpTypes.Enabled = false;
                    // 확장 정리는 pnputil 등 시스템 도구를 쓰므로 ARM 에서도 대부분 동작.
                    // DriveCleanup 자체를 못 쓸 뿐. 확장 섹션은 활성 유지.
                    return;
                }

                if (!DriveCleanupRunner.IsAvailable)
                {
                    AppendLog("[경고] DriveCleanup 실행 파일을 찾을 수 없습니다:");
                    AppendLog("        " + DriveCleanupRunner.ExePath);
                    AppendLog("        drivecleanup 폴더에 실행 파일을 넣어주세요.");
                    btnScan.Enabled = false;
                    btnClean.Enabled = false;
                }
                else
                {
                    AppendLog("[준비] 사용 실행 파일: " + DriveCleanupRunner.ExePath);
                    AppendLog("먼저 [미리보기]로 무엇이 제거될지 확인한 뒤 [정리 실행]을 권장합니다.");
                }
                AppendLog("");
                AppendLog("[확장 정리] DriveCleanup 이 놓치는 다섯 가지 흔적을 추가로 처리합니다:");
                AppendLog("  · 이벤트 로그 (Partition/Diagnostic, Storsvc, Kernel-PnP, System WPD)");
                AppendLog("  · setupapi.dev.log 의 USBSTOR/WPDBUSENUM 섹션");
                AppendLog("  · 현재 연결된 USB 저장장치 (pnputil 로 강제 제거)");
                AppendLog("  · Windows Search VolumeInfoCache");
                AppendLog("  · MountPoints2 잔여 GUID");
            };
        }

        private CheckBox[] TypeBoxes() => new[]
        {
            chkUsbStor, chkDisks, chkVolumes, chkCdrom, chkFloppy, chkWpd
        };

        private bool _suppressCheckEvents;

        private void ChkAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressCheckEvents) return;
            if (chkAll.Checked)
            {
                _suppressCheckEvents = true;
                foreach (var box in TypeBoxes()) box.Checked = false;
                _suppressCheckEvents = false;
            }
            else if (NoTypeSelected())
            {
                _suppressCheckEvents = true;
                chkAll.Checked = true;
                _suppressCheckEvents = false;
            }
        }

        private void TypeBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressCheckEvents) return;
            _suppressCheckEvents = true;
            if (((CheckBox)sender).Checked)
            {
                chkAll.Checked = false;
            }
            else if (NoTypeSelected())
            {
                chkAll.Checked = true;
            }
            _suppressCheckEvents = false;
        }

        private bool NoTypeSelected()
        {
            foreach (var box in TypeBoxes())
                if (box.Checked) return false;
            return true;
        }

        private string BuildArgs(bool testMode)
        {
            if (chkAll.Checked)
                return DriveCleanupRunner.BuildArgs(testMode);

            return DriveCleanupRunner.BuildArgs(
                testMode,
                usbMassStorage: chkUsbStor.Checked,
                disks: chkDisks.Checked,
                volumes: chkVolumes.Checked,
                cdrom: chkCdrom.Checked,
                floppy: chkFloppy.Checked,
                wpd: chkWpd.Checked);
        }

        private async void BtnClean_Click(object sender, EventArgs e)
        {
            string scope = chkAll.Checked ? "모든 종류" : "선택한 종류";
            var confirm = MessageBox.Show(this,
                $"현재 연결되지 않은 {scope}의 드라이브 장치 기록을 제거합니다.\n" +
                "이 작업은 되돌릴 수 없습니다. 계속할까요?",
                "정리 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            await RunAsync(testMode: false);
        }

        private async System.Threading.Tasks.Task RunAsync(bool testMode)
        {
            if (!DriveCleanupRunner.IsAvailable)
            {
                MessageBox.Show(this,
                    "DriveCleanup 실행 파일을 찾을 수 없습니다.\n" + DriveCleanupRunner.ExePath,
                    "Ashes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string args = BuildArgs(testMode);

            SetRunning(true);
            AppendLog("");
            AppendLog((testMode ? "> [미리보기] DriveCleanup " : "> DriveCleanup ") + args);

            _cts = new CancellationTokenSource();
            try
            {
                await DriveCleanupRunner.RunAsync(args, OnOutput, _cts.Token);
                AppendLog(testMode ? "[미리보기 완료]" : "[완료]");
            }
            catch (OperationCanceledException)
            {
                AppendLog("[중지됨] 사용자가 작업을 취소했습니다.");
            }
            catch (Exception ex)
            {
                AppendLog("[오류] " + ex.Message);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                SetRunning(false);
                TaskbarFlasher.Flash(this);
            }
        }

        // ---------- 외장 드라이브 흔적 분석 → JSON export ----------

        private async void BtnAudit_Click(object sender, EventArgs e)
        {
            string outPath;
            using (var dlg = new SaveFileDialog
            {
                Title = "분석 결과를 저장할 위치",
                Filter = "JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"UsbAudit_{Environment.MachineName}_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                OverwritePrompt = true,
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                outPath = dlg.FileName;
            }

            SetRunning(true);
            AppendLog("");
            AppendLog("> 외장 드라이브 흔적 분석 (--all -v)");

            try
            {
                // 스캔은 레지스트리/이벤트로그/섀도 복사본까지 훑어 수 초~수십 초 걸릴 수 있어
                // UI 가 멈추지 않도록 백그라운드 스레드에서 실행하고, 로그는 OnOutput 으로 마샬링.
                int count = await System.Threading.Tasks.Task.Run(
                    () => Ashes.UsbAudit.UsbAuditor.RunAndExport(outPath, OnOutput));

                AppendLog($"[분석 완료] 총 {count}건, 저장: {outPath}");

                if (MessageBox.Show(this,
                        $"분석 완료 — {count}건 수집.\n저장 위치:\n{outPath}\n\n저장 폴더를 열까요?",
                        "분석 완료", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{outPath}\"",
                            UseShellExecute = true,
                        });
                    }
                    catch { /* 탐색기 열기 실패는 무시 */ }
                }
            }
            catch (Exception ex)
            {
                AppendLog("[분석 오류] " + ex.Message);
            }
            finally
            {
                SetRunning(false);
                TaskbarFlasher.Flash(this);
            }
        }



        private void BtnExtRun_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(this,
                "확장 정리를 실행합니다. 아래 항목을 처리합니다:\n" +
                "  · 이벤트 로그 축소 (Partition/Diagnostic 등)\n" +
                "  · setupapi.dev.log 편집\n" +
                "  · 연결된 USB 저장장치 강제 제거\n" +
                "  · VolumeInfoCache / MountPoints2 정리\n\n" +
                "이 작업은 되돌릴 수 없습니다. 계속할까요?",
                "확장 정리 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            _ = RunExtendedAsync(dryRun: false);
        }

        private async System.Threading.Tasks.Task RunExtendedAsync(bool dryRun)
        {
            SetRunning(true);
            AppendLog("");
            AppendLog(dryRun ? "> [미리보기] 확장 정리" : "> 확장 정리 실행");

            _cts = new CancellationTokenSource();
            try
            {
                var opt = new TraceCleaner.Options
                {
                    DryRun            = dryRun,
                    EventLogs         = chkExtEvents.Checked,
                    SetupApi          = chkExtSetupApi.Checked,
                    ConnectedDevices  = chkExtConnected.Checked,
                    VolumeInfoCache   = chkExtVolCache.Checked,
                    MountPoints2      = chkExtMountPts.Checked,
                    RegistryUsb       = chkExtRegUsb.Checked,
                    Amcache           = chkExtAmcache.Checked,
                };
                await TraceCleaner.RunAsync(opt, OnOutput, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                AppendLog("[중지됨] 사용자가 작업을 취소했습니다.");
            }
            catch (Exception ex)
            {
                AppendLog("[오류] " + ex.Message);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                SetRunning(false);
                TaskbarFlasher.Flash(this);
            }
        }

        private void SetRunning(bool running)
        {
            btnScan.Enabled = !running;
            btnClean.Enabled = !running;
            btnCancel.Enabled = running;
            grpTypes.Enabled = !running;
            btnExtScan.Enabled = !running;
            btnExtRun.Enabled = !running;
            grpExt.Enabled = !running;
            btnAudit.Enabled = !running;
        }

        // ---------- logging (same approach as WipeForm) ----------

        private int _lastLineStart = 0;

        private void OnOutput(string text)
        {
            if (txtLog.InvokeRequired)
                txtLog.BeginInvoke(new Action<string>(AppendLog), text);
            else
                AppendLog(text);
        }

        private void AppendLog(string line)
        {
            if (txtLog.TextLength > _lastLineStart)
            {
                txtLog.AppendText(Environment.NewLine);
            }
            txtLog.AppendText((line ?? string.Empty) + Environment.NewLine);
            _lastLineStart = txtLog.TextLength;
            ScrollLogToBottom();
        }

        private void ScrollLogToBottom()
        {
            txtLog.Select(txtLog.TextLength, 0);
            txtLog.ScrollToCaret();
            SendMessage(txtLog.Handle, WM_HSCROLL, (IntPtr)SB_LEFT, IntPtr.Zero);
        }

        private const int WM_HSCROLL = 0x0114;
        private const int SB_LEFT = 6;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
