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
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblHint;

        private void InitializeComponent()
        {
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
            lstTargets.Location = new System.Drawing.Point(12, 31);
            lstTargets.Name = "lstTargets";
            lstTargets.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            lstTargets.Size = new System.Drawing.Size(560, 170);
            lstTargets.TabIndex = 1;
            // 
            // btnAdd
            // 
            btnAdd.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnAdd.Location = new System.Drawing.Point(578, 31);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new System.Drawing.Size(110, 28);
            btnAdd.TabIndex = 2;
            btnAdd.Text = "추가...";
            btnAdd.UseVisualStyleBackColor = true;
            // 
            // btnRemove
            // 
            btnRemove.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnRemove.Location = new System.Drawing.Point(578, 65);
            btnRemove.Name = "btnRemove";
            btnRemove.Size = new System.Drawing.Size(110, 28);
            btnRemove.TabIndex = 3;
            btnRemove.Text = "선택 제거";
            btnRemove.UseVisualStyleBackColor = true;
            // 
            // btnClear
            // 
            btnClear.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnClear.Location = new System.Drawing.Point(578, 99);
            btnClear.Name = "btnClear";
            btnClear.Size = new System.Drawing.Size(110, 28);
            btnClear.TabIndex = 4;
            btnClear.Text = "전체 비우기";
            btnClear.UseVisualStyleBackColor = true;
            // 
            // chkReadOnly
            // 
            chkReadOnly.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            chkReadOnly.AutoSize = true;
            chkReadOnly.Location = new System.Drawing.Point(12, 215);
            chkReadOnly.Name = "chkReadOnly";
            chkReadOnly.Size = new System.Drawing.Size(155, 19);
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
            chkRecurse.Location = new System.Drawing.Point(200, 215);
            chkRecurse.Name = "chkRecurse";
            chkRecurse.Size = new System.Drawing.Size(128, 19);
            chkRecurse.TabIndex = 7;
            chkRecurse.Text = "하위 폴더 재귀 (-s)";
            chkRecurse.UseVisualStyleBackColor = true;
            // 
            // lblPasses
            // 
            lblPasses.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            lblPasses.AutoSize = true;
            lblPasses.Location = new System.Drawing.Point(390, 216);
            lblPasses.Name = "lblPasses";
            lblPasses.Size = new System.Drawing.Size(107, 15);
            lblPasses.TabIndex = 8;
            lblPasses.Text = "덮어쓰기 횟수 (-p)";
            // 
            // numPasses
            // 
            numPasses.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            numPasses.Location = new System.Drawing.Point(508, 213);
            numPasses.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numPasses.Name = "numPasses";
            numPasses.Size = new System.Drawing.Size(64, 23);
            numPasses.TabIndex = 9;
            numPasses.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnRun
            // 
            btnRun.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnRun.Location = new System.Drawing.Point(578, 209);
            btnRun.Name = "btnRun";
            btnRun.Size = new System.Drawing.Size(110, 30);
            btnRun.TabIndex = 10;
            btnRun.Text = "안전 삭제 실행";
            btnRun.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnCancel.Enabled = false;
            btnCancel.Location = new System.Drawing.Point(578, 245);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(110, 26);
            btnCancel.TabIndex = 11;
            btnCancel.Text = "중지";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnWipe
            // 
            btnWipe.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            btnWipe.Location = new System.Drawing.Point(12, 245);
            btnWipe.Name = "btnWipe";
            btnWipe.Size = new System.Drawing.Size(170, 26);
            btnWipe.TabIndex = 12;
            btnWipe.Text = "드라이브 빈 공간 정리...";
            btnWipe.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            txtLog.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtLog.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            txtLog.ForeColor = System.Drawing.Color.Gainsboro;
            txtLog.Location = new System.Drawing.Point(12, 285);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtLog.Size = new System.Drawing.Size(676, 165);
            txtLog.TabIndex = 13;
            txtLog.WordWrap = false;
            // 
            // lblHint
            // 
            lblHint.AutoSize = true;
            lblHint.ForeColor = System.Drawing.SystemColors.GrayText;
            lblHint.Location = new System.Drawing.Point(12, 9);
            lblHint.Name = "lblHint";
            lblHint.Size = new System.Drawing.Size(343, 15);
            lblHint.TabIndex = 0;
            lblHint.Text = "삭제할 파일/폴더를 추가하세요. (복구 불가능하게 삭제됩니다)";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(700, 462);
            Controls.Add(lblHint);
            Controls.Add(txtLog);
            Controls.Add(btnWipe);
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
            MinimumSize = new System.Drawing.Size(620, 440);
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