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
                // Don't allow a state where nothing is selected; re-check 전체.
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
            // 전체 selected → no type flags → DriveCleanup cleans everything.
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
                // DriveCleanup prints its own per-type "N ... to remove" summary,
                // so we just mark completion. (Its exit code isn't a reliable
                // device count — in test mode it's 0 even when items are listed.)
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

        private void SetRunning(bool running)
        {
            btnScan.Enabled = !running;
            btnClean.Enabled = !running;
            btnCancel.Enabled = running;
            grpTypes.Enabled = !running;
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