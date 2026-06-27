namespace Ashes
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private System.Windows.Forms.ListBox lstTargets;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.CheckBox chkReadOnly;
        private System.Windows.Forms.CheckBox chkRecurse;
        private System.Windows.Forms.Label lblPasses;
        private System.Windows.Forms.NumericUpDown numPasses;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnWipe;
        private System.Windows.Forms.Button btnCleanup;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblHint;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            lstTargets = new System.Windows.Forms.ListBox();
            btnAdd = new System.Windows.Forms.Button();
            btnRemove = new System.Windows.Forms.Button();
            btnClear = new System.Windows.Forms.Button();
            chkReadOnly = new System.Windows.Forms.CheckBox();
            chkRecurse = new System.Windows.Forms.CheckBox();
            lblPasses = new System.Windows.Forms.Label();
            numPasses = new System.Windows.Forms.NumericUpDown();
            btnRun = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            btnWipe = new System.Windows.Forms.Button();
            btnCleanup = new System.Windows.Forms.Button();
            txtLog = new System.Windows.Forms.TextBox();
            lblHint = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)numPasses).BeginInit();
            SuspendLayout();
            // 
            // lstTargets
            // 
            lstTargets.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lstTargets.FormattingEnabled = true;
            lstTargets.HorizontalScrollbar = true;
            lstTargets.IntegralHeight = false;
            lstTargets.Location = new System.Drawing.Point(17, 52);
            lstTargets.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            lstTargets.Name = "lstTargets";
            lstTargets.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            lstTargets.Size = new System.Drawing.Size(798, 281);
            lstTargets.TabIndex = 1;
            // 
            // btnAdd
            // 
            btnAdd.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnAdd.Location = new System.Drawing.Point(826, 52);
            btnAdd.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new System.Drawing.Size(157, 47);
            btnAdd.TabIndex = 2;
            btnAdd.Text = "추가...";
            btnAdd.UseVisualStyleBackColor = true;
            // 
            // btnRemove
            // 
            btnRemove.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnRemove.Location = new System.Drawing.Point(826, 108);
            btnRemove.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnRemove.Name = "btnRemove";
            btnRemove.Size = new System.Drawing.Size(157, 47);
            btnRemove.TabIndex = 3;
            btnRemove.Text = "선택 제거";
            btnRemove.UseVisualStyleBackColor = true;
            // 
            // btnClear
            // 
            btnClear.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnClear.Location = new System.Drawing.Point(826, 165);
            btnClear.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnClear.Name = "btnClear";
            btnClear.Size = new System.Drawing.Size(157, 47);
            btnClear.TabIndex = 4;
            btnClear.Text = "전체 비우기";
            btnClear.UseVisualStyleBackColor = true;
            // 
            // chkReadOnly
            // 
            chkReadOnly.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            chkReadOnly.AutoSize = true;
            chkReadOnly.Location = new System.Drawing.Point(17, 361);
            chkReadOnly.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            chkReadOnly.Name = "chkReadOnly";
            chkReadOnly.Size = new System.Drawing.Size(229, 29);
            chkReadOnly.TabIndex = 6;
            chkReadOnly.Text = "읽기 전용 속성 제거 (-r)";
            chkReadOnly.UseVisualStyleBackColor = true;
            // 
            // chkRecurse
            // 
            chkRecurse.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            chkRecurse.AutoSize = true;
            chkRecurse.Checked = true;
            chkRecurse.CheckState = System.Windows.Forms.CheckState.Checked;
            chkRecurse.Location = new System.Drawing.Point(286, 361);
            chkRecurse.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            chkRecurse.Name = "chkRecurse";
            chkRecurse.Size = new System.Drawing.Size(189, 29);
            chkRecurse.TabIndex = 7;
            chkRecurse.Text = "하위 폴더 재귀 (-s)";
            chkRecurse.UseVisualStyleBackColor = true;
            // 
            // lblPasses
            // 
            lblPasses.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            lblPasses.AutoSize = true;
            lblPasses.Location = new System.Drawing.Point(557, 360);
            lblPasses.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblPasses.Name = "lblPasses";
            lblPasses.Size = new System.Drawing.Size(160, 25);
            lblPasses.TabIndex = 8;
            lblPasses.Text = "덮어쓰기 횟수 (-p)";
            // 
            // numPasses
            // 
            numPasses.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            numPasses.Location = new System.Drawing.Point(726, 355);
            numPasses.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numPasses.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numPasses.Name = "numPasses";
            numPasses.Size = new System.Drawing.Size(91, 31);
            numPasses.TabIndex = 9;
            numPasses.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnRun
            // 
            btnRun.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnRun.Location = new System.Drawing.Point(826, 348);
            btnRun.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnRun.Name = "btnRun";
            btnRun.Size = new System.Drawing.Size(157, 50);
            btnRun.TabIndex = 10;
            btnRun.Text = "안전 삭제 실행";
            btnRun.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnCancel.Enabled = false;
            btnCancel.Location = new System.Drawing.Point(826, 408);
            btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(157, 43);
            btnCancel.TabIndex = 11;
            btnCancel.Text = "중지";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnWipe
            // 
            btnWipe.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            btnWipe.Location = new System.Drawing.Point(17, 408);
            btnWipe.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnWipe.Name = "btnWipe";
            btnWipe.Size = new System.Drawing.Size(229, 43);
            btnWipe.TabIndex = 12;
            btnWipe.Text = "드라이브 빈 공간 정리...";
            btnWipe.UseVisualStyleBackColor = true;
            // 
            // btnCleanup
            // 
            btnCleanup.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            btnCleanup.Location = new System.Drawing.Point(254, 408);
            btnCleanup.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnCleanup.Name = "btnCleanup";
            btnCleanup.Size = new System.Drawing.Size(257, 43);
            btnCleanup.TabIndex = 13;
            btnCleanup.Text = "외장 드라이브 흔적 정리...";
            btnCleanup.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            txtLog.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtLog.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            txtLog.ForeColor = System.Drawing.Color.Gainsboro;
            txtLog.Location = new System.Drawing.Point(17, 475);
            txtLog.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtLog.Size = new System.Drawing.Size(964, 272);
            txtLog.TabIndex = 13;
            txtLog.WordWrap = false;
            // 
            // lblHint
            // 
            lblHint.AutoSize = true;
            lblHint.ForeColor = System.Drawing.SystemColors.GrayText;
            lblHint.Location = new System.Drawing.Point(17, 15);
            lblHint.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblHint.Name = "lblHint";
            lblHint.Size = new System.Drawing.Size(513, 25);
            lblHint.TabIndex = 0;
            lblHint.Text = "삭제할 파일/폴더를 추가하세요. (복구 불가능하게 삭제됩니다)";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1000, 770);
            Controls.Add(lblHint);
            Controls.Add(txtLog);
            Controls.Add(btnWipe);
            Controls.Add(btnCleanup);
            Controls.Add(btnCancel);
            Controls.Add(btnRun);
            Controls.Add(numPasses);
            Controls.Add(lblPasses);
            Controls.Add(chkRecurse);
            Controls.Add(chkReadOnly);
            Controls.Add(btnClear);
            Controls.Add(btnRemove);
            Controls.Add(btnAdd);
            Controls.Add(lstTargets);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            MinimumSize = new System.Drawing.Size(876, 696);
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Ashes";
            ((System.ComponentModel.ISupportInitialize)numPasses).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}