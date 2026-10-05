using System.Drawing;
using System.Windows.Forms;

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
            this.tcMain = new System.Windows.Forms.TabControl();
            this.tpBackup = new System.Windows.Forms.TabPage();
            this.lblSourcePart = new System.Windows.Forms.Label();
            this.cmbBackupSource = new System.Windows.Forms.ComboBox();
            this.lblBackupDest = new System.Windows.Forms.Label();
            this.txtBackupDest = new System.Windows.Forms.TextBox();
            this.btnBrowseBackup = new System.Windows.Forms.Button();
            this.lblCompression = new System.Windows.Forms.Label();
            this.cbCompression = new System.Windows.Forms.ComboBox();
            this.btnStartBackup = new System.Windows.Forms.Button();
            this.tpRestore = new System.Windows.Forms.TabPage();
            this.lblWimPath = new System.Windows.Forms.Label();
            this.txtWimPath = new System.Windows.Forms.TextBox();
            this.btnBrowseWim = new System.Windows.Forms.Button();
            this.lblWimIndex = new System.Windows.Forms.Label();
            this.cmbWimIndex = new System.Windows.Forms.ComboBox();
            this.gbStrategy = new System.Windows.Forms.GroupBox();
            this.rbPartRestore = new System.Windows.Forms.RadioButton();
            this.rbWholeDisk = new System.Windows.Forms.RadioButton();
            this.lblTarget = new System.Windows.Forms.Label();
            this.cmbRestoreTarget = new System.Windows.Forms.ComboBox();
            this.chkCreateBoot = new System.Windows.Forms.CheckBox();
            this.gbBoot = new System.Windows.Forms.GroupBox();
            this.rbUEFI = new System.Windows.Forms.RadioButton();
            this.rbBIOS = new System.Windows.Forms.RadioButton();
            this.gbPartitionLayout = new System.Windows.Forms.GroupBox();
            this.lblBootSize = new System.Windows.Forms.Label();
            this.numBootSizeMB = new System.Windows.Forms.NumericUpDown();
            this.lblWinSize = new System.Windows.Forms.Label();
            this.numWinSizeGB = new System.Windows.Forms.NumericUpDown();
            this.chkCreateRecovery = new System.Windows.Forms.CheckBox();
            this.numRecoverySizeMB = new System.Windows.Forms.NumericUpDown();
            this.lblDataSize = new System.Windows.Forms.Label();
            this.pnlVisualMap = new System.Windows.Forms.Panel();
            this.btnStartRestore = new System.Windows.Forms.Button();
            this.tpBootFix = new System.Windows.Forms.TabPage();
            this.lblBootFixTitle = new System.Windows.Forms.Label();
            this.lblBootFixInfo = new System.Windows.Forms.Label();
            this.lblInstalledOS = new System.Windows.Forms.Label();
            this.btnDriverBackup = new System.Windows.Forms.Button();
            this.btnDriverRestore = new System.Windows.Forms.Button();
            this.btnAutoBootFix = new System.Windows.Forms.Button();
            this.lblBootFixDesc = new System.Windows.Forms.Label();
            this.btnHealthCheck = new System.Windows.Forms.Button();
            this.lblHealthCheckDesc = new System.Windows.Forms.Label();
            this.tpSettings = new System.Windows.Forms.TabPage();
            this.lblLang = new System.Windows.Forms.Label();
            this.cbLang = new System.Windows.Forms.ComboBox();
            this.lblHeader = new System.Windows.Forms.Label();
            this.btnAbout = new System.Windows.Forms.Button();
            this.lblBootMode = new System.Windows.Forms.Label();
            this.pbMain = new System.Windows.Forms.ProgressBar();
            this.lblProgressStatus = new System.Windows.Forms.Label();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.pnlFooter = new System.Windows.Forms.Panel();
            this.lnkGithub = new System.Windows.Forms.LinkLabel();
            this.lnkWeb = new System.Windows.Forms.LinkLabel();
            this.chkPostAction = new System.Windows.Forms.CheckBox();
            this.cmbPostAction = new System.Windows.Forms.ComboBox();
            this.tcMain.SuspendLayout();
            this.tpBackup.SuspendLayout();
            this.tpRestore.SuspendLayout();
            this.gbStrategy.SuspendLayout();
            this.gbBoot.SuspendLayout();
            this.gbPartitionLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBootSizeMB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWinSizeGB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRecoverySizeMB)).BeginInit();
            this.tpBootFix.SuspendLayout();
            this.tpSettings.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            this.SuspendLayout();
            // 
            // tcMain
            // 
            this.tcMain.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tcMain.Controls.Add(this.tpBackup);
            this.tcMain.Controls.Add(this.tpRestore);
            this.tcMain.Controls.Add(this.tpBootFix);
            this.tcMain.Controls.Add(this.tpSettings);
            this.tcMain.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcMain.ItemSize = new System.Drawing.Size(200, 35);
            this.tcMain.Location = new System.Drawing.Point(12, 60);
            this.tcMain.Name = "tcMain";
            this.tcMain.SelectedIndex = 0;
            this.tcMain.Size = new System.Drawing.Size(810, 360);
            this.tcMain.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tcMain.TabIndex = 1;
            // 
            // tpBackup
            // 
            this.tpBackup.Controls.Add(this.lblSourcePart);
            this.tpBackup.Controls.Add(this.cmbBackupSource);
            this.tpBackup.Controls.Add(this.lblBackupDest);
            this.tpBackup.Controls.Add(this.txtBackupDest);
            this.tpBackup.Controls.Add(this.btnBrowseBackup);
            this.tpBackup.Controls.Add(this.lblCompression);
            this.tpBackup.Controls.Add(this.cbCompression);
            this.tpBackup.Controls.Add(this.btnStartBackup);
            this.tpBackup.Location = new System.Drawing.Point(4, 39);
            this.tpBackup.Name = "tpBackup";
            this.tpBackup.Padding = new System.Windows.Forms.Padding(3);
            this.tpBackup.Size = new System.Drawing.Size(802, 317);
            this.tpBackup.TabIndex = 0;
            this.tpBackup.Text = "Backup";
            this.tpBackup.UseVisualStyleBackColor = true;
            // 
            // lblSourcePart
            // 
            this.lblSourcePart.Location = new System.Drawing.Point(63, 19);
            this.lblSourcePart.Name = "lblSourcePart";
            this.lblSourcePart.AutoSize = true;
            this.lblSourcePart.TabIndex = 0;
            this.lblSourcePart.Tag = "LBL_SourcePart";
            this.lblSourcePart.Text = "Source:";
            // 
            // cmbBackupSource
            // 
            this.cmbBackupSource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbBackupSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBackupSource.Location = new System.Drawing.Point(63, 44);
            this.cmbBackupSource.Name = "cmbBackupSource";
            this.cmbBackupSource.Size = new System.Drawing.Size(658, 21);
            this.cmbBackupSource.TabIndex = 1;
            // 
            // lblBackupDest
            // 
            this.lblBackupDest.Location = new System.Drawing.Point(63, 99);
            this.lblBackupDest.Name = "lblBackupDest";
            this.lblBackupDest.AutoSize = true;
            this.lblBackupDest.TabIndex = 2;
            this.lblBackupDest.Tag = "LBL_BackupDest";
            this.lblBackupDest.Text = "Target WIM:";
            // 
            // txtBackupDest
            // 
            this.txtBackupDest.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBackupDest.Location = new System.Drawing.Point(63, 123);
            this.txtBackupDest.Name = "txtBackupDest";
            this.txtBackupDest.ReadOnly = true;
            this.txtBackupDest.Size = new System.Drawing.Size(573, 20);
            this.txtBackupDest.TabIndex = 3;
            // 
            // btnBrowseBackup
            // 
            this.btnBrowseBackup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseBackup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseBackup.Location = new System.Drawing.Point(642, 121);
            this.btnBrowseBackup.Name = "btnBrowseBackup";
            this.btnBrowseBackup.Size = new System.Drawing.Size(79, 28);
            this.btnBrowseBackup.TabIndex = 4;
            this.btnBrowseBackup.Tag = "BTN_Browse";
            this.btnBrowseBackup.Text = "Browse";
            // 
            // lblCompression
            // 
            this.lblCompression.Location = new System.Drawing.Point(63, 169);
            this.lblCompression.Name = "lblCompression";
            this.lblCompression.AutoSize = true;
            this.lblCompression.TabIndex = 5;
            this.lblCompression.Tag = "LBL_Compression";
            this.lblCompression.Text = "Compression:";
            // 
            // cbCompression
            // 
            this.cbCompression.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbCompression.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCompression.Location = new System.Drawing.Point(63, 194);
            this.cbCompression.Name = "cbCompression";
            this.cbCompression.Size = new System.Drawing.Size(658, 21);
            this.cbCompression.TabIndex = 6;
            // 
            // btnStartBackup
            // 
            this.btnStartBackup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartBackup.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnStartBackup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartBackup.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnStartBackup.ForeColor = System.Drawing.Color.White;
            this.btnStartBackup.Location = new System.Drawing.Point(184, 240);
            this.btnStartBackup.Name = "btnStartBackup";
            this.btnStartBackup.Size = new System.Drawing.Size(399, 45);
            this.btnStartBackup.TabIndex = 7;
            this.btnStartBackup.Tag = "BTN_StartBackup";
            this.btnStartBackup.Text = "START BACKUP";
            this.btnStartBackup.UseVisualStyleBackColor = false;
            // 
            // tpRestore
            // 
            this.tpRestore.Controls.Add(this.lblWimPath);
            this.tpRestore.Controls.Add(this.txtWimPath);
            this.tpRestore.Controls.Add(this.btnBrowseWim);
            this.tpRestore.Controls.Add(this.lblWimIndex);
            this.tpRestore.Controls.Add(this.cmbWimIndex);
            this.tpRestore.Controls.Add(this.gbStrategy);
            this.tpRestore.Controls.Add(this.lblTarget);
            this.tpRestore.Controls.Add(this.cmbRestoreTarget);
            this.tpRestore.Controls.Add(this.chkCreateBoot);
            this.tpRestore.Controls.Add(this.gbBoot);
            this.tpRestore.Controls.Add(this.gbPartitionLayout);
            this.tpRestore.Controls.Add(this.btnStartRestore);
            this.tpRestore.Location = new System.Drawing.Point(4, 39);
            this.tpRestore.Name = "tpRestore";
            this.tpRestore.Padding = new System.Windows.Forms.Padding(3);
            this.tpRestore.Size = new System.Drawing.Size(802, 317);
            this.tpRestore.TabIndex = 1;
            this.tpRestore.Text = "Restore";
            this.tpRestore.UseVisualStyleBackColor = true;
            // 
            // lblWimPath
            // 
            this.lblWimPath.Location = new System.Drawing.Point(25, 20);
            this.lblWimPath.Name = "lblWimPath";
            this.lblWimPath.AutoSize = true;
            this.lblWimPath.TabIndex = 0;
            this.lblWimPath.Tag = "LBL_WimPath";
            this.lblWimPath.Text = "Image Path:";
            // 
            // txtWimPath
            // 
            this.txtWimPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtWimPath.Location = new System.Drawing.Point(25, 45);
            this.txtWimPath.Name = "txtWimPath";
            this.txtWimPath.ReadOnly = true;
            this.txtWimPath.Size = new System.Drawing.Size(310, 20);
            this.txtWimPath.TabIndex = 1;
            // 
            // btnBrowseWim
            // 
            this.btnBrowseWim.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseWim.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseWim.Location = new System.Drawing.Point(345, 43);
            this.btnBrowseWim.Name = "btnBrowseWim";
            this.btnBrowseWim.Size = new System.Drawing.Size(80, 28);
            this.btnBrowseWim.TabIndex = 2;
            this.btnBrowseWim.Tag = "BTN_Browse";
            this.btnBrowseWim.Text = "Browse";
            // 
            // lblWimIndex
            // 
            this.lblWimIndex.Location = new System.Drawing.Point(25, 85);
            this.lblWimIndex.Name = "lblWimIndex";
            this.lblWimIndex.AutoSize = true;
            this.lblWimIndex.TabIndex = 3;
            this.lblWimIndex.Tag = "LBL_WimIndex";
            this.lblWimIndex.Text = "Index:";
            // 
            // cmbWimIndex
            // 
            this.cmbWimIndex.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbWimIndex.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWimIndex.Location = new System.Drawing.Point(25, 110);
            this.cmbWimIndex.Name = "cmbWimIndex";
            this.cmbWimIndex.Size = new System.Drawing.Size(400, 21);
            this.cmbWimIndex.TabIndex = 4;
            // 
            // gbStrategy
            // 
            this.gbStrategy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbStrategy.Controls.Add(this.rbPartRestore);
            this.gbStrategy.Controls.Add(this.rbWholeDisk);
            this.gbStrategy.Location = new System.Drawing.Point(460, 15);
            this.gbStrategy.Name = "gbStrategy";
            this.gbStrategy.Size = new System.Drawing.Size(320, 85);
            this.gbStrategy.TabIndex = 5;
            this.gbStrategy.TabStop = false;
            this.gbStrategy.Tag = "GB_Strategy";
            this.gbStrategy.Text = "Method";
            // 
            // rbPartRestore
            // 
            this.rbPartRestore.AutoSize = false;
            this.rbPartRestore.Checked = true;
            this.rbPartRestore.Location = new System.Drawing.Point(15, 20);
            this.rbPartRestore.Name = "rbPartRestore";
            this.rbPartRestore.Size = new System.Drawing.Size(295, 30);
            this.rbPartRestore.TabIndex = 0;
            this.rbPartRestore.TabStop = true;
            this.rbPartRestore.Tag = "RB_PartOnly";
            this.rbPartRestore.Text = "Partition";
            // 
            // rbWholeDisk
            // 
            this.rbWholeDisk.AutoSize = false;
            this.rbWholeDisk.Location = new System.Drawing.Point(15, 50);
            this.rbWholeDisk.Name = "rbWholeDisk";
            this.rbWholeDisk.Size = new System.Drawing.Size(295, 30);
            this.rbWholeDisk.TabIndex = 1;
            this.rbWholeDisk.Tag = "RB_DiskRestore";
            this.rbWholeDisk.Text = "Whole Disk";
            // 
            // lblTarget
            // 
            this.lblTarget.Location = new System.Drawing.Point(25, 145);
            this.lblTarget.Name = "lblTarget";
            this.lblTarget.AutoSize = true;
            this.lblTarget.TabIndex = 6;
            this.lblTarget.Tag = "LBL_Target";
            this.lblTarget.Text = "Target:";
            // 
            // cmbRestoreTarget
            // 
            this.cmbRestoreTarget.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbRestoreTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRestoreTarget.Location = new System.Drawing.Point(25, 170);
            this.cmbRestoreTarget.Name = "cmbRestoreTarget";
            this.cmbRestoreTarget.Size = new System.Drawing.Size(400, 21);
            this.cmbRestoreTarget.TabIndex = 7;
            // 
            // chkCreateBoot
            // 
            this.chkCreateBoot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkCreateBoot.AutoSize = false;
            this.chkCreateBoot.Checked = true;
            this.chkCreateBoot.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCreateBoot.Location = new System.Drawing.Point(460, 105);
            this.chkCreateBoot.Name = "chkCreateBoot";
            this.chkCreateBoot.Size = new System.Drawing.Size(320, 25);
            this.chkCreateBoot.TabIndex = 8;
            this.chkCreateBoot.Tag = "CHK_Boot";
            this.chkCreateBoot.Text = "Repair Boot";
            // 
            // gbBoot
            // 
            this.gbBoot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbBoot.Controls.Add(this.rbUEFI);
            this.gbBoot.Controls.Add(this.rbBIOS);
            this.gbBoot.Location = new System.Drawing.Point(460, 135);
            this.gbBoot.Name = "gbBoot";
            this.gbBoot.Size = new System.Drawing.Size(320, 85);
            this.gbBoot.TabIndex = 9;
            this.gbBoot.TabStop = false;
            this.gbBoot.Tag = "GB_Boot";
            this.gbBoot.Text = "Boot Mode";
            // 
            // rbUEFI
            // 
            this.rbUEFI.AutoSize = false;
            this.rbUEFI.Checked = true;
            this.rbUEFI.Location = new System.Drawing.Point(15, 20);
            this.rbUEFI.Name = "rbUEFI";
            this.rbUEFI.Size = new System.Drawing.Size(295, 30);
            this.rbUEFI.TabIndex = 0;
            this.rbUEFI.TabStop = true;
            this.rbUEFI.Text = "UEFI (GPT)";
            // 
            // rbBIOS
            // 
            this.rbBIOS.AutoSize = false;
            this.rbBIOS.Location = new System.Drawing.Point(15, 50);
            this.rbBIOS.Name = "rbBIOS";
            this.rbBIOS.Size = new System.Drawing.Size(295, 30);
            this.rbBIOS.TabIndex = 1;
            this.rbBIOS.Text = "BIOS (MBR)";
            // 
            // gbPartitionLayout
            // 
            this.gbPartitionLayout.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbPartitionLayout.Controls.Add(this.lblBootSize);
            this.gbPartitionLayout.Controls.Add(this.numBootSizeMB);
            this.gbPartitionLayout.Controls.Add(this.lblWinSize);
            this.gbPartitionLayout.Controls.Add(this.numWinSizeGB);
            this.gbPartitionLayout.Controls.Add(this.chkCreateRecovery);
            this.gbPartitionLayout.Controls.Add(this.numRecoverySizeMB);
            this.gbPartitionLayout.Controls.Add(this.lblDataSize);
            this.gbPartitionLayout.Controls.Add(this.pnlVisualMap);
            this.gbPartitionLayout.Location = new System.Drawing.Point(22, 202);
            this.gbPartitionLayout.Name = "gbPartitionLayout";
            this.gbPartitionLayout.Size = new System.Drawing.Size(402, 103);
            this.gbPartitionLayout.TabIndex = 10;
            this.gbPartitionLayout.TabStop = false;
            this.gbPartitionLayout.Tag = "GB_Layout";
            this.gbPartitionLayout.Text = "Partition Sizing";
            this.gbPartitionLayout.Visible = false;
            // 
            // lblBootSize
            // 
            this.lblBootSize.Location = new System.Drawing.Point(5, 19);
            this.lblBootSize.Name = "lblBootSize";
            this.lblBootSize.Size = new System.Drawing.Size(80, 23);
            this.lblBootSize.TabIndex = 0;
            this.lblBootSize.Tag = "LBL_BootSize";
            this.lblBootSize.Text = "Boot (MB):";
            // 
            // numBootSizeMB
            // 
            this.numBootSizeMB.Location = new System.Drawing.Point(7, 42);
            this.numBootSizeMB.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
            this.numBootSizeMB.Minimum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.numBootSizeMB.Name = "numBootSizeMB";
            this.numBootSizeMB.Size = new System.Drawing.Size(70, 20);
            this.numBootSizeMB.TabIndex = 1;
            this.numBootSizeMB.Value = new decimal(new int[] {
            500,
            0,
            0,
            0});
            // 
            // lblWinSize
            // 
            this.lblWinSize.Location = new System.Drawing.Point(93, 18);
            this.lblWinSize.Name = "lblWinSize";
            this.lblWinSize.Size = new System.Drawing.Size(70, 23);
            this.lblWinSize.TabIndex = 2;
            this.lblWinSize.Tag = "LBL_WinSize";
            this.lblWinSize.Text = "Win (GB):";
            // 
            // numWinSizeGB
            // 
            this.numWinSizeGB.Location = new System.Drawing.Point(96, 42);
            this.numWinSizeGB.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numWinSizeGB.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numWinSizeGB.Name = "numWinSizeGB";
            this.numWinSizeGB.Size = new System.Drawing.Size(70, 20);
            this.numWinSizeGB.TabIndex = 3;
            this.numWinSizeGB.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // chkCreateRecovery
            // 
            this.chkCreateRecovery.AutoSize = true;
            this.chkCreateRecovery.Checked = true;
            this.chkCreateRecovery.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCreateRecovery.Location = new System.Drawing.Point(242, 14);
            this.chkCreateRecovery.Name = "chkCreateRecovery";
            this.chkCreateRecovery.Size = new System.Drawing.Size(75, 17);
            this.chkCreateRecovery.TabIndex = 4;
            this.chkCreateRecovery.Tag = "CHK_Rec";
            this.chkCreateRecovery.Text = "Recovery:";
            // 
            // numRecoverySizeMB
            // 
            this.numRecoverySizeMB.Location = new System.Drawing.Point(280, 41);
            this.numRecoverySizeMB.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.numRecoverySizeMB.Minimum = new decimal(new int[] {
            800,
            0,
            0,
            0});
            this.numRecoverySizeMB.Name = "numRecoverySizeMB";
            this.numRecoverySizeMB.Size = new System.Drawing.Size(70, 20);
            this.numRecoverySizeMB.TabIndex = 5;
            this.numRecoverySizeMB.Value = new decimal(new int[] {
            800,
            0,
            0,
            0});
            // 
            // lblDataSize
            // 
            this.lblDataSize.AutoSize = true;
            this.lblDataSize.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblDataSize.ForeColor = System.Drawing.Color.LimeGreen;
            this.lblDataSize.Location = new System.Drawing.Point(172, 46);
            this.lblDataSize.Name = "lblDataSize";
            this.lblDataSize.Size = new System.Drawing.Size(70, 15);
            this.lblDataSize.TabIndex = 6;
            this.lblDataSize.Tag = "LBL_Remaining";
            this.lblDataSize.Text = "DATA: 0 GB";
            // 
            // pnlVisualMap
            // 
            this.pnlVisualMap.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
            this.pnlVisualMap.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlVisualMap.Location = new System.Drawing.Point(6, 68);
            this.pnlVisualMap.Name = "pnlVisualMap";
            this.pnlVisualMap.Size = new System.Drawing.Size(390, 30);
            this.pnlVisualMap.TabIndex = 7;
            // 
            // btnStartRestore
            // 
            this.btnStartRestore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartRestore.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(209)))), ((int)(((byte)(52)))), ((int)(((byte)(56)))));
            this.btnStartRestore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartRestore.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnStartRestore.ForeColor = System.Drawing.Color.White;
            this.btnStartRestore.Location = new System.Drawing.Point(460, 202);
            this.btnStartRestore.Name = "btnStartRestore";
            this.btnStartRestore.Size = new System.Drawing.Size(320, 42);
            this.btnStartRestore.TabIndex = 11;
            this.btnStartRestore.Tag = "BTN_StartRestore";
            this.btnStartRestore.Text = "START RESTORE";
            this.btnStartRestore.UseVisualStyleBackColor = false;
            // 
            // tpBootFix
            // 
            this.tpBootFix.Controls.Add(this.lblBootFixTitle);
            this.tpBootFix.Controls.Add(this.lblBootFixInfo);
            this.tpBootFix.Controls.Add(this.lblInstalledOS);
            this.tpBootFix.Controls.Add(this.btnDriverBackup);
            this.tpBootFix.Controls.Add(this.btnDriverRestore);
            this.tpBootFix.Controls.Add(this.btnAutoBootFix);
            this.tpBootFix.Controls.Add(this.lblBootFixDesc);
            this.tpBootFix.Controls.Add(this.btnHealthCheck);
            this.tpBootFix.Controls.Add(this.lblHealthCheckDesc);
            this.tpBootFix.Location = new System.Drawing.Point(4, 39);
            this.tpBootFix.Name = "tpBootFix";
            this.tpBootFix.Size = new System.Drawing.Size(802, 317);
            this.tpBootFix.TabIndex = 2;
            this.tpBootFix.Text = "Boot Fix";
            this.tpBootFix.UseVisualStyleBackColor = true;
            // 
            // lblBootFixTitle
            // 
            this.lblBootFixTitle.AutoSize = true;
            this.lblBootFixTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblBootFixTitle.Location = new System.Drawing.Point(30, 30);
            this.lblBootFixTitle.Name = "lblBootFixTitle";
            this.lblBootFixTitle.Size = new System.Drawing.Size(153, 30);
            this.lblBootFixTitle.TabIndex = 0;
            this.lblBootFixTitle.Tag = "LBL_BootFixTitle";
            this.lblBootFixTitle.Text = "Auto Boot Fix";
            // 
            // lblBootFixInfo
            // 
            this.lblBootFixInfo.ForeColor = System.Drawing.Color.Gray;
            this.lblBootFixInfo.Location = new System.Drawing.Point(30, 80);
            this.lblBootFixInfo.Name = "lblBootFixInfo";
            this.lblBootFixInfo.Size = new System.Drawing.Size(476, 21);
            this.lblBootFixInfo.TabIndex = 1;
            this.lblBootFixInfo.Tag = "LBL_BootFixInfo";
            this.lblBootFixInfo.Text = "Select your Windows partition and click scan to repair boot files.";
            // 
            // lblInstalledOS
            // 
            this.lblInstalledOS.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblInstalledOS.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.lblInstalledOS.Location = new System.Drawing.Point(30, 130);
            this.lblInstalledOS.Name = "lblInstalledOS";
            this.lblInstalledOS.Size = new System.Drawing.Size(570, 30);
            this.lblInstalledOS.TabIndex = 3;
            // 
            // btnDriverBackup
            // 
            this.btnDriverBackup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDriverBackup.BackColor = System.Drawing.Color.Teal;
            this.btnDriverBackup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDriverBackup.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnDriverBackup.ForeColor = System.Drawing.Color.White;
            this.btnDriverBackup.Location = new System.Drawing.Point(592, 26);
            this.btnDriverBackup.Name = "btnDriverBackup";
            this.btnDriverBackup.Size = new System.Drawing.Size(198, 42);
            this.btnDriverBackup.TabIndex = 6;
            this.btnDriverBackup.Tag = "UI_BTN_DriverBackup";
            this.btnDriverBackup.Text = "Driver Backup";
            this.btnDriverBackup.UseVisualStyleBackColor = false;
            // 
            // btnDriverRestore
            // 
            this.btnDriverRestore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDriverRestore.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.btnDriverRestore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDriverRestore.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnDriverRestore.ForeColor = System.Drawing.Color.White;
            this.btnDriverRestore.Location = new System.Drawing.Point(388, 26);
            this.btnDriverRestore.Name = "btnDriverRestore";
            this.btnDriverRestore.Size = new System.Drawing.Size(198, 42);
            this.btnDriverRestore.TabIndex = 7;
            this.btnDriverRestore.Tag = "UI_BTN_DriverRestore";
            this.btnDriverRestore.Text = "Driver Restore";
            this.btnDriverRestore.UseVisualStyleBackColor = false;
            // 
            // btnAutoBootFix
            // 
            this.btnAutoBootFix.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.btnAutoBootFix.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAutoBootFix.ForeColor = System.Drawing.Color.White;
            this.btnAutoBootFix.Location = new System.Drawing.Point(30, 202);
            this.btnAutoBootFix.Name = "btnAutoBootFix";
            this.btnAutoBootFix.Size = new System.Drawing.Size(300, 50);
            this.btnAutoBootFix.TabIndex = 2;
            this.btnAutoBootFix.Tag = "BTN_BootFix";
            this.btnAutoBootFix.Text = "SCAN & REPAIR";
            this.btnAutoBootFix.UseVisualStyleBackColor = false;
            // 
            // lblBootFixDesc
            // 
            this.lblBootFixDesc.ForeColor = System.Drawing.Color.Gray;
            this.lblBootFixDesc.Location = new System.Drawing.Point(34, 255);
            this.lblBootFixDesc.Name = "lblBootFixDesc";
            this.lblBootFixDesc.Size = new System.Drawing.Size(296, 40);
            this.lblBootFixDesc.TabIndex = 5;
            this.lblBootFixDesc.Tag = "UI_LBL_BootFixDesc";
            this.lblBootFixDesc.Text = "Automatically repair boot files (BCD) if the system fails to start.";
            // 
            // btnHealthCheck
            // 
            this.btnHealthCheck.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.btnHealthCheck.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnHealthCheck.ForeColor = System.Drawing.Color.White;
            this.btnHealthCheck.Location = new System.Drawing.Point(338, 202);
            this.btnHealthCheck.Name = "btnHealthCheck";
            this.btnHealthCheck.Size = new System.Drawing.Size(300, 50);
            this.btnHealthCheck.TabIndex = 3;
            this.btnHealthCheck.Tag = "UI_BTN_HealthCheck";
            this.btnHealthCheck.Text = "CHECK & REPAIR HEALTH";
            this.btnHealthCheck.UseVisualStyleBackColor = false;
            // 
            // lblHealthCheckDesc
            // 
            this.lblHealthCheckDesc.ForeColor = System.Drawing.Color.Gray;
            this.lblHealthCheckDesc.Location = new System.Drawing.Point(338, 255);
            this.lblHealthCheckDesc.Name = "lblHealthCheckDesc";
            this.lblHealthCheckDesc.Size = new System.Drawing.Size(300, 40);
            this.lblHealthCheckDesc.TabIndex = 4;
            this.lblHealthCheckDesc.Tag = "UI_LBL_HealthCheckInfo";
            this.lblHealthCheckDesc.Text = "Scan and repair corrupted system files and Windows image (DISM & SFC).";
            // 
            // tpSettings
            // 
            this.tpSettings.Controls.Add(this.lblLang);
            this.tpSettings.Controls.Add(this.cbLang);
            this.tpSettings.Location = new System.Drawing.Point(4, 39);
            this.tpSettings.Name = "tpSettings";
            this.tpSettings.Size = new System.Drawing.Size(802, 317);
            this.tpSettings.TabIndex = 3;
            this.tpSettings.Text = "Settings";
            this.tpSettings.UseVisualStyleBackColor = true;
            // 
            // lblLang
            // 
            this.lblLang.Location = new System.Drawing.Point(30, 26);
            this.lblLang.Name = "lblLang";
            this.lblLang.Size = new System.Drawing.Size(100, 23);
            this.lblLang.TabIndex = 0;
            this.lblLang.Tag = "LBL_Lang";
            this.lblLang.Text = "Language:";
            // 
            // cbLang
            // 
            this.cbLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLang.Location = new System.Drawing.Point(133, 22);
            this.cbLang.Name = "cbLang";
            this.cbLang.Size = new System.Drawing.Size(134, 21);
            this.cbLang.TabIndex = 1;
            // 
            // lblHeader
            // 
            this.lblHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHeader.AutoEllipsis = true;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI Semibold", 18F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Location = new System.Drawing.Point(12, 12);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Size = new System.Drawing.Size(500, 32);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.Tag = "UI_Header";
            this.lblHeader.Text = "Windows Backup / Restore Tool";
            // 
            // btnAbout
            // 
            this.btnAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAbout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAbout.Location = new System.Drawing.Point(747, 15);
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(75, 30);
            this.btnAbout.TabIndex = 6;
            this.btnAbout.Tag = "BTN_About";
            this.btnAbout.Text = "About";
            // 
            // lblBootMode
            // 
            this.lblBootMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBootMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.lblBootMode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblBootMode.Location = new System.Drawing.Point(522, 15);
            this.lblBootMode.Name = "lblBootMode";
            this.lblBootMode.Size = new System.Drawing.Size(220, 30);
            this.lblBootMode.TabIndex = 5;
            this.lblBootMode.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pbMain
            // 
            this.pbMain.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbMain.Location = new System.Drawing.Point(12, 455);
            this.pbMain.Name = "pbMain";
            this.pbMain.Size = new System.Drawing.Size(500, 23);
            this.pbMain.TabIndex = 2;
            // 
            // lblProgressStatus
            // 
            this.lblProgressStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblProgressStatus.Location = new System.Drawing.Point(121, 430);
            this.lblProgressStatus.Name = "lblProgressStatus";
            this.lblProgressStatus.Size = new System.Drawing.Size(701, 23);
            this.lblProgressStatus.TabIndex = 3;
            this.lblProgressStatus.Text = "Ready";
            this.lblProgressStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnClearLog
            // 
            this.btnClearLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearLog.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btnClearLog.Location = new System.Drawing.Point(12, 430);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(105, 23);
            this.btnClearLog.TabIndex = 10;
            this.btnClearLog.Text = "Clear";
            this.btnClearLog.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // rtbLog
            // 
            this.rtbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.rtbLog.ForeColor = System.Drawing.SystemColors.Window;
            this.rtbLog.Location = new System.Drawing.Point(12, 485);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.Size = new System.Drawing.Size(810, 166);
            this.rtbLog.TabIndex = 4;
            this.rtbLog.Text = "";
            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.pnlFooter.Controls.Add(this.lnkGithub);
            this.pnlFooter.Controls.Add(this.lnkWeb);
            this.pnlFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFooter.Location = new System.Drawing.Point(0, 657);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Size = new System.Drawing.Size(834, 23);
            this.pnlFooter.TabIndex = 7;
            // 
            // lnkGithub
            // 
            this.lnkGithub.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lnkGithub.AutoSize = true;
            this.lnkGithub.Location = new System.Drawing.Point(777, 3);
            this.lnkGithub.Name = "lnkGithub";
            this.lnkGithub.Size = new System.Drawing.Size(40, 13);
            this.lnkGithub.TabIndex = 2;
            this.lnkGithub.TabStop = true;
            this.lnkGithub.Text = "GitHub";
            // 
            // lnkWeb
            // 
            this.lnkWeb.AutoSize = true;
            this.lnkWeb.Location = new System.Drawing.Point(9, 3);
            this.lnkWeb.Name = "lnkWeb";
            this.lnkWeb.Size = new System.Drawing.Size(51, 13);
            this.lnkWeb.TabIndex = 1;
            this.lnkWeb.TabStop = true;
            this.lnkWeb.Text = "Web Site";
            // 
            // chkPostAction
            // 
            this.chkPostAction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkPostAction.Location = new System.Drawing.Point(531, 457);
            this.chkPostAction.Name = "chkPostAction";
            this.chkPostAction.Size = new System.Drawing.Size(104, 23);
            this.chkPostAction.TabIndex = 8;
            this.chkPostAction.Text = "On Finish:";
            this.chkPostAction.UseVisualStyleBackColor = true;
            // 
            // cmbPostAction
            // 
            this.cmbPostAction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPostAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPostAction.Location = new System.Drawing.Point(655, 455);
            this.cmbPostAction.Name = "cmbPostAction";
            this.cmbPostAction.Size = new System.Drawing.Size(163, 21);
            this.cmbPostAction.TabIndex = 9;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.LightGray;
            this.ClientSize = new System.Drawing.Size(834, 680);
            this.Controls.Add(this.tcMain);
            this.Controls.Add(this.lblHeader);
            this.Controls.Add(this.btnAbout);
            this.Controls.Add(this.btnClearLog);
            this.Controls.Add(this.lblBootMode);
            this.Controls.Add(this.pbMain);
            this.Controls.Add(this.chkPostAction);
            this.Controls.Add(this.cmbPostAction);
            this.Controls.Add(this.lblProgressStatus);
            this.Controls.Add(this.rtbLog);
            this.Controls.Add(this.pnlFooter);
            this.MinimumSize = new System.Drawing.Size(850, 684);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Windows Backup / Restore Tool v4 | by Abdullah ERTÜRK";
            this.tcMain.ResumeLayout(false);
            this.tpBackup.ResumeLayout(false);
            this.tpBackup.PerformLayout();
            this.tpRestore.ResumeLayout(false);
            this.tpRestore.PerformLayout();
            this.gbStrategy.ResumeLayout(false);
            this.gbStrategy.PerformLayout();
            this.gbBoot.ResumeLayout(false);
            this.gbBoot.PerformLayout();
            this.gbPartitionLayout.ResumeLayout(false);
            this.gbPartitionLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBootSizeMB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWinSizeGB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRecoverySizeMB)).EndInit();
            this.tpBootFix.ResumeLayout(false);
            this.tpBootFix.PerformLayout();
            this.tpSettings.ResumeLayout(false);
            this.pnlFooter.ResumeLayout(false);
            this.pnlFooter.PerformLayout();
            this.ResumeLayout(false);

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



