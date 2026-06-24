using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Ashes
{
    public partial class WipeForm : Form
    {
        private CancellationTokenSource _cts;

        public WipeForm()
        {
            InitializeComponent();

            btnRefresh.Click += (s, e) => LoadDrives();
            btnRun.Click += BtnRun_Click;
            btnCancel.Click += (s, e) => _cts?.Cancel();
            Load += (s, e) => LoadDrives();
        }

        private void LoadDrives()
        {
            cmbDrive.Items.Clear();
            foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                // Display "C:\  (label, 120 GB free)" but keep the root letter for the arg.
                string label = string.IsNullOrWhiteSpace(d.VolumeLabel) ? "" : d.VolumeLabel;
                string free = FormatBytes(d.AvailableFreeSpace);
                cmbDrive.Items.Add(new DriveItem(d.Name,
                    $"{d.Name}  ({(label.Length > 0 ? label + ", " : "")}{free} 여유)"));
            }
            if (cmbDrive.Items.Count > 0) cmbDrive.SelectedIndex = 0;
        }

        private sealed class DriveItem
        {
            public string Root { get; }
            private readonly string _display;
            public DriveItem(string root, string display) { Root = root; _display = display; }
            public override string ToString() => _display;
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int i = 0;
            while (size >= 1024 && i < units.Length - 1) { size /= 1024; i++; }
            return $"{size:0.#} {units[i]}";
        }

        private async void BtnRun_Click(object sender, EventArgs e)
        {
            if (cmbDrive.SelectedItem is not DriveItem drive)
            {
                MessageBox.Show(this, "드라이브를 선택하세요.", "Ashes",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!SDeleteRunner.IsAvailable)
            {
                MessageBox.Show(this,
                    "SDelete 실행 파일을 찾을 수 없습니다.\n" + SDeleteRunner.ExePath,
                    "Ashes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // SDelete wants the drive letter with colon, e.g. "C:".
            string driveArg = drive.Root.TrimEnd('\\');

            var confirm = MessageBox.Show(this,
                $"{driveArg} 드라이브의 빈 공간을 정리합니다.\n" +
                "기존 파일은 영향받지 않지만 시간이 오래 걸릴 수 있습니다.\n계속할까요?",
                "정리 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            string modeFlag = rdoZero.Checked ? "-z" : "-c";
            string args = string.Join(" ",
                "-accepteula", "-nobanner",
                "-p", ((int)numPasses.Value).ToString(),
                modeFlag,
                SDeleteRunner.Quote(driveArg));

            SetRunning(true);
            AppendLog("");
            AppendLog("> sdelete " + args);

            _cts = new CancellationTokenSource();
            try
            {
                int code = await SDeleteRunner.RunAsync(args, OnSDeleteOutput, _cts.Token);
                AppendLog($"[완료] 종료 코드 {code}");
                LoadDrives();
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
                TaskbarFlasher.Flash(this); // highlight taskbar when done
            }
        }

        private void SetRunning(bool running)
        {
            btnRun.Enabled = !running;
            btnCancel.Enabled = running;
            btnRefresh.Enabled = !running;
            cmbDrive.Enabled = !running;
            numPasses.Enabled = !running;
            rdoClean.Enabled = !running;
            rdoZero.Enabled = !running;
        }

        private int _lastLineStart = 0;

        private void OnSDeleteOutput(string text, bool isProgress)
        {
            if (txtLog.InvokeRequired)
                txtLog.BeginInvoke(new Action<string, bool>(WriteSegment), text, isProgress);
            else
                WriteSegment(text, isProgress);
        }

        private void WriteSegment(string text, bool isProgress)
        {
            if (string.IsNullOrEmpty(text)) return;

            bool overwrite = isProgress || text.Contains('%');

            if (overwrite)
            {
                if (_lastLineStart < txtLog.TextLength)
                {
                    txtLog.Select(_lastLineStart, txtLog.TextLength - _lastLineStart);
                    txtLog.SelectedText = text;
                }
                else
                {
                    txtLog.AppendText(text);
                }
            }
            else
            {
                if (_lastLineStart < txtLog.TextLength)
                {
                    txtLog.Select(_lastLineStart, txtLog.TextLength - _lastLineStart);
                    txtLog.SelectedText = text;
                }
                else
                {
                    txtLog.AppendText(text);
                }
                txtLog.AppendText(Environment.NewLine);
                _lastLineStart = txtLog.TextLength;
            }
            ScrollLogToBottom();
        }

        private void AppendLog(string line)
        {
            if (txtLog.TextLength > _lastLineStart)
            {
                txtLog.AppendText(Environment.NewLine);
            }
            txtLog.AppendText(line + Environment.NewLine);
            _lastLineStart = txtLog.TextLength;
            ScrollLogToBottom();
        }

        // Scrolls to newest line without horizontal jitter (see MainForm).
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