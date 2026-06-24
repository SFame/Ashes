namespace Ashes
{
    partial class WipeForm
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

        private System.Windows.Forms.Label lblDrive;
        private System.Windows.Forms.ComboBox cmbDrive;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.GroupBox grpMode;
        private System.Windows.Forms.RadioButton rdoClean;
        private System.Windows.Forms.RadioButton rdoZero;
        private System.Windows.Forms.Label lblPasses;
        private System.Windows.Forms.NumericUpDown numPasses;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblWarn;

        private void InitializeComponent()
        {
            this.lblDrive = new System.Windows.Forms.Label();
            this.cmbDrive = new System.Windows.Forms.ComboBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.grpMode = new System.Windows.Forms.GroupBox();
            this.rdoClean = new System.Windows.Forms.RadioButton();
            this.rdoZero = new System.Windows.Forms.RadioButton();
            this.lblPasses = new System.Windows.Forms.Label();
            this.numPasses = new System.Windows.Forms.NumericUpDown();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.lblWarn = new System.Windows.Forms.Label();
            this.grpMode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPasses)).BeginInit();
            this.SuspendLayout();
            // 
            // lblDrive
            // 
            this.lblDrive.AutoSize = true;
            this.lblDrive.Location = new System.Drawing.Point(12, 15);
            this.lblDrive.Name = "lblDrive";
            this.lblDrive.Size = new System.Drawing.Size(54, 15);
            this.lblDrive.TabIndex = 0;
            this.lblDrive.Text = "드라이브";
            // 
            // cmbDrive
            // 
            this.cmbDrive.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDrive.FormattingEnabled = true;
            this.cmbDrive.Location = new System.Drawing.Point(80, 12);
            this.cmbDrive.Name = "cmbDrive";
            this.cmbDrive.Size = new System.Drawing.Size(220, 23);
            this.cmbDrive.TabIndex = 1;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(308, 11);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(80, 25);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "새로고침";
            this.btnRefresh.UseVisualStyleBackColor = true;
            // 
            // grpMode
            // 
            this.grpMode.Controls.Add(this.rdoClean);
            this.grpMode.Controls.Add(this.rdoZero);
            this.grpMode.Location = new System.Drawing.Point(12, 47);
            this.grpMode.Name = "grpMode";
            this.grpMode.Size = new System.Drawing.Size(376, 60);
            this.grpMode.TabIndex = 3;
            this.grpMode.TabStop = false;
            this.grpMode.Text = "정리 방식";
            // 
            // rdoClean
            // 
            this.rdoClean.AutoSize = true;
            this.rdoClean.Checked = true;
            this.rdoClean.Location = new System.Drawing.Point(15, 25);
            this.rdoClean.Name = "rdoClean";
            this.rdoClean.Size = new System.Drawing.Size(160, 19);
            this.rdoClean.TabIndex = 0;
            this.rdoClean.TabStop = true;
            this.rdoClean.Text = "빈 공간 안전 정리 (-c)";
            this.rdoClean.UseVisualStyleBackColor = true;
            // 
            // rdoZero
            // 
            this.rdoZero.AutoSize = true;
            this.rdoZero.Location = new System.Drawing.Point(195, 25);
            this.rdoZero.Name = "rdoZero";
            this.rdoZero.Size = new System.Drawing.Size(165, 19);
            this.rdoZero.TabIndex = 1;
            this.rdoZero.Text = "0으로 채우기 (-z)";
            this.rdoZero.UseVisualStyleBackColor = true;
            // 
            // lblPasses
            // 
            this.lblPasses.AutoSize = true;
            this.lblPasses.Location = new System.Drawing.Point(12, 122);
            this.lblPasses.Name = "lblPasses";
            this.lblPasses.Size = new System.Drawing.Size(101, 15);
            this.lblPasses.TabIndex = 4;
            this.lblPasses.Text = "덮어쓰기 횟수 (-p)";
            // 
            // numPasses
            // 
            this.numPasses.Location = new System.Drawing.Point(125, 120);
            this.numPasses.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numPasses.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numPasses.Name = "numPasses";
            this.numPasses.Size = new System.Drawing.Size(64, 23);
            this.numPasses.TabIndex = 5;
            this.numPasses.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnRun
            // 
            this.btnRun.Location = new System.Drawing.Point(228, 116);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(90, 30);
            this.btnRun.TabIndex = 6;
            this.btnRun.Text = "정리 실행";
            this.btnRun.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Enabled = false;
            this.btnCancel.Location = new System.Drawing.Point(324, 116);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(64, 30);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "중지";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right));
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.txtLog.ForeColor = System.Drawing.Color.Gainsboro;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.Location = new System.Drawing.Point(12, 184);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(376, 165);
            this.txtLog.TabIndex = 9;
            this.txtLog.WordWrap = false;
            // 
            // lblWarn
            // 
            this.lblWarn.AutoSize = true;
            this.lblWarn.ForeColor = System.Drawing.Color.Firebrick;
            this.lblWarn.Location = new System.Drawing.Point(12, 160);
            this.lblWarn.Name = "lblWarn";
            this.lblWarn.Size = new System.Drawing.Size(360, 15);
            this.lblWarn.TabIndex = 8;
            this.lblWarn.Text = "주의: 시간이 오래 걸릴 수 있습니다.";
            // 
            // WipeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 361);
            this.Controls.Add(this.lblWarn);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.numPasses);
            this.Controls.Add(this.lblPasses);
            this.Controls.Add(this.grpMode);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.cmbDrive);
            this.Controls.Add(this.lblDrive);
            this.MinimumSize = new System.Drawing.Size(416, 400);
            this.Name = "WipeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "드라이브 빈 공간 정리";
            this.grpMode.ResumeLayout(false);
            this.grpMode.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPasses)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
