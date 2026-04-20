namespace BackupRestoreTool
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

        private void InitializeComponent()
        {
            tcMain = new TabControl();
            tpBackup = new TabPage();
            lblSourcePart = new Label();
            cmbBackupSource = new ComboBox();
            lblBackupDest = new Label();
            txtBackupDest = new TextBox();
            btnBrowseBackup = new Button();
            lblCompression = new Label();
            cbCompression = new ComboBox();
            btnStartBackup = new Button();
            tpRestore = new TabPage();
            lblWimPath = new Label();
            txtWimPath = new TextBox();
            btnBrowseWim = new Button();
            lblWimIndex = new Label();
            cmbWimIndex = new ComboBox();
            gbStrategy = new GroupBox();
            rbPartRestore = new RadioButton();
            rbWholeDisk = new RadioButton();
            lblTarget = new Label();
            cmbRestoreTarget = new ComboBox();
            chkCreateBoot = new CheckBox();
            gbBoot = new GroupBox();
            rbUEFI = new RadioButton();
            rbBIOS = new RadioButton();
            gbPartitionLayout = new GroupBox();
            lblBootSize = new Label();
            numBootSizeMB = new NumericUpDown();
            lblWinSize = new Label();
            numWinSizeGB = new NumericUpDown();
            chkCreateRecovery = new CheckBox();
            numRecoverySizeMB = new NumericUpDown();
            lblDataSize = new Label();
            pnlVisualMap = new Panel();
            btnStartRestore = new Button();
            tpBootFix = new TabPage();
            lblBootFixTitle = new Label();
            lblBootFixInfo = new Label();
            lblInstalledOS = new Label();
            btnDriverBackup = new Button();
            btnDriverRestore = new Button();
            btnAutoBootFix = new Button();
            lblBootFixDesc = new Label();
            btnHealthCheck = new Button();
            lblHealthCheckDesc = new Label();
            tpSettings = new TabPage();
            lblLang = new Label();
            cbLang = new ComboBox();
            lblHeader = new Label();
            btnAbout = new Button();
            lblBootMode = new Label();
            pbMain = new ProgressBar();
            lblProgressStatus = new Label();
            btnClearLog = new Button();
            rtbLog = new RichTextBox();
            pnlFooter = new Panel();
            lnkGithub = new LinkLabel();
            lnkWeb = new LinkLabel();
            chkPostAction = new CheckBox();
            cmbPostAction = new ComboBox();
            tcMain.SuspendLayout();
            tpBackup.SuspendLayout();
            tpRestore.SuspendLayout();
            gbStrategy.SuspendLayout();
            gbBoot.SuspendLayout();
            gbPartitionLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numBootSizeMB).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numWinSizeGB).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numRecoverySizeMB).BeginInit();
            tpBootFix.SuspendLayout();
            tpSettings.SuspendLayout();
            pnlFooter.SuspendLayout();
            SuspendLayout();
            // 
            // tcMain
            // 
            tcMain.Controls.Add(tpBackup);
            tcMain.Controls.Add(tpRestore);
            tcMain.Controls.Add(tpBootFix);
            tcMain.Controls.Add(tpSettings);
            tcMain.DrawMode = TabDrawMode.OwnerDrawFixed;
            tcMain.ItemSize = new Size(200, 35);
            tcMain.Location = new Point(12, 60);
            tcMain.Name = "tcMain";
            tcMain.SelectedIndex = 0;
            tcMain.Size = new Size(810, 360);
            tcMain.SizeMode = TabSizeMode.Fixed;
            tcMain.TabIndex = 1;
            tcMain.DrawItem += tcMain_DrawItem;
            // 
            // tpBackup
            // 
            tpBackup.Controls.Add(lblSourcePart);
            tpBackup.Controls.Add(cmbBackupSource);
            tpBackup.Controls.Add(lblBackupDest);
            tpBackup.Controls.Add(txtBackupDest);
            tpBackup.Controls.Add(btnBrowseBackup);
            tpBackup.Controls.Add(lblCompression);
            tpBackup.Controls.Add(cbCompression);
            tpBackup.Controls.Add(btnStartBackup);
            tpBackup.Location = new Point(4, 39);
            tpBackup.Name = "tpBackup";
            tpBackup.Padding = new Padding(3);
            tpBackup.Size = new Size(802, 317);
            tpBackup.TabIndex = 0;
            tpBackup.Text = "Backup";
            tpBackup.UseVisualStyleBackColor = true;
            // 
            // lblSourcePart
            // 
            lblSourcePart.Location = new Point(63, 19);
            lblSourcePart.Name = "lblSourcePart";
            lblSourcePart.Size = new Size(99, 23);
            lblSourcePart.TabIndex = 0;
            lblSourcePart.Tag = "LBL_SourcePart";
            lblSourcePart.Text = "Source:";
            // 
            // cmbBackupSource
            // 
            cmbBackupSource.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBackupSource.Location = new Point(63, 44);
            cmbBackupSource.Name = "cmbBackupSource";
            cmbBackupSource.Size = new Size(658, 23);
            cmbBackupSource.TabIndex = 1;
            // 
            // lblBackupDest
            // 
            lblBackupDest.Location = new Point(63, 99);
            lblBackupDest.Name = "lblBackupDest";
            lblBackupDest.Size = new Size(99, 23);
            lblBackupDest.TabIndex = 2;
            lblBackupDest.Tag = "LBL_BackupDest";
            lblBackupDest.Text = "Target WIM:";
            // 
            // txtBackupDest
            // 
            txtBackupDest.Location = new Point(63, 123);
            txtBackupDest.Name = "txtBackupDest";
            txtBackupDest.ReadOnly = true;
            txtBackupDest.Size = new Size(573, 23);
            txtBackupDest.TabIndex = 3;
            // 
            // btnBrowseBackup
            // 
            btnBrowseBackup.FlatStyle = FlatStyle.Flat;
            btnBrowseBackup.Location = new Point(642, 121);
            btnBrowseBackup.Name = "btnBrowseBackup";
            btnBrowseBackup.Size = new Size(79, 28);
            btnBrowseBackup.TabIndex = 4;
            btnBrowseBackup.Tag = "BTN_Browse";
            btnBrowseBackup.Text = "Browse";
            // 
            // lblCompression
            // 
            lblCompression.Location = new Point(63, 169);
            lblCompression.Name = "lblCompression";
            lblCompression.Size = new Size(99, 23);
            lblCompression.TabIndex = 5;
            lblCompression.Tag = "LBL_Compression";
            lblCompression.Text = "Compression:";
            // 
            // cbCompression
            // 
            cbCompression.DropDownStyle = ComboBoxStyle.DropDownList;
            cbCompression.Location = new Point(63, 194);
            cbCompression.Name = "cbCompression";
            cbCompression.Size = new Size(658, 23);
            cbCompression.TabIndex = 6;
            // 
            // btnStartBackup
            // 
            btnStartBackup.BackColor = Color.FromArgb(0, 120, 215);
            btnStartBackup.FlatStyle = FlatStyle.Flat;
            btnStartBackup.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnStartBackup.ForeColor = Color.White;
            btnStartBackup.Location = new Point(184, 240);
            btnStartBackup.Name = "btnStartBackup";
            btnStartBackup.Size = new Size(399, 45);
            btnStartBackup.TabIndex = 7;
            btnStartBackup.Tag = "BTN_StartBackup";
            btnStartBackup.Text = "START BACKUP";
            btnStartBackup.UseVisualStyleBackColor = false;
            // 
            // tpRestore
            // 
            tpRestore.Controls.Add(lblWimPath);
            tpRestore.Controls.Add(txtWimPath);
            tpRestore.Controls.Add(btnBrowseWim);
            tpRestore.Controls.Add(lblWimIndex);
            tpRestore.Controls.Add(cmbWimIndex);
            tpRestore.Controls.Add(gbStrategy);
            tpRestore.Controls.Add(lblTarget);
            tpRestore.Controls.Add(cmbRestoreTarget);
            tpRestore.Controls.Add(chkCreateBoot);
            tpRestore.Controls.Add(gbBoot);
            tpRestore.Controls.Add(gbPartitionLayout);
            tpRestore.Controls.Add(btnStartRestore);
            tpRestore.Location = new Point(4, 39);
            tpRestore.Name = "tpRestore";
            tpRestore.Padding = new Padding(3);
            tpRestore.Size = new Size(802, 317);
            tpRestore.TabIndex = 1;
            tpRestore.Text = "Restore";
            tpRestore.UseVisualStyleBackColor = true;
            // 
            // lblWimPath
            // 
            lblWimPath.Location = new Point(25, 20);
            lblWimPath.Name = "lblWimPath";
            lblWimPath.Size = new Size(100, 23);
            lblWimPath.TabIndex = 0;
            lblWimPath.Tag = "LBL_WimPath";
            lblWimPath.Text = "Image Path:";
            // 
            // txtWimPath
            // 
            txtWimPath.Location = new Point(25, 45);
            txtWimPath.Name = "txtWimPath";
            txtWimPath.ReadOnly = true;
            txtWimPath.Size = new Size(310, 23);
            txtWimPath.TabIndex = 1;
            // 
            // btnBrowseWim
            // 
            btnBrowseWim.FlatStyle = FlatStyle.Flat;
            btnBrowseWim.Location = new Point(345, 43);
            btnBrowseWim.Name = "btnBrowseWim";
            btnBrowseWim.Size = new Size(80, 28);
            btnBrowseWim.TabIndex = 2;
            btnBrowseWim.Tag = "BTN_Browse";
            btnBrowseWim.Text = "Browse";
            // 
            // lblWimIndex
            // 
            lblWimIndex.Location = new Point(25, 85);
            lblWimIndex.Name = "lblWimIndex";
            lblWimIndex.Size = new Size(100, 23);
            lblWimIndex.TabIndex = 3;
            lblWimIndex.Tag = "LBL_WimIndex";
            lblWimIndex.Text = "Index:";
            // 
            // cmbWimIndex
            // 
            cmbWimIndex.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbWimIndex.Location = new Point(25, 110);
            cmbWimIndex.Name = "cmbWimIndex";
            cmbWimIndex.Size = new Size(400, 23);
            cmbWimIndex.TabIndex = 4;
            // 
            // gbStrategy
            // 
            gbStrategy.Controls.Add(rbPartRestore);
            gbStrategy.Controls.Add(rbWholeDisk);
            gbStrategy.Location = new Point(460, 15);
            gbStrategy.Name = "gbStrategy";
            gbStrategy.Size = new Size(320, 80);
            gbStrategy.TabIndex = 5;
            gbStrategy.TabStop = false;
            gbStrategy.Tag = "GB_Strategy";
            gbStrategy.Text = "Method";
            // 
            // rbPartRestore
            // 
            rbPartRestore.AutoSize = true;
            rbPartRestore.Checked = true;
            rbPartRestore.Location = new Point(20, 35);
            rbPartRestore.Name = "rbPartRestore";
            rbPartRestore.Size = new Size(70, 19);
            rbPartRestore.TabIndex = 0;
            rbPartRestore.TabStop = true;
            rbPartRestore.Tag = "RB_PartOnly";
            rbPartRestore.Text = "Partition";
            // 
            // rbWholeDisk
            // 
            rbWholeDisk.AutoSize = true;
            rbWholeDisk.Location = new Point(125, 35);
            rbWholeDisk.Name = "rbWholeDisk";
            rbWholeDisk.Size = new Size(84, 19);
            rbWholeDisk.TabIndex = 1;
            rbWholeDisk.Tag = "RB_DiskRestore";
            rbWholeDisk.Text = "Whole Disk";
            // 
            // lblTarget
            // 
            lblTarget.Location = new Point(25, 145);
            lblTarget.Name = "lblTarget";
            lblTarget.Size = new Size(100, 23);
            lblTarget.TabIndex = 6;
            lblTarget.Tag = "LBL_Target";
            lblTarget.Text = "Target:";
            // 
            // cmbRestoreTarget
            // 
            cmbRestoreTarget.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRestoreTarget.Location = new Point(25, 170);
            cmbRestoreTarget.Name = "cmbRestoreTarget";
            cmbRestoreTarget.Size = new Size(400, 23);
            cmbRestoreTarget.TabIndex = 7;
            // 
            // chkCreateBoot
            // 
            chkCreateBoot.AutoSize = true;
            chkCreateBoot.Checked = true;
            chkCreateBoot.CheckState = CheckState.Checked;
            chkCreateBoot.Location = new Point(460, 105);
            chkCreateBoot.Name = "chkCreateBoot";
            chkCreateBoot.Size = new Size(87, 19);
            chkCreateBoot.TabIndex = 8;
            chkCreateBoot.Tag = "CHK_Boot";
            chkCreateBoot.Text = "Repair Boot";
            // 
            // gbBoot
            // 
            gbBoot.Controls.Add(rbUEFI);
            gbBoot.Controls.Add(rbBIOS);
            gbBoot.Location = new Point(460, 135);
            gbBoot.Name = "gbBoot";
            gbBoot.Size = new Size(320, 60);
            gbBoot.TabIndex = 9;
            gbBoot.TabStop = false;
            gbBoot.Tag = "GB_Boot";
            gbBoot.Text = "Boot Mode";
            // 
            // rbUEFI
            // 
            rbUEFI.AutoSize = true;
            rbUEFI.Checked = true;
            rbUEFI.Location = new Point(20, 25);
            rbUEFI.Name = "rbUEFI";
            rbUEFI.Size = new Size(81, 19);
            rbUEFI.TabIndex = 0;
            rbUEFI.TabStop = true;
            rbUEFI.Text = "UEFI (GPT)";
            // 
            // rbBIOS
            // 
            rbBIOS.AutoSize = true;
            rbBIOS.Location = new Point(140, 25);
            rbBIOS.Name = "rbBIOS";
            rbBIOS.Size = new Size(86, 19);
            rbBIOS.TabIndex = 1;
            rbBIOS.Text = "BIOS (MBR)";
            // 
            // gbPartitionLayout
            // 
            gbPartitionLayout.Controls.Add(lblBootSize);
            gbPartitionLayout.Controls.Add(numBootSizeMB);
            gbPartitionLayout.Controls.Add(lblWinSize);
            gbPartitionLayout.Controls.Add(numWinSizeGB);
            gbPartitionLayout.Controls.Add(chkCreateRecovery);
            gbPartitionLayout.Controls.Add(numRecoverySizeMB);
            gbPartitionLayout.Controls.Add(lblDataSize);
            gbPartitionLayout.Controls.Add(pnlVisualMap);
            gbPartitionLayout.Location = new Point(22, 202);
            gbPartitionLayout.Name = "gbPartitionLayout";
            gbPartitionLayout.Size = new Size(402, 103);
            gbPartitionLayout.TabIndex = 10;
            gbPartitionLayout.TabStop = false;
            gbPartitionLayout.Tag = "GB_Layout";
            gbPartitionLayout.Text = "Partition Sizing";
            gbPartitionLayout.Visible = false;
            // 
            // lblBootSize
            // 
            lblBootSize.Location = new Point(5, 19);
            lblBootSize.Name = "lblBootSize";
            lblBootSize.Size = new Size(80, 23);
            lblBootSize.TabIndex = 0;
            lblBootSize.Tag = "LBL_BootSize";
            lblBootSize.Text = "Boot (MB):";
            // 
            // numBootSizeMB
            // 
            numBootSizeMB.Location = new Point(7, 42);
            numBootSizeMB.Maximum = new decimal(new int[] { 2048, 0, 0, 0 });
            numBootSizeMB.Minimum = new decimal(new int[] { 200, 0, 0, 0 });
            numBootSizeMB.Name = "numBootSizeMB";
            numBootSizeMB.Size = new Size(70, 23);
            numBootSizeMB.TabIndex = 1;
            numBootSizeMB.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // lblWinSize
            // 
            lblWinSize.Location = new Point(93, 18);
            lblWinSize.Name = "lblWinSize";
            lblWinSize.Size = new Size(70, 23);
            lblWinSize.TabIndex = 2;
            lblWinSize.Tag = "LBL_WinSize";
            lblWinSize.Text = "Win (GB):";
            // 
            // numWinSizeGB
            // 
            numWinSizeGB.Location = new Point(96, 42);
            numWinSizeGB.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numWinSizeGB.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            numWinSizeGB.Name = "numWinSizeGB";
            numWinSizeGB.Size = new Size(70, 23);
            numWinSizeGB.TabIndex = 3;
            numWinSizeGB.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // chkCreateRecovery
            // 
            chkCreateRecovery.AutoSize = true;
            chkCreateRecovery.Checked = true;
            chkCreateRecovery.CheckState = CheckState.Checked;
            chkCreateRecovery.Location = new Point(242, 14);
            chkCreateRecovery.Name = "chkCreateRecovery";
            chkCreateRecovery.Size = new Size(77, 19);
            chkCreateRecovery.TabIndex = 4;
            chkCreateRecovery.Tag = "CHK_Rec";
            chkCreateRecovery.Text = "Recovery:";
            // 
            // numRecoverySizeMB
            // 
            numRecoverySizeMB.Location = new Point(280, 41);
            numRecoverySizeMB.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
            numRecoverySizeMB.Minimum = new decimal(new int[] { 800, 0, 0, 0 });
            numRecoverySizeMB.Name = "numRecoverySizeMB";
            numRecoverySizeMB.Size = new Size(70, 23);
            numRecoverySizeMB.TabIndex = 5;
            numRecoverySizeMB.Value = new decimal(new int[] { 800, 0, 0, 0 });
            // 
            // lblDataSize
            // 
            lblDataSize.AutoSize = true;
            lblDataSize.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblDataSize.ForeColor = Color.LimeGreen;
            lblDataSize.Location = new Point(172, 46);
            lblDataSize.Name = "lblDataSize";
            lblDataSize.Size = new Size(70, 15);
            lblDataSize.TabIndex = 6;
            lblDataSize.Tag = "LBL_Remaining";
            lblDataSize.Text = "DATA: 0 GB";
            // 
            // pnlVisualMap
            // 
            pnlVisualMap.BackColor = Color.FromArgb(45, 45, 45);
            pnlVisualMap.BorderStyle = BorderStyle.FixedSingle;
            pnlVisualMap.Location = new Point(6, 68);
            pnlVisualMap.Name = "pnlVisualMap";
            pnlVisualMap.Size = new Size(390, 30);
            pnlVisualMap.TabIndex = 7;
            pnlVisualMap.Paint += PnlVisualMap_Paint;
            pnlVisualMap.MouseMove += PnlVisualMap_MouseMove;
            // 
            // btnStartRestore
            // 
            btnStartRestore.BackColor = Color.FromArgb(209, 52, 56);
            btnStartRestore.FlatStyle = FlatStyle.Flat;
            btnStartRestore.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnStartRestore.ForeColor = Color.White;
            btnStartRestore.Location = new Point(460, 202);
            btnStartRestore.Name = "btnStartRestore";
            btnStartRestore.Size = new Size(320, 42);
            btnStartRestore.TabIndex = 11;
            btnStartRestore.Tag = "BTN_StartRestore";
            btnStartRestore.Text = "START RESTORE";
            btnStartRestore.UseVisualStyleBackColor = false;
            // 
            // tpBootFix
            // 
            tpBootFix.Controls.Add(lblBootFixTitle);
            tpBootFix.Controls.Add(lblBootFixInfo);
            tpBootFix.Controls.Add(lblInstalledOS);
            tpBootFix.Controls.Add(btnDriverBackup);
            tpBootFix.Controls.Add(btnDriverRestore);
            tpBootFix.Controls.Add(btnAutoBootFix);
            tpBootFix.Controls.Add(lblBootFixDesc);
            tpBootFix.Controls.Add(btnHealthCheck);
            tpBootFix.Controls.Add(lblHealthCheckDesc);
            tpBootFix.Location = new Point(4, 39);
            tpBootFix.Name = "tpBootFix";
            tpBootFix.Size = new Size(802, 317);
            tpBootFix.TabIndex = 2;
            tpBootFix.Text = "Boot Fix";
            tpBootFix.UseVisualStyleBackColor = true;
            // 
            // lblBootFixTitle
            // 
            lblBootFixTitle.AutoSize = true;
            lblBootFixTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblBootFixTitle.Location = new Point(30, 30);
            lblBootFixTitle.Name = "lblBootFixTitle";
            lblBootFixTitle.Size = new Size(153, 30);
            lblBootFixTitle.TabIndex = 0;
            lblBootFixTitle.Tag = "LBL_BootFixTitle";
            lblBootFixTitle.Text = "Auto Boot Fix";
            // 
            // lblBootFixInfo
            // 
            lblBootFixInfo.ForeColor = Color.Gray;
            lblBootFixInfo.Location = new Point(30, 80);
            lblBootFixInfo.Name = "lblBootFixInfo";
            lblBootFixInfo.Size = new Size(476, 21);
            lblBootFixInfo.TabIndex = 1;
            lblBootFixInfo.Tag = "LBL_BootFixInfo";
            lblBootFixInfo.Text = "Select your Windows partition and click scan to repair boot files.";
            // 
            // lblInstalledOS
            // 
            lblInstalledOS.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblInstalledOS.ForeColor = Color.FromArgb(0, 120, 215);
            lblInstalledOS.Location = new Point(30, 130);
            lblInstalledOS.Name = "lblInstalledOS";
            lblInstalledOS.Size = new Size(570, 30);
            lblInstalledOS.TabIndex = 3;
            // 
            // btnDriverBackup
            // 
            btnDriverBackup.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDriverBackup.BackColor = Color.Teal;
            btnDriverBackup.FlatStyle = FlatStyle.Flat;
            btnDriverBackup.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnDriverBackup.ForeColor = Color.White;
            btnDriverBackup.Location = new Point(592, 26);
            btnDriverBackup.Name = "btnDriverBackup";
            btnDriverBackup.Size = new Size(198, 42);
            btnDriverBackup.TabIndex = 6;
            btnDriverBackup.Tag = "UI_BTN_DriverBackup";
            btnDriverBackup.Text = "Driver Backup";
            btnDriverBackup.UseVisualStyleBackColor = false;
            btnDriverBackup.Click += btnDriverBackup_Click;
            // 
            // btnDriverRestore
            // 
            btnDriverRestore.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDriverRestore.BackColor = Color.FromArgb(128, 128, 255);
            btnDriverRestore.FlatStyle = FlatStyle.Flat;
            btnDriverRestore.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnDriverRestore.ForeColor = Color.White;
            btnDriverRestore.Location = new Point(388, 26);
            btnDriverRestore.Name = "btnDriverRestore";
            btnDriverRestore.Size = new Size(198, 42);
            btnDriverRestore.TabIndex = 7;
            btnDriverRestore.Tag = "UI_BTN_DriverRestore";
            btnDriverRestore.Text = "Driver Restore";
            btnDriverRestore.UseVisualStyleBackColor = false;
            btnDriverRestore.Click += btnDriverRestore_Click;
            // 
            // btnAutoBootFix
            // 
            btnAutoBootFix.BackColor = Color.FromArgb(100, 100, 100);
            btnAutoBootFix.FlatStyle = FlatStyle.Flat;
            btnAutoBootFix.ForeColor = Color.White;
            btnAutoBootFix.Location = new Point(30, 202);
            btnAutoBootFix.Name = "btnAutoBootFix";
            btnAutoBootFix.Size = new Size(300, 50);
            btnAutoBootFix.TabIndex = 2;
            btnAutoBootFix.Tag = "BTN_BootFix";
            btnAutoBootFix.Text = "SCAN & REPAIR";
            btnAutoBootFix.UseVisualStyleBackColor = false;
            // 
            // lblBootFixDesc
            // 
            lblBootFixDesc.ForeColor = Color.Gray;
            lblBootFixDesc.Location = new Point(34, 255);
            lblBootFixDesc.Name = "lblBootFixDesc";
            lblBootFixDesc.Size = new Size(296, 40);
            lblBootFixDesc.TabIndex = 5;
            lblBootFixDesc.Tag = "UI_LBL_BootFixDesc";
            lblBootFixDesc.Text = "Automatically repair boot files (BCD) if the system fails to start.";
            // 
            // btnHealthCheck
            // 
            btnHealthCheck.BackColor = Color.FromArgb(100, 100, 100);
            btnHealthCheck.FlatStyle = FlatStyle.Flat;
            btnHealthCheck.ForeColor = Color.White;
            btnHealthCheck.Location = new Point(338, 202);
            btnHealthCheck.Name = "btnHealthCheck";
            btnHealthCheck.Size = new Size(300, 50);
            btnHealthCheck.TabIndex = 3;
            btnHealthCheck.Tag = "UI_BTN_HealthCheck";
            btnHealthCheck.Text = "CHECK & REPAIR HEALTH";
            btnHealthCheck.UseVisualStyleBackColor = false;
            // 
            // lblHealthCheckDesc
            // 
            lblHealthCheckDesc.ForeColor = Color.Gray;
            lblHealthCheckDesc.Location = new Point(338, 255);
            lblHealthCheckDesc.Name = "lblHealthCheckDesc";
            lblHealthCheckDesc.Size = new Size(300, 40);
            lblHealthCheckDesc.TabIndex = 4;
            lblHealthCheckDesc.Tag = "UI_LBL_HealthCheckInfo";
            lblHealthCheckDesc.Text = "Scan and repair corrupted system files and Windows image (DISM & SFC).";
            // 
            // tpSettings
            // 
            tpSettings.Controls.Add(lblLang);
            tpSettings.Controls.Add(cbLang);
            tpSettings.Location = new Point(4, 39);
            tpSettings.Name = "tpSettings";
            tpSettings.Size = new Size(802, 317);
            tpSettings.TabIndex = 3;
            tpSettings.Text = "Settings";
            tpSettings.UseVisualStyleBackColor = true;
            // 
            // lblLang
            // 
            lblLang.Location = new Point(30, 26);
            lblLang.Name = "lblLang";
            lblLang.Size = new Size(100, 23);
            lblLang.TabIndex = 0;
            lblLang.Tag = "LBL_Lang";
            lblLang.Text = "Language:";
            // 
            // cbLang
            // 
            cbLang.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLang.Location = new Point(133, 22);
            cbLang.Name = "cbLang";
            cbLang.Size = new Size(134, 23);
            cbLang.TabIndex = 1;
            // 
            // lblHeader
            // 
            lblHeader.AutoSize = true;
            lblHeader.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            lblHeader.Location = new Point(12, 12);
            lblHeader.Name = "lblHeader";
            lblHeader.Size = new Size(358, 32);
            lblHeader.TabIndex = 0;
            lblHeader.Tag = "UI_Header";
            lblHeader.Text = "Windows Backup / Restore Tool";
            // 
            // btnAbout
            // 
            btnAbout.FlatStyle = FlatStyle.Flat;
            btnAbout.Location = new Point(747, 15);
            btnAbout.Name = "btnAbout";
            btnAbout.Size = new Size(75, 30);
            btnAbout.TabIndex = 6;
            btnAbout.Tag = "BTN_About";
            btnAbout.Text = "About";
            // 
            // lblBootMode
            // 
            lblBootMode.BackColor = Color.FromArgb(240, 240, 240);
            lblBootMode.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblBootMode.Location = new Point(522, 15);
            lblBootMode.Name = "lblBootMode";
            lblBootMode.Size = new Size(220, 30);
            lblBootMode.TabIndex = 5;
            lblBootMode.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pbMain
            // 
            pbMain.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            pbMain.Location = new Point(12, 455);
            pbMain.Name = "pbMain";
            pbMain.Size = new Size(500, 23);
            pbMain.TabIndex = 2;
            // 
            // lblProgressStatus
            // 
            lblProgressStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblProgressStatus.Location = new Point(121, 430);
            lblProgressStatus.Name = "lblProgressStatus";
            lblProgressStatus.Size = new Size(701, 23);
            lblProgressStatus.TabIndex = 3;
            lblProgressStatus.Text = "Ready";
            lblProgressStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnClearLog
            // 
            btnClearLog.FlatStyle = FlatStyle.Flat;
            btnClearLog.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 162);
            btnClearLog.Location = new Point(12, 430);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Size = new Size(105, 23);
            btnClearLog.TabIndex = 10;
            btnClearLog.Text = "Clear";
            btnClearLog.TextAlign = ContentAlignment.TopCenter;
            btnClearLog.Click += btnClearLog_Click;
            // 
            // rtbLog
            // 
            rtbLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbLog.BackColor = Color.FromArgb(30, 30, 30);
            rtbLog.ForeColor = SystemColors.Window;
            rtbLog.Location = new Point(12, 485);
            rtbLog.Name = "rtbLog";
            rtbLog.Size = new Size(810, 166);
            rtbLog.TabIndex = 4;
            rtbLog.Text = "";
            // 
            // pnlFooter
            // 
            pnlFooter.BackColor = Color.FromArgb(240, 240, 240);
            pnlFooter.Controls.Add(lnkGithub);
            pnlFooter.Controls.Add(lnkWeb);
            pnlFooter.Dock = DockStyle.Bottom;
            pnlFooter.Location = new Point(0, 657);
            pnlFooter.Name = "pnlFooter";
            pnlFooter.Size = new Size(834, 23);
            pnlFooter.TabIndex = 7;
            // 
            // lnkGithub
            // 
            lnkGithub.AutoSize = true;
            lnkGithub.Location = new Point(777, 3);
            lnkGithub.Name = "lnkGithub";
            lnkGithub.Size = new Size(45, 15);
            lnkGithub.TabIndex = 2;
            lnkGithub.TabStop = true;
            lnkGithub.Text = "GitHub";
            // 
            // lnkWeb
            // 
            lnkWeb.AutoSize = true;
            lnkWeb.Location = new Point(9, 3);
            lnkWeb.Name = "lnkWeb";
            lnkWeb.Size = new Size(53, 15);
            lnkWeb.TabIndex = 1;
            lnkWeb.TabStop = true;
            lnkWeb.Text = "Web Site";
            // 
            // chkPostAction
            // 
            chkPostAction.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            chkPostAction.Location = new Point(531, 457);
            chkPostAction.Name = "chkPostAction";
            chkPostAction.Size = new Size(104, 23);
            chkPostAction.TabIndex = 8;
            chkPostAction.Text = "On Finish:";
            chkPostAction.UseVisualStyleBackColor = true;
            // 
            // cmbPostAction
            // 
            cmbPostAction.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            cmbPostAction.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPostAction.Location = new Point(655, 455);
            cmbPostAction.Name = "cmbPostAction";
            cmbPostAction.Size = new Size(163, 23);
            cmbPostAction.TabIndex = 9;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.LightGray;
            ClientSize = new Size(834, 680);
            Controls.Add(tcMain);
            Controls.Add(lblHeader);
            Controls.Add(btnAbout);
            Controls.Add(btnClearLog);
            Controls.Add(lblBootMode);
            Controls.Add(pbMain);
            Controls.Add(chkPostAction);
            Controls.Add(cmbPostAction);
            Controls.Add(lblProgressStatus);
            Controls.Add(rtbLog);
            Controls.Add(pnlFooter);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Windows Backup / Restore Tool v3 | made by Abdullah ERTÜRK";
            tcMain.ResumeLayout(false);
            tpBackup.ResumeLayout(false);
            tpBackup.PerformLayout();
            tpRestore.ResumeLayout(false);
            tpRestore.PerformLayout();
            gbStrategy.ResumeLayout(false);
            gbStrategy.PerformLayout();
            gbBoot.ResumeLayout(false);
            gbBoot.PerformLayout();
            gbPartitionLayout.ResumeLayout(false);
            gbPartitionLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numBootSizeMB).EndInit();
            ((System.ComponentModel.ISupportInitialize)numWinSizeGB).EndInit();
            ((System.ComponentModel.ISupportInitialize)numRecoverySizeMB).EndInit();
            tpBootFix.ResumeLayout(false);
            tpBootFix.PerformLayout();
            tpSettings.ResumeLayout(false);
            pnlFooter.ResumeLayout(false);
            pnlFooter.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tcMain;
        private System.Windows.Forms.TabPage tpBackup;
        private System.Windows.Forms.TabPage tpRestore;
        private System.Windows.Forms.TabPage tpBootFix;
        private System.Windows.Forms.TabPage tpSettings;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Button btnAbout;
        private System.Windows.Forms.Label lblBootMode;
        private System.Windows.Forms.ProgressBar pbMain;
        private System.Windows.Forms.Label lblProgressStatus;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.Panel pnlFooter;
        private System.Windows.Forms.LinkLabel lnkGithub;
        private System.Windows.Forms.LinkLabel lnkWeb;
        private System.Windows.Forms.CheckBox chkPostAction;
        private System.Windows.Forms.ComboBox cmbPostAction;

        // Backup Tab
        private System.Windows.Forms.Label lblSourcePart;
        private System.Windows.Forms.ComboBox cmbBackupSource;
        private System.Windows.Forms.Label lblBackupDest;
        private System.Windows.Forms.TextBox txtBackupDest;
        private System.Windows.Forms.Button btnBrowseBackup;
        private System.Windows.Forms.Label lblCompression;
        private System.Windows.Forms.ComboBox cbCompression;
        private System.Windows.Forms.Button btnStartBackup;

        // Restore Tab
        private System.Windows.Forms.Label lblWimPath;
        private System.Windows.Forms.TextBox txtWimPath;
        private System.Windows.Forms.Button btnBrowseWim;
        private System.Windows.Forms.Label lblWimIndex;
        private System.Windows.Forms.ComboBox cmbWimIndex;
        private System.Windows.Forms.GroupBox gbStrategy;
        private System.Windows.Forms.RadioButton rbPartRestore;
        private System.Windows.Forms.RadioButton rbWholeDisk;
        private System.Windows.Forms.Label lblTarget;
        private System.Windows.Forms.ComboBox cmbRestoreTarget;
        private System.Windows.Forms.CheckBox chkCreateBoot;
        private System.Windows.Forms.GroupBox gbBoot;
        private System.Windows.Forms.RadioButton rbUEFI;
        private System.Windows.Forms.RadioButton rbBIOS;
        private System.Windows.Forms.GroupBox gbPartitionLayout;
        private System.Windows.Forms.Label lblBootSize;
        private System.Windows.Forms.NumericUpDown numBootSizeMB;
        private System.Windows.Forms.Label lblWinSize;
        private System.Windows.Forms.NumericUpDown numWinSizeGB;
        private System.Windows.Forms.CheckBox chkCreateRecovery;
        private System.Windows.Forms.NumericUpDown numRecoverySizeMB;
        private System.Windows.Forms.Label lblDataSize;
        private System.Windows.Forms.Panel pnlVisualMap;
        private System.Windows.Forms.Button btnStartRestore;

        // BootFix Tab
        private System.Windows.Forms.Label lblBootFixTitle;
        private System.Windows.Forms.Label lblBootFixInfo;
        private System.Windows.Forms.Label lblInstalledOS;
        private System.Windows.Forms.Button btnAutoBootFix;
        private System.Windows.Forms.Label lblBootFixDesc;
        private System.Windows.Forms.Button btnHealthCheck; private System.Windows.Forms.Label lblHealthCheckDesc;

        // Settings Tab
        private System.Windows.Forms.Label lblLang;
        private System.Windows.Forms.ComboBox cbLang;
        private System.Windows.Forms.Button btnDriverBackup;
        private System.Windows.Forms.Button btnDriverRestore;
    }
}
