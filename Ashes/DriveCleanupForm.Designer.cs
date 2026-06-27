namespace Ashes
{
    partial class DriveCleanupForm
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

        private System.Windows.Forms.Label lblIntro;
        private System.Windows.Forms.GroupBox grpTypes;
        private System.Windows.Forms.CheckBox chkAll;
        private System.Windows.Forms.CheckBox chkUsbStor;
        private System.Windows.Forms.CheckBox chkDisks;
        private System.Windows.Forms.CheckBox chkVolumes;
        private System.Windows.Forms.CheckBox chkCdrom;
        private System.Windows.Forms.CheckBox chkFloppy;
        private System.Windows.Forms.CheckBox chkWpd;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.Button btnClean;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblWarn;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DriveCleanupForm));
            lblIntro = new System.Windows.Forms.Label();
            grpTypes = new System.Windows.Forms.GroupBox();
            chkAll = new System.Windows.Forms.CheckBox();
            chkUsbStor = new System.Windows.Forms.CheckBox();
            chkDisks = new System.Windows.Forms.CheckBox();
            chkVolumes = new System.Windows.Forms.CheckBox();
            chkCdrom = new System.Windows.Forms.CheckBox();
            chkFloppy = new System.Windows.Forms.CheckBox();
            chkWpd = new System.Windows.Forms.CheckBox();
            btnScan = new System.Windows.Forms.Button();
            btnClean = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            txtLog = new System.Windows.Forms.TextBox();
            lblWarn = new System.Windows.Forms.Label();
            grpTypes.SuspendLayout();
            SuspendLayout();
            // 
            // lblIntro
            // 
            lblIntro.AutoSize = true;
            lblIntro.Location = new System.Drawing.Point(17, 20);
            lblIntro.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblIntro.Name = "lblIntro";
            lblIntro.Size = new System.Drawing.Size(598, 25);
            lblIntro.TabIndex = 0;
            lblIntro.Text = "현재 연결되지 않은 드라이브 장치의 흔적을 레지스트리에서 제거합니다.";
            // 
            // grpTypes
            // 
            grpTypes.Controls.Add(chkAll);
            grpTypes.Controls.Add(chkUsbStor);
            grpTypes.Controls.Add(chkDisks);
            grpTypes.Controls.Add(chkVolumes);
            grpTypes.Controls.Add(chkCdrom);
            grpTypes.Controls.Add(chkFloppy);
            grpTypes.Controls.Add(chkWpd);
            grpTypes.Location = new System.Drawing.Point(17, 63);
            grpTypes.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            grpTypes.Name = "grpTypes";
            grpTypes.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            grpTypes.Size = new System.Drawing.Size(629, 183);
            grpTypes.TabIndex = 1;
            grpTypes.TabStop = false;
            grpTypes.Text = "정리할 장치 종류";
            // 
            // chkAll
            // 
            chkAll.AutoSize = true;
            chkAll.Checked = true;
            chkAll.CheckState = System.Windows.Forms.CheckState.Checked;
            chkAll.Location = new System.Drawing.Point(21, 40);
            chkAll.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            chkAll.Name = "chkAll";
            chkAll.Size = new System.Drawing.Size(168, 29);
            chkAll.TabIndex = 0;
            chkAll.Text = "전체 (모든 종류)";
            chkAll.UseVisualStyleBackColor = true;
            // 
            // chkUsbStor
            // 
            chkUsbStor.AutoSize = true;
            chkUsbStor.Location = new System.Drawing.Point(229, 40);
            chkUsbStor.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            chkUsbStor.Name = "chkUsbStor";
            chkUsbStor.Size = new System.Drawing.Size(243, 29);
            chkUsbStor.TabIndex = 1;
            chkUsbStor.Text = "USB 대용량 저장장치 (-u)";
            chkUsbStor.UseVisualStyleBackColor = true;
            // 
            // chkDisks
            // 
            chkDisks.AutoSize = true;
            chkDisks.Location = new System.Drawing.Point(229, 82);
            chkDisks.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            chkDisks.Name = "chkDisks";
            chkDisks.Size = new System.Drawing.Size(168, 29);
            chkDisks.TabIndex = 2;
            chkDisks.Text = "디스크 장치 (-d)";
            chkDisks.UseVisualStyleBackColor = true;
            // 
            // chkVolumes
            // 
            chkVolumes.AutoSize = true;
            chkVolumes.Location = new System.Drawing.Point(229, 123);
            chkVolumes.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            chkVolumes.Name = "chkVolumes";
            chkVolumes.Size = new System.Drawing.Size(184, 29);
            chkVolumes.TabIndex = 3;
            chkVolumes.Text = "스토리지 볼륨 (-v)";
            chkVolumes.UseVisualStyleBackColor = true;
            // 
            // chkCdrom
            // 
            chkCdrom.AutoSize = true;
            chkCdrom.Location = new System.Drawing.Point(443, 40);
            chkCdrom.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            chkCdrom.Name = "chkCdrom";
            chkCdrom.Size = new System.Drawing.Size(143, 29);
            chkCdrom.TabIndex = 4;
            chkCdrom.Text = "CD-ROM (-c)";
            chkCdrom.UseVisualStyleBackColor = true;
            // 
            // chkFloppy
            // 
            chkFloppy.AutoSize = true;
            chkFloppy.Location = new System.Drawing.Point(443, 82);
            chkFloppy.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            chkFloppy.Name = "chkFloppy";
            chkFloppy.Size = new System.Drawing.Size(121, 29);
            chkFloppy.TabIndex = 5;
            chkFloppy.Text = "플로피 (-f)";
            chkFloppy.UseVisualStyleBackColor = true;
            // 
            // chkWpd
            // 
            chkWpd.AutoSize = true;
            chkWpd.Location = new System.Drawing.Point(443, 123);
            chkWpd.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            chkWpd.Name = "chkWpd";
            chkWpd.Size = new System.Drawing.Size(154, 29);
            chkWpd.TabIndex = 6;
            chkWpd.Text = "USB WPD (-w)";
            chkWpd.UseVisualStyleBackColor = true;
            // 
            // btnScan
            // 
            btnScan.Location = new System.Drawing.Point(17, 263);
            btnScan.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnScan.Name = "btnScan";
            btnScan.Size = new System.Drawing.Size(200, 50);
            btnScan.TabIndex = 2;
            btnScan.Text = "미리보기 (스캔)";
            btnScan.UseVisualStyleBackColor = true;
            // 
            // btnClean
            // 
            btnClean.Location = new System.Drawing.Point(326, 263);
            btnClean.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnClean.Name = "btnClean";
            btnClean.Size = new System.Drawing.Size(200, 50);
            btnClean.TabIndex = 3;
            btnClean.Text = "정리 실행";
            btnClean.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Enabled = false;
            btnCancel.Location = new System.Drawing.Point(534, 263);
            btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(111, 50);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "중지";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            txtLog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtLog.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            txtLog.ForeColor = System.Drawing.Color.Gainsboro;
            txtLog.Location = new System.Drawing.Point(17, 370);
            txtLog.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtLog.Size = new System.Drawing.Size(655, 297);
            txtLog.TabIndex = 6;
            txtLog.WordWrap = false;
            // 
            // lblWarn
            // 
            lblWarn.AutoSize = true;
            lblWarn.ForeColor = System.Drawing.Color.Firebrick;
            lblWarn.Location = new System.Drawing.Point(17, 330);
            lblWarn.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblWarn.Name = "lblWarn";
            lblWarn.Size = new System.Drawing.Size(662, 25);
            lblWarn.TabIndex = 5;
            lblWarn.Text = "주의: 정리 실행은 현재 연결되지 않은 해당 종류의 장치 기록을 모두 제거합니다.";
            // 
            // DriveCleanupForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(691, 690);
            Controls.Add(txtLog);
            Controls.Add(lblWarn);
            Controls.Add(btnCancel);
            Controls.Add(btnClean);
            Controls.Add(btnScan);
            Controls.Add(grpTypes);
            Controls.Add(lblIntro);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            MinimumSize = new System.Drawing.Size(676, 718);
            Name = "DriveCleanupForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "외장 드라이브 흔적 정리";
            grpTypes.ResumeLayout(false);
            grpTypes.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
