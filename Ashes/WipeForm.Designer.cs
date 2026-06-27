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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WipeForm));
            lblDrive = new System.Windows.Forms.Label();
            cmbDrive = new System.Windows.Forms.ComboBox();
            btnRefresh = new System.Windows.Forms.Button();
            grpMode = new System.Windows.Forms.GroupBox();
            rdoClean = new System.Windows.Forms.RadioButton();
            rdoZero = new System.Windows.Forms.RadioButton();
            lblPasses = new System.Windows.Forms.Label();
            numPasses = new System.Windows.Forms.NumericUpDown();
            btnRun = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            txtLog = new System.Windows.Forms.TextBox();
            lblWarn = new System.Windows.Forms.Label();
            grpMode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numPasses).BeginInit();
            SuspendLayout();
            // 
            // lblDrive
            // 
            lblDrive.AutoSize = true;
            lblDrive.Location = new System.Drawing.Point(17, 25);
            lblDrive.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblDrive.Name = "lblDrive";
            lblDrive.Size = new System.Drawing.Size(84, 25);
            lblDrive.TabIndex = 0;
            lblDrive.Text = "드라이브";
            // 
            // cmbDrive
            // 
            cmbDrive.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbDrive.FormattingEnabled = true;
            cmbDrive.Location = new System.Drawing.Point(114, 20);
            cmbDrive.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            cmbDrive.Name = "cmbDrive";
            cmbDrive.Size = new System.Drawing.Size(313, 33);
            cmbDrive.TabIndex = 1;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new System.Drawing.Point(440, 18);
            btnRefresh.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new System.Drawing.Size(114, 42);
            btnRefresh.TabIndex = 2;
            btnRefresh.Text = "새로고침";
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // grpMode
            // 
            grpMode.Controls.Add(rdoClean);
            grpMode.Controls.Add(rdoZero);
            grpMode.Location = new System.Drawing.Point(17, 78);
            grpMode.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            grpMode.Name = "grpMode";
            grpMode.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            grpMode.Size = new System.Drawing.Size(537, 100);
            grpMode.TabIndex = 3;
            grpMode.TabStop = false;
            grpMode.Text = "정리 방식";
            // 
            // rdoClean
            // 
            rdoClean.AutoSize = true;
            rdoClean.Checked = true;
            rdoClean.Location = new System.Drawing.Point(21, 42);
            rdoClean.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            rdoClean.Name = "rdoClean";
            rdoClean.Size = new System.Drawing.Size(213, 29);
            rdoClean.TabIndex = 0;
            rdoClean.TabStop = true;
            rdoClean.Text = "빈 공간 안전 정리 (-c)";
            rdoClean.UseVisualStyleBackColor = true;
            // 
            // rdoZero
            // 
            rdoZero.AutoSize = true;
            rdoZero.Location = new System.Drawing.Point(279, 42);
            rdoZero.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            rdoZero.Name = "rdoZero";
            rdoZero.Size = new System.Drawing.Size(174, 29);
            rdoZero.TabIndex = 1;
            rdoZero.Text = "0으로 채우기 (-z)";
            rdoZero.UseVisualStyleBackColor = true;
            // 
            // lblPasses
            // 
            lblPasses.AutoSize = true;
            lblPasses.Location = new System.Drawing.Point(17, 203);
            lblPasses.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblPasses.Name = "lblPasses";
            lblPasses.Size = new System.Drawing.Size(160, 25);
            lblPasses.TabIndex = 4;
            lblPasses.Text = "덮어쓰기 횟수 (-p)";
            // 
            // numPasses
            // 
            numPasses.Location = new System.Drawing.Point(179, 200);
            numPasses.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            numPasses.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numPasses.Name = "numPasses";
            numPasses.Size = new System.Drawing.Size(91, 31);
            numPasses.TabIndex = 5;
            numPasses.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnRun
            // 
            btnRun.Location = new System.Drawing.Point(326, 193);
            btnRun.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnRun.Name = "btnRun";
            btnRun.Size = new System.Drawing.Size(129, 50);
            btnRun.TabIndex = 6;
            btnRun.Text = "정리 실행";
            btnRun.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Enabled = false;
            btnCancel.Location = new System.Drawing.Point(463, 193);
            btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(91, 50);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "중지";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            txtLog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtLog.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            txtLog.ForeColor = System.Drawing.Color.Gainsboro;
            txtLog.Location = new System.Drawing.Point(17, 307);
            txtLog.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtLog.Size = new System.Drawing.Size(535, 272);
            txtLog.TabIndex = 9;
            txtLog.WordWrap = false;
            // 
            // lblWarn
            // 
            lblWarn.AutoSize = true;
            lblWarn.ForeColor = System.Drawing.Color.Firebrick;
            lblWarn.Location = new System.Drawing.Point(17, 267);
            lblWarn.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblWarn.Name = "lblWarn";
            lblWarn.Size = new System.Drawing.Size(302, 25);
            lblWarn.TabIndex = 8;
            lblWarn.Text = "주의: 시간이 오래 걸릴 수 있습니다.";
            // 
            // WipeForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(571, 602);
            Controls.Add(lblWarn);
            Controls.Add(txtLog);
            Controls.Add(btnCancel);
            Controls.Add(btnRun);
            Controls.Add(numPasses);
            Controls.Add(lblPasses);
            Controls.Add(grpMode);
            Controls.Add(btnRefresh);
            Controls.Add(cmbDrive);
            Controls.Add(lblDrive);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            MinimumSize = new System.Drawing.Size(585, 629);
            Name = "WipeForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "드라이브 빈 공간 정리";
            grpMode.ResumeLayout(false);
            grpMode.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numPasses).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
