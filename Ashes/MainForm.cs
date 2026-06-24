using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Ashes
{
    public partial class MainForm : Form
    {
        private CancellationTokenSource _cts;

        public MainForm()
        {
            InitializeComponent();

            // Wire events here so the designer file stays purely layout.
            this.btnAdd.Click += BtnAdd_Click;
            this.btnRemove.Click += BtnRemove_Click;
            this.btnClear.Click += BtnClear_Click;
            this.btnRun.Click += BtnRun_Click;
            this.btnCancel.Click += BtnCancel_Click;
            this.btnWipe.Click += BtnWipe_Click;

            this.lstTargets.KeyDown += LstTargets_KeyDown;

            this.Load += MainForm_Load;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!SDeleteRunner.IsAvailable)
            {
                AppendLog("[경고] SDelete 실행 파일을 찾을 수 없습니다:");
                AppendLog("        " + SDeleteRunner.ExePath);
                AppendLog("        sdelete 폴더에 실행 파일을 넣어주세요.");
            }
            else
            {
                AppendLog("[준비] 사용 실행 파일: " + SDeleteRunner.ExePath);
            }
        }

        // ---------- target list management ----------

        private void AddPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            path = path.Trim();

            // Avoid duplicates (case-insensitive on Windows).
            foreach (var item in lstTargets.Items)
            {
                if (string.Equals(item?.ToString(), path, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            if (File.Exists(path) || Directory.Exists(path))
                lstTargets.Items.Add(path);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            // One button, small popup menu to choose files or a folder.
            var menu = new ContextMenuStrip();
            menu.Items.Add("파일 선택...", null, (s, ev) => AddFilesDialog());
            menu.Items.Add("폴더 선택...", null, (s, ev) => AddFolderDialog());
            menu.Show(btnAdd, new System.Drawing.Point(0, btnAdd.Height));
        }

        private void AddFilesDialog()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "삭제할 파일 선택",
                Multiselect = true,
                CheckFileExists = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                foreach (var f in dlg.FileNames) AddPath(f);
            }
        }

        private void AddFolderDialog()
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "삭제할 폴더 선택",
                ShowNewFolderButton = false
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                AddPath(dlg.SelectedPath);
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            for (int i = lstTargets.SelectedIndices.Count - 1; i >= 0; i--)
            {
                lstTargets.Items.RemoveAt(lstTargets.SelectedIndices[i]);
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            lstTargets.Items.Clear();
        }

        private void LstTargets_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                BtnRemove_Click(sender, e);
                e.Handled = true;
            }
        }

        // ---------- run ----------

        private async void BtnRun_Click(object sender, EventArgs e)
        {
            if (lstTargets.Items.Count == 0)
            {
                MessageBox.Show(this, "삭제할 항목을 먼저 추가하세요.", "Ashes",
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

            var targets = lstTargets.Items.Cast<object>()
                .Select(o => o.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var confirm = MessageBox.Show(this,
                $"{targets.Count}개 항목을 복구 불가능하게 삭제합니다.\n계속할까요?",
                "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes) return;

            string args = BuildArgs(targets);

            SetRunning(true);
            AppendLog("");
            AppendLog("> sdelete " + args);

            _cts = new CancellationTokenSource();
            try
            {
                int code = await SDeleteRunner.RunAsync(args, OnSDeleteOutput, _cts.Token);
                AppendLog($"[완료] 종료 코드 {code}");
                if (code == 0)
                {
                    // SDelete removed the files; clear the list.
                    lstTargets.Items.Clear();
                }
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

        private string BuildArgs(List<string> targets)
        {
            var parts = new List<string> { "-accepteula", "-nobanner" };

            parts.Add("-p");
            parts.Add(((int)numPasses.Value).ToString());

            if (chkReadOnly.Checked) parts.Add("-r");
            if (chkRecurse.Checked) parts.Add("-s");

            foreach (var t in targets)
                parts.Add(SDeleteRunner.Quote(t));

            return string.Join(" ", parts);
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
        }

        private void SetRunning(bool running)
        {
            btnRun.Enabled = !running;
            btnCancel.Enabled = running;
            btnAdd.Enabled = !running;
            btnRemove.Enabled = !running;
            btnClear.Enabled = !running;
            btnWipe.Enabled = !running;
            numPasses.Enabled = !running;
            chkReadOnly.Enabled = !running;
            chkRecurse.Enabled = !running;
            lstTargets.Enabled = !running;
        }

        // ---------- drive wipe window ----------

        private void BtnWipe_Click(object sender, EventArgs e)
        {
            using var wipe = new WipeForm();
            wipe.ShowDialog(this);
        }

        // ---------- logging ----------

        // Index in txtLog where the current (possibly in-progress) line starts.
        // Used so \r progress updates can overwrite the last line in place.
        private int _lastLineStart = 0;

        // Called by SDeleteRunner. isProgress = true means "overwrite last line".
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
                // In-place progress update: replace the current line.
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
                // Completed line: replace current line content, then break.
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

        // Plain helper for our own status messages (always a new line).
        private void AppendLog(string line)
        {
            // Make sure we start on a fresh line if a progress line was pending.
            if (txtLog.TextLength > _lastLineStart)
            {
                txtLog.AppendText(Environment.NewLine);
            }
            txtLog.AppendText(line + Environment.NewLine);
            _lastLineStart = txtLog.TextLength;
            ScrollLogToBottom();
        }

        // Scrolls the log to the newest line WITHOUT scrolling horizontally.
        // ScrollToCaret() yanks the view right when a long path appears and back
        // left on a short one, which makes the box jitter side to side. We keep
        // the caret at the start of the last line and force the horizontal
        // scrollbar home so only the vertical position follows the output.
        private void ScrollLogToBottom()
        {
            txtLog.Select(txtLog.TextLength, 0);
            txtLog.ScrollToCaret();
            // Pin horizontal scroll to the far left.
            SendMessage(txtLog.Handle, WM_HSCROLL, (IntPtr)SB_LEFT, IntPtr.Zero);
        }

        private const int WM_HSCROLL = 0x0114;
        private const int SB_LEFT = 6;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}