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
        // 확장 정리
        private System.Windows.Forms.GroupBox grpExt;
        private System.Windows.Forms.CheckBox chkExtEvents;
        private System.Windows.Forms.CheckBox chkExtSetupApi;
        private System.Windows.Forms.CheckBox chkExtConnected;
        private System.Windows.Forms.CheckBox chkExtVolCache;
        private System.Windows.Forms.CheckBox chkExtMountPts;
        private System.Windows.Forms.CheckBox chkExtRegUsb;
        private System.Windows.Forms.CheckBox chkExtAmcache;
        private System.Windows.Forms.CheckBox chkExtJumpList;
        private System.Windows.Forms.CheckBox chkExtPrefetch;
        private System.Windows.Forms.CheckBox chkExtShellbags;
        private System.Windows.Forms.Button btnExtScan;
        private System.Windows.Forms.Button btnExtRun;
        private System.Windows.Forms.Button btnAudit;

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
            grpExt = new System.Windows.Forms.GroupBox();
            chkExtEvents = new System.Windows.Forms.CheckBox();
            chkExtSetupApi = new System.Windows.Forms.CheckBox();
            chkExtConnected = new System.Windows.Forms.CheckBox();
            chkExtVolCache = new System.Windows.Forms.CheckBox();
            chkExtMountPts = new System.Windows.Forms.CheckBox();
            chkExtRegUsb = new System.Windows.Forms.CheckBox();
            chkExtAmcache = new System.Windows.Forms.CheckBox();
            chkExtJumpList = new System.Windows.Forms.CheckBox();
            chkExtPrefetch = new System.Windows.Forms.CheckBox();
            chkExtShellbags = new System.Windows.Forms.CheckBox();
            btnExtScan = new System.Windows.Forms.Button();
            btnExtRun = new System.Windows.Forms.Button();
            btnAudit = new System.Windows.Forms.Button();
            grpTypes.SuspendLayout();
            grpExt.SuspendLayout();
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
            grpTypes.Text = "정리할 장치 종류 (DriveCleanup)";
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
            // grpExt — 확장 정리
            //
            grpExt.Controls.Add(chkExtEvents);
            grpExt.Controls.Add(chkExtSetupApi);
            grpExt.Controls.Add(chkExtConnected);
            grpExt.Controls.Add(chkExtVolCache);
            grpExt.Controls.Add(chkExtMountPts);
            grpExt.Controls.Add(chkExtRegUsb);
            grpExt.Controls.Add(chkExtAmcache);
            grpExt.Controls.Add(chkExtJumpList);
            grpExt.Controls.Add(chkExtPrefetch);
            grpExt.Controls.Add(chkExtShellbags);
            grpExt.Controls.Add(btnExtScan);
            grpExt.Controls.Add(btnExtRun);
            grpExt.Location = new System.Drawing.Point(17, 370);
            grpExt.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            grpExt.Name = "grpExt";
            grpExt.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            grpExt.Size = new System.Drawing.Size(629, 465);
            grpExt.TabIndex = 7;
            grpExt.TabStop = false;
            grpExt.Text = "확장 정리 (DriveCleanup 미커버 영역)";
            //
            // chkExtEvents
            //
            chkExtEvents.AutoSize = true;
            chkExtEvents.Checked = true;
            chkExtEvents.CheckState = System.Windows.Forms.CheckState.Checked;
            chkExtEvents.Location = new System.Drawing.Point(21, 40);
            chkExtEvents.Name = "chkExtEvents";
            chkExtEvents.Size = new System.Drawing.Size(400, 29);
            chkExtEvents.TabIndex = 0;
            chkExtEvents.Text = "이벤트 로그 축소 (Partition/Diag, Storsvc, Kernel-PnP, System)";
            chkExtEvents.UseVisualStyleBackColor = true;
            //
            // chkExtSetupApi
            //
            chkExtSetupApi.AutoSize = true;
            chkExtSetupApi.Checked = true;
            chkExtSetupApi.CheckState = System.Windows.Forms.CheckState.Checked;
            chkExtSetupApi.Location = new System.Drawing.Point(21, 75);
            chkExtSetupApi.Name = "chkExtSetupApi";
            chkExtSetupApi.Size = new System.Drawing.Size(400, 29);
            chkExtSetupApi.TabIndex = 1;
            chkExtSetupApi.Text = "setupapi.dev.log 편집 (USBSTOR/WPDBUSENUM 섹션 제거)";
            chkExtSetupApi.UseVisualStyleBackColor = true;
            //
            // chkExtConnected
            //
            chkExtConnected.AutoSize = true;
            chkExtConnected.Checked = true;
            chkExtConnected.CheckState = System.Windows.Forms.CheckState.Checked;
            chkExtConnected.Location = new System.Drawing.Point(21, 110);
            chkExtConnected.Name = "chkExtConnected";
            chkExtConnected.Size = new System.Drawing.Size(400, 29);
            chkExtConnected.TabIndex = 2;
            chkExtConnected.Text = "연결된 USB 저장장치 강제 제거 (pnputil)";
            chkExtConnected.UseVisualStyleBackColor = true;
            //
            // chkExtVolCache
            //
            chkExtVolCache.AutoSize = true;
            chkExtVolCache.Checked = true;
            chkExtVolCache.CheckState = System.Windows.Forms.CheckState.Checked;
            chkExtVolCache.Location = new System.Drawing.Point(21, 145);
            chkExtVolCache.Name = "chkExtVolCache";
            chkExtVolCache.Size = new System.Drawing.Size(400, 29);
            chkExtVolCache.TabIndex = 3;
            chkExtVolCache.Text = "Windows Search VolumeInfoCache";
            chkExtVolCache.UseVisualStyleBackColor = true;
            //
            // chkExtMountPts
            //
            chkExtMountPts.AutoSize = true;
            chkExtMountPts.Checked = true;
            chkExtMountPts.CheckState = System.Windows.Forms.CheckState.Checked;
            chkExtMountPts.Location = new System.Drawing.Point(21, 180);
            chkExtMountPts.Name = "chkExtMountPts";
            chkExtMountPts.Size = new System.Drawing.Size(400, 29);
            chkExtMountPts.TabIndex = 4;
            chkExtMountPts.Text = "MountPoints2 잔여 GUID (사용자 하이브)";
            chkExtMountPts.UseVisualStyleBackColor = true;
            //
            // chkExtRegUsb
            //
            chkExtRegUsb.AutoSize = true;
            chkExtRegUsb.Checked = true;
            chkExtRegUsb.CheckState = System.Windows.Forms.CheckState.Checked;
            chkExtRegUsb.Location = new System.Drawing.Point(21, 215);
            chkExtRegUsb.Name = "chkExtRegUsb";
            chkExtRegUsb.Size = new System.Drawing.Size(400, 29);
            chkExtRegUsb.TabIndex = 5;
            chkExtRegUsb.Text = "Enum\\USB / DeviceContainers 저장장치 잔여 노드";
            chkExtRegUsb.UseVisualStyleBackColor = true;
            //
            // chkExtAmcache
            //
            chkExtAmcache.AutoSize = true;
            chkExtAmcache.Checked = true;
            chkExtAmcache.CheckState = System.Windows.Forms.CheckState.Checked;
            chkExtAmcache.Location = new System.Drawing.Point(21, 250);
            chkExtAmcache.Name = "chkExtAmcache";
            chkExtAmcache.Size = new System.Drawing.Size(400, 29);
            chkExtAmcache.TabIndex = 6;
            chkExtAmcache.Text = "Amcache 저장장치 기록 (InventoryDevice*)";
            chkExtAmcache.UseVisualStyleBackColor = true;
            //
            // chkExtJumpList
            //
            chkExtJumpList.AutoSize = true;
            chkExtJumpList.Checked = true;
            chkExtJumpList.CheckState = System.Windows.Forms.CheckState.Checked;
            chkExtJumpList.Location = new System.Drawing.Point(21, 285);
            chkExtJumpList.Name = "chkExtJumpList";
            chkExtJumpList.Size = new System.Drawing.Size(400, 29);
            chkExtJumpList.TabIndex = 7;
            chkExtJumpList.Text = "점프리스트 외장 볼륨 참조 파일 (AutomaticDestinations)";
            chkExtJumpList.UseVisualStyleBackColor = true;
            //
            // chkExtPrefetch
            //
            chkExtPrefetch.AutoSize = true;
            chkExtPrefetch.Checked = true;
            chkExtPrefetch.CheckState = System.Windows.Forms.CheckState.Checked;
            chkExtPrefetch.Location = new System.Drawing.Point(21, 320);
            chkExtPrefetch.Name = "chkExtPrefetch";
            chkExtPrefetch.Size = new System.Drawing.Size(400, 29);
            chkExtPrefetch.TabIndex = 8;
            chkExtPrefetch.Text = "Prefetch 전체 삭제 (외장 참조 + 삭제도구 실행 흔적)";
            chkExtPrefetch.UseVisualStyleBackColor = true;
            //
            // chkExtShellbags
            //
            chkExtShellbags.AutoSize = true;
            chkExtShellbags.Checked = true;
            chkExtShellbags.CheckState = System.Windows.Forms.CheckState.Checked;
            chkExtShellbags.Location = new System.Drawing.Point(21, 355);
            chkExtShellbags.Name = "chkExtShellbags";
            chkExtShellbags.Size = new System.Drawing.Size(400, 29);
            chkExtShellbags.TabIndex = 9;
            chkExtShellbags.Text = "Shellbags 외장 드라이브 컨테이너 초기화 (E:/F: 폴더 트리)";
            chkExtShellbags.UseVisualStyleBackColor = true;
            //
            // btnExtScan
            //
            btnExtScan.Location = new System.Drawing.Point(21, 395);
            btnExtScan.Name = "btnExtScan";
            btnExtScan.Size = new System.Drawing.Size(180, 40);
            btnExtScan.TabIndex = 8;
            btnExtScan.Text = "확장 미리보기";
            btnExtScan.UseVisualStyleBackColor = true;
            //
            // btnExtRun
            //
            btnExtRun.Location = new System.Drawing.Point(220, 395);
            btnExtRun.Name = "btnExtRun";
            btnExtRun.Size = new System.Drawing.Size(180, 40);
            btnExtRun.TabIndex = 9;
            btnExtRun.Text = "확장 정리 실행";
            btnExtRun.UseVisualStyleBackColor = true;
            //
            // btnAudit
            //
            btnAudit.Location = new System.Drawing.Point(17, 845);
            btnAudit.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnAudit.Name = "btnAudit";
            btnAudit.Size = new System.Drawing.Size(629, 45);
            btnAudit.TabIndex = 9;
            btnAudit.Text = "현재 외장 드라이브 분석 로그 출력 (JSON export)";
            btnAudit.UseVisualStyleBackColor = true;
            //
            // txtLog
            //
            txtLog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtLog.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            txtLog.ForeColor = System.Drawing.Color.Gainsboro;
            txtLog.Location = new System.Drawing.Point(17, 910);
            txtLog.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtLog.Size = new System.Drawing.Size(655, 260);
            txtLog.TabIndex = 8;
            txtLog.WordWrap = false;
            //
            // DriveCleanupForm
            //
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(691, 1200);
            Controls.Add(txtLog);
            Controls.Add(grpExt);
            Controls.Add(btnAudit);
            Controls.Add(lblWarn);
            Controls.Add(btnCancel);
            Controls.Add(btnClean);
            Controls.Add(btnScan);
            Controls.Add(grpTypes);
            Controls.Add(lblIntro);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            MinimumSize = new System.Drawing.Size(700, 1230);
            Name = "DriveCleanupForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "외장 드라이브 흔적 정리";
            grpTypes.ResumeLayout(false);
            grpTypes.PerformLayout();
            grpExt.ResumeLayout(false);
            grpExt.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
