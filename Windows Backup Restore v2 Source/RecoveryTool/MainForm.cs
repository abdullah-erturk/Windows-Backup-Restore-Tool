using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Principal;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Management;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Reflection; 

namespace BackupRestoreTool
{
    public class MainForm : Form
    {
        // UI Controls
        private Button btnBackup;
        private Button btnRestore;
        private Button btnExit;
        private Label lblHeader;
        private RichTextBox txtLog;
        private ProgressBar progressBar;
        private Label lblProgress;
        
        // New UI
        private TextBox txtSavePath;
        private Button btnBrowse;
        private Label lblSavePath;

        private ComboBox cmbSource;
        
        // Log History
        private readonly List<LogEntry> logHistory = new List<LogEntry>();
        private Label lblSource;
        private Button btnRefresh;
        private Button btnRefreshRestore;
        private ComboBox cbLang;
        private Label lblLang; // New Label
        private ComboBox cbCompress;
        private Label lblCompress;
        private LinkLabel lnkWeb;
        private LinkLabel lnkGit;
        private LinkLabel lnkAbout;
        private ToolTip toolTip;

        // Restore UI
        private GroupBox gbRestore;
        private Label lblWimPath;
        private TextBox txtWimPath;
        private Button btnBrowseWim;
        private RadioButton rbWholeDisk;
        private RadioButton rbPartOnly;
        private Label lblRestoreTarget;
        private ComboBox cmbTarget;
        private GroupBox gbBootMode;
        private GroupBox gbPartitionLayout; // New
        private Label lblDiskSizeInfo; // New
        private Label lblBootSize; // New
        private NumericUpDown numBootSize; // New
        private Label lblWinSize; // New
        private NumericUpDown numWinSize; // New
        private Label lblDataSizeInfo; // New
        private Label lblDataSizeValue; // New
        
        private RadioButton rbGPT;
        private RadioButton rbMBR;
        private Label lblBootHint;
        private Label lblBootInfo;
        private CheckBox cbCreateBoot;
        private Label lblFirmwareInfo; // New Firmware Info Label
        private GroupBox gbBackup;
        private TableLayoutPanel tlpContent;
        private ComboBox cmbWimIndex; 
        
        // Post-Action UI
        private CheckBox chkPostAction;
        private ComboBox cmbPostAction;


        // State
        private readonly Dictionary<string, string> currentLang = new Dictionary<string, string>();
        private readonly Dictionary<string, string> defaultLang = new Dictionary<string, string>(); // English Fallback
        private readonly List<LanguageItem> availableLanguages = new List<LanguageItem>();
        private bool isWinPE = false;
        private bool isOperationRunning = false;
        private volatile bool isBackupRunning = false; // v91
        private string currentBackupTarget = ""; // v91
        private readonly object processLock = new object();
        private System.ComponentModel.IContainer components;
        private Process currentProcess = null; // v91

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

        const uint GENERIC_READ = 0x80000000;
        const uint FILE_SHARE_READ = 0x00000001;
        const uint FILE_SHARE_WRITE = 0x00000002;
        const uint OPEN_EXISTING = 3;
        const uint IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS = 0x00560000;

        [StructLayout(LayoutKind.Sequential)]
        struct DISK_EXTENT
        {
            public int DiskNumber;
            public long StartingOffset;
            public long ExtentLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct VOLUME_DISK_EXTENTS
        {
            public int NumberOfDiskExtents;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public DISK_EXTENT[] Extents;
        }

        public MainForm()
        {
            DetectEnvironment();
            InitLanguages();
            
            InitializeComponent();
            foreach(var lang in availableLanguages) cbLang.Items.Add(lang);
            RefreshSourcePartitions();
            
            // Auto-select language
            // Auto-select language with Registry Priority
            string sysLang = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName.ToLower();
            string storedLang = null;
            
            // 1. Check Registry Preference
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\BackupRestoreTool")) {
                    if (key != null) storedLang = key.GetValue("Language") as string;
                }
            } catch { }

            LanguageItem target = null;

            // Priority 1: Registry
            if (!string.IsNullOrEmpty(storedLang))
            {
                target = availableLanguages.FirstOrDefault(x => x.Code == storedLang);
            }

            // Priority 2: System Language
            if (target == null)
            {
                target = availableLanguages.FirstOrDefault(x => x.Code == sysLang);
            }

            if (target != null) 
            {
                cbLang.SelectedItem = target;
            }
            else 
            {
                // Priority 3: Default to Turkish (tr) if nothing matches
                var fallback = availableLanguages.FirstOrDefault(x => x.Code == "tr");
                if (fallback != null) cbLang.SelectedItem = fallback;
                else cbLang.SelectedIndex = 0;
            }

            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BackupRestoreTool.icon.ico"))
                {
                    if (stream != null) this.Icon = new Icon(stream);
                }
            }
            catch { }


            // Initial Log with dynamic key
            ResetLogState();


            // Set dynamic visibility that Designer cannot handle
            lblBootHint.Visible = isWinPE;

            // WinPE Specifics for Boot Mode
            if (isWinPE)
            {
                bool isUEFI = false;
                DetectBootModeWinPE(out isUEFI);
                rbGPT.Checked = isUEFI;
                rbMBR.Checked = !isUEFI;
            }

            // Ensure wim_exclusions.ini exists (v89)
            string exclusionFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin\\wim_exclusions.ini");
            if (!File.Exists(exclusionFile))
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("[ExclusionList]");
                    sb.AppendLine("\\$ntfs.log");
                    sb.AppendLine("\\hiberfil.sys");
                    sb.AppendLine("\\Windows\\System32\\DriverStore\\Temp");
                    sb.AppendLine("\\pagefile.sys");
                    sb.AppendLine("\\swapfile.sys");
                    sb.AppendLine("\\System Volume Information");
                    sb.AppendLine("\\RECYCLER");
                    sb.AppendLine("\\$Recycle.Bin");
                    sb.AppendLine("\\Windows\\CSC");
                    sb.AppendLine("\\Windows\\Temp");
                    sb.AppendLine("\\Temp");
                    sb.AppendLine("\\PerfLogs");
                    sb.AppendLine("");
                    sb.AppendLine("[CompressionExclusionList]");
                    sb.AppendLine("*.mp3");
                    sb.AppendLine("*.mp4");
                    sb.AppendLine("*.avi");
                    sb.AppendLine("*.mkv");
                    sb.AppendLine("*.zip");
                    sb.AppendLine("*.rar");
                    sb.AppendLine("*.7z");
                    sb.AppendLine("*.cab");
                    sb.AppendLine("*.jpg");
                    sb.AppendLine("*.jpeg");
                    sb.AppendLine("*.png");
                    sb.AppendLine("\\WINDOWS\\inf\\*.pnf");
                    sb.AppendLine("\\WINDOWS\\System32\\DriverStore\\*.cab");
                    sb.AppendLine("");
                    sb.AppendLine("; =========================================================");
                    sb.AppendLine("; NOTE: Cloud folders (OneDrive, Google Drive, Dropbox, etc.)");
                    sb.AppendLine("; are AUTOMATICALLY detected and excluded by the application.");
                    sb.AppendLine("; You do not need to list them here.");
                    sb.AppendLine("; =========================================================");
                    File.WriteAllText(exclusionFile, sb.ToString());
                }
                catch { }
            }
            
            UpdateUILanguage();
        }

        private void DetectEnvironment()
        {
            try {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\MiniNT")) {
                    isWinPE = (key != null);
                }
            } catch { isWinPE = false; }
        }

        private void InitLanguages()
        {
            // 1. Setup Default (English) Fallback Hardcoded
            defaultLang["Title"] = "Windows Backup & Restore Tool";
            defaultLang["ErrorTitle"] = "Error";
            defaultLang["ConfirmTitle"] = "Confirm";
            defaultLang["WarningTitle"] = "Warning";
            defaultLang["PostProcess"] = "After completion:";
            defaultLang["Shutdown"] = "Shutdown PC";
            defaultLang["Restart"] = "Restart PC";
            defaultLang["About"] = "About";
            defaultLang["SystemInit"] = "System Init...";
            defaultLang["TimeRemaining"] = "Time remaining: {0}";
            defaultLang["BtnCancel"] = "Cancel";
            defaultLang["FirmwareMode"] = "Firmware Mode: {0}";
            defaultLang["LanguageLabel"] = "Language:"; // Default Key
            defaultLang["BackupAborted"] = "Operation Aborted by User.";

            // 2. Load eng.ini into defaultLang to get latest updates
            LoadSimpleDictionary("eng", defaultLang);

            // 3. Scan for Languages
            availableLanguages.Clear();
            string langDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin\\Lang");
            if (Directory.Exists(langDir))
            {
                string[] files = Directory.GetFiles(langDir, "*.ini");
                foreach (string f in files)
                {
                    string code = Path.GetFileNameWithoutExtension(f).ToLower();
                    // Read LanguageName from file
                    string name = ReadIniValue(f, "LanguageName");
                    if (string.IsNullOrEmpty(name)) name = code.ToUpper();
                    
                    availableLanguages.Add(new LanguageItem { Name = name, Code = code });
                }
            }
            
            // Ensure at least English is there if nothing found
            if (availableLanguages.Count == 0 || !availableLanguages.Any(x => x.Code == "eng"))
            {
                 availableLanguages.Insert(0, new LanguageItem { Name = "English", Code = "eng" });
            }
        }

        private void LoadLanguage(string code)
        {
            currentLang.Clear();
            LoadSimpleDictionary(code, currentLang);
        }

        private void LoadSimpleDictionary(string code, Dictionary<string, string> dict)
        {
             string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin\\Lang\\" + code + ".ini");
             if (!File.Exists(path)) return;
             try
             {
                 string[] lines = File.ReadAllLines(path);
                 foreach (string line in lines)
                 {
                     if (string.IsNullOrEmpty(line) || !line.Contains("=") || line.StartsWith("[")) continue;
                     int idx = line.IndexOf('=');
                     string key = line.Substring(0, idx).Trim();
                     string val = line.Substring(idx + 1).Trim();
                     val = val.Replace("\\n", "\n");
                     dict[key] = val;
                 }
             }
             catch { }
        }

        private string ReadIniValue(string path, string key)
        {
            try {
                foreach (string line in File.ReadAllLines(path)) {
                    if (line.StartsWith(key + "=")) return line.Substring(key.Length + 1).Trim();
                }
            } catch { }
            return null;
        }

        private string GetStr(string key) 
        { 
            if (string.IsNullOrEmpty(key)) return key ?? "";
            if (currentLang.ContainsKey(key)) return currentLang[key];
            if (defaultLang.ContainsKey(key)) return defaultLang[key];
            return key; 
        }

        private void LogKey(string key, params object[] args)
        {
            LogEntry entry = new LogEntry 
            { 
                Key = key, 
                Args = args, 
                Timestamp = DateTime.Now 
            };
            logHistory.Add(entry);
            
            string fmt = GetStr(key);
            string msg = (args != null && args.Length > 0) ? string.Format(fmt, args) : fmt;
            
            if (txtLog.InvokeRequired)
                txtLog.Invoke(new Action(() => txtLog.AppendText(msg + "\n")));
            else
                txtLog.AppendText(msg + "\n");
        }

        private void RefillLogBox()
        {
            if (txtLog == null) return;
            txtLog.Clear();
            foreach(var entry in logHistory)
            {
                if (!string.IsNullOrEmpty(entry.RawText))
                {
                    txtLog.AppendText(entry.RawText + "\n");
                }
                else
                {
                    string fmt = GetStr(entry.Key ?? "");
                    string msg = (entry.Args != null && entry.Args.Length > 0) ? string.Format(fmt, entry.Args) : fmt;
                    txtLog.AppendText(msg + "\n");
                }
            }
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void UpdateUILanguage()
        {
            this.Text = "Windows Backup / Restore Tool v2 | made by Abdullah ERTÜRK";
            lblHeader.Text = GetStr("Header");
            if (lblLang != null) lblLang.Text = GetStr("LanguageLabel");
            btnExit.Text = GetStr("Exit");
            lnkWeb.Text = GetStr("WebLink");
            lnkGit.Text = GetStr("GitLink");
            if (lnkAbout != null) lnkAbout.Text = GetStr("About");

            // Backup Section
            if (gbBackup != null) gbBackup.Text = GetStr("Backup");
            if (lblSource != null) lblSource.Text = GetStr("SourceLabel");
            if (lblSavePath != null) lblSavePath.Text = GetStr("SavePath");
            if (btnBrowse != null) btnBrowse.Text = GetStr("Browse");
            if (btnBackup != null) btnBackup.Text = GetStr("Backup");
            if (lblCompress != null) lblCompress.Text = GetStr("CompressLabel");
            
            if (cbCompress != null)
            {
                int selectedIdx = cbCompress.SelectedIndex;
                cbCompress.Items.Clear();
                cbCompress.Items.Add(GetStr("CompressNone"));
                cbCompress.Items.Add(GetStr("CompressFast"));
                cbCompress.Items.Add(GetStr("CompressMax"));
                cbCompress.SelectedIndex = selectedIdx >= 0 ? selectedIdx : 1;
            }

            if (cmbWimIndex != null)
            {
                if (cmbWimIndex.Items.Count < 1 || (cmbWimIndex.Items.Count == 1 && cmbWimIndex.Items[0] is string))
                {
                     cmbWimIndex.Items.Clear();
                     // Use specific fallback key or generic placeholder based on context
                     // Since we don't have easy context here, we assume:
                     // If existing item was "Default Image" (localized or not), we map to DefaultImage. Else SelectWimIndex.
                     
                     // Safer Approach for this function: Always default to SelectWimIndex unless we know otherwise.
                     // The user's main complaint was "Select Version" not updating.
                     
                     if (string.IsNullOrEmpty(txtWimPath.Text))
                         cmbWimIndex.Items.Add(GetStr("SelectWimIndex"));
                     else
                         cmbWimIndex.Items.Add(GetStr("DefaultImage"));
                         
                     cmbWimIndex.SelectedIndex = 0;
                }
            }

            // Post-Action UI
            if (chkPostAction != null) chkPostAction.Text = GetStr("OnCompletion");
            if (cmbPostAction != null)
            {
                int sIdx = cmbPostAction.SelectedIndex;
                cmbPostAction.Items.Clear();
                cmbPostAction.Items.Add(GetStr("Shutdown"));
                cmbPostAction.Items.Add(GetStr("Restart"));
                cmbPostAction.SelectedIndex = sIdx >= 0 ? sIdx : 1;
            }

            // Restore Section
            if (gbRestore != null) gbRestore.Text = GetStr("Restore");
            if (lblWimPath != null) lblWimPath.Text = GetStr("WimPath");
            if (btnBrowseWim != null) btnBrowseWim.Text = GetStr("Browse");
            if (rbWholeDisk != null) rbWholeDisk.Text = GetStr("RestoreTypeDisk");
            if (rbPartOnly != null) rbPartOnly.Text = GetStr("RestoreTypePart");
            if (lblRestoreTarget != null) lblRestoreTarget.Text = rbWholeDisk.Checked ? GetStr("TargetDisk") : GetStr("TargetPart");
            if (gbBootMode != null) gbBootMode.Text = GetStr("BootModeTitle");
            if (rbGPT != null) rbGPT.Text = GetStr("BootModeGPT");
            if (rbMBR != null) rbMBR.Text = GetStr("BootModeMBR");
            if (lblBootHint != null) lblBootHint.Text = GetStr("BootModeAuto");
            if (cbCreateBoot != null) cbCreateBoot.Text = GetStr("CreateBootRecord");
            if (btnRestore != null) btnRestore.Text = GetStr("Restore");

            // Partition Layout Localization
            if (gbPartitionLayout != null) gbPartitionLayout.Text = GetStr("PartitionLayout");
            if (lblBootSize != null) lblBootSize.Text = GetStr("BootSizeLabel");
            if (lblWinSize != null) lblWinSize.Text = GetStr("WinSizeLabel");
            if (lblDataSizeInfo != null) lblDataSizeInfo.Text = GetStr("DataSizeLabel");
            if (lblDiskSizeInfo != null && currentDiskSizeGB > 0) 
                 lblDiskSizeInfo.Text = GetStr("TotalDiskSize") + " " + currentDiskSizeGB + " GB";
            
            CalculatePartitionSizes();
            
            if (gbBootMode != null) gbBootMode.Text = GetStr("BootModeTitle");

            if (lblFirmwareInfo != null)
            {
                lblFirmwareInfo.Visible = true;
                bool isU = false;
                DetectBootModeWinPE(out isU);
                lblFirmwareInfo.Text = string.Format(GetStr("FirmwareLabel"), isU ? "UEFI" : "BIOS");
                lblFirmwareInfo.BringToFront();
            }

            if (btnRefresh != null) toolTip.SetToolTip(btnRefresh, GetStr("RefreshTip"));
            if (btnRefreshRestore != null) toolTip.SetToolTip(btnRefreshRestore, GetStr("RefreshTip"));

            
            // Update log ready message
            if (txtLog != null && txtLog.Text.Contains(GetStr("LogReady")))
            {
                // Only update if it's just the ready message
                // txtLog.Text = GetStr("LogReady") + "\n";
            }

            if (toolTip != null && btnRefresh != null) toolTip.SetToolTip(btnRefresh, GetStr("RefreshTip"));
            
            AlignFooterLinks();
            RefillLogBox();
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblLang = new System.Windows.Forms.Label();
            this.cbLang = new System.Windows.Forms.ComboBox();
            this.tlpContent = new System.Windows.Forms.TableLayoutPanel();
            this.gbRestore = new System.Windows.Forms.GroupBox();
            this.btnRestore = new System.Windows.Forms.Button();
            this.lblWimPath = new System.Windows.Forms.Label();
            this.txtWimPath = new System.Windows.Forms.TextBox();
            this.btnBrowseWim = new System.Windows.Forms.Button();
            this.cmbWimIndex = new System.Windows.Forms.ComboBox();
            this.rbWholeDisk = new System.Windows.Forms.RadioButton();
            this.rbPartOnly = new System.Windows.Forms.RadioButton();
            this.lblRestoreTarget = new System.Windows.Forms.Label();
            this.cmbTarget = new System.Windows.Forms.ComboBox();
            this.btnRefreshRestore = new System.Windows.Forms.Button();
            this.gbBootMode = new System.Windows.Forms.GroupBox();
            this.rbGPT = new System.Windows.Forms.RadioButton();
            this.rbMBR = new System.Windows.Forms.RadioButton();
            this.lblBootHint = new System.Windows.Forms.Label();
            this.lblBootInfo = new System.Windows.Forms.Label();
            this.lblFirmwareInfo = new System.Windows.Forms.Label();
            this.cbCreateBoot = new System.Windows.Forms.CheckBox();
            this.gbPartitionLayout = new System.Windows.Forms.GroupBox();
            this.lblDiskSizeInfo = new System.Windows.Forms.Label();
            this.lblBootSize = new System.Windows.Forms.Label();
            this.numBootSize = new System.Windows.Forms.NumericUpDown();
            this.lblWinSize = new System.Windows.Forms.Label();
            this.numWinSize = new System.Windows.Forms.NumericUpDown();
            this.lblDataSizeInfo = new System.Windows.Forms.Label();
            this.lblDataSizeValue = new System.Windows.Forms.Label();
            this.gbBackup = new System.Windows.Forms.GroupBox();
            this.lblSource = new System.Windows.Forms.Label();
            this.cmbSource = new System.Windows.Forms.ComboBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.lblSavePath = new System.Windows.Forms.Label();
            this.txtSavePath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.lblCompress = new System.Windows.Forms.Label();
            this.cbCompress = new System.Windows.Forms.ComboBox();
            this.btnBackup = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.chkPostAction = new System.Windows.Forms.CheckBox();
            this.cmbPostAction = new System.Windows.Forms.ComboBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblProgress = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.lnkWeb = new System.Windows.Forms.LinkLabel();
            this.lnkAbout = new System.Windows.Forms.LinkLabel();
            this.lnkGit = new System.Windows.Forms.LinkLabel();
            this.tlpContent.SuspendLayout();
            this.gbRestore.SuspendLayout();
            this.gbBootMode.SuspendLayout();
            this.gbPartitionLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numBootSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWinSize)).BeginInit();
            this.gbBackup.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblHeader
            // 
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Location = new System.Drawing.Point(20, 10);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Size = new System.Drawing.Size(500, 30);
            this.lblHeader.TabIndex = 0;
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLang
            // 
            this.lblLang.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLang.AutoSize = true;
            this.lblLang.Location = new System.Drawing.Point(640, 18);
            this.lblLang.Name = "lblLang";
            this.lblLang.Size = new System.Drawing.Size(0, 13);
            this.lblLang.TabIndex = 1;
            // 
            // cbLang
            // 
            this.cbLang.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLang.Location = new System.Drawing.Point(710, 15);
            this.cbLang.Name = "cbLang";
            this.cbLang.Size = new System.Drawing.Size(110, 21);
            this.cbLang.TabIndex = 2;
            this.cbLang.SelectedIndexChanged += new System.EventHandler(this.CbLang_SelectedIndexChanged);
            // 
            // tlpContent
            // 
            this.tlpContent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpContent.ColumnCount = 2;
            this.tlpContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpContent.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpContent.Controls.Add(this.gbRestore, 0, 0);
            this.tlpContent.Controls.Add(this.gbBackup, 1, 0);
            this.tlpContent.Location = new System.Drawing.Point(20, 50);
            this.tlpContent.Name = "tlpContent";
            this.tlpContent.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlpContent.Size = new System.Drawing.Size(800, 470);
            this.tlpContent.TabIndex = 3;
            // 
            // gbRestore
            // 
            this.gbRestore.Controls.Add(this.btnRestore);
            this.gbRestore.Controls.Add(this.lblWimPath);
            this.gbRestore.Controls.Add(this.txtWimPath);
            this.gbRestore.Controls.Add(this.btnBrowseWim);
            this.gbRestore.Controls.Add(this.cmbWimIndex);
            this.gbRestore.Controls.Add(this.rbWholeDisk);
            this.gbRestore.Controls.Add(this.rbPartOnly);
            this.gbRestore.Controls.Add(this.lblRestoreTarget);
            this.gbRestore.Controls.Add(this.cmbTarget);
            this.gbRestore.Controls.Add(this.btnRefreshRestore);
            this.gbRestore.Controls.Add(this.gbBootMode);
            this.gbRestore.Controls.Add(this.lblBootInfo);
            this.gbRestore.Controls.Add(this.lblFirmwareInfo);
            this.gbRestore.Controls.Add(this.cbCreateBoot);
            this.gbRestore.Controls.Add(this.gbPartitionLayout);
            this.gbRestore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbRestore.Location = new System.Drawing.Point(3, 3);
            this.gbRestore.Name = "gbRestore";
            this.gbRestore.Size = new System.Drawing.Size(394, 464);
            this.gbRestore.TabIndex = 0;
            this.gbRestore.TabStop = false;
            // 
            // btnRestore
            // 
            this.btnRestore.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            this.btnRestore.Location = new System.Drawing.Point(113, 426);
            this.btnRestore.Name = "btnRestore";
            this.btnRestore.Size = new System.Drawing.Size(136, 32);
            this.btnRestore.TabIndex = 13;
            this.btnRestore.Text = "Restore";
            this.btnRestore.Click += new System.EventHandler(this.BtnRestore_Click);
            // 
            // lblWimPath
            // 
            this.lblWimPath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblWimPath.Location = new System.Drawing.Point(15, 25);
            this.lblWimPath.Name = "lblWimPath";
            this.lblWimPath.Size = new System.Drawing.Size(277, 20);
            this.lblWimPath.TabIndex = 0;
            // 
            // txtWimPath
            // 
            this.txtWimPath.BackColor = System.Drawing.Color.White;
            this.txtWimPath.Location = new System.Drawing.Point(15, 45);
            this.txtWimPath.Name = "txtWimPath";
            this.txtWimPath.ReadOnly = true;
            this.txtWimPath.Size = new System.Drawing.Size(277, 20);
            this.txtWimPath.TabIndex = 1;
            // 
            // btnBrowseWim
            // 
            this.btnBrowseWim.Location = new System.Drawing.Point(296, 43);
            this.btnBrowseWim.Name = "btnBrowseWim";
            this.btnBrowseWim.Size = new System.Drawing.Size(85, 24);
            this.btnBrowseWim.TabIndex = 2;
            this.btnBrowseWim.Click += new System.EventHandler(this.BtnBrowseWim_Click);
            // 
            // cmbWimIndex
            // 
            this.cmbWimIndex.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbWimIndex.Location = new System.Drawing.Point(15, 75);
            this.cmbWimIndex.Name = "cmbWimIndex";
            this.cmbWimIndex.Size = new System.Drawing.Size(366, 21);
            this.cmbWimIndex.TabIndex = 3;
            // 
            // rbWholeDisk
            // 
            this.rbWholeDisk.Checked = true;
            this.rbWholeDisk.Location = new System.Drawing.Point(15, 105);
            this.rbWholeDisk.Name = "rbWholeDisk";
            this.rbWholeDisk.Size = new System.Drawing.Size(315, 20);
            this.rbWholeDisk.TabIndex = 4;
            this.rbWholeDisk.TabStop = true;
            this.rbWholeDisk.CheckedChanged += new System.EventHandler(this.RbRestoreMode_CheckedChanged);
            // 
            // rbPartOnly
            // 
            this.rbPartOnly.Location = new System.Drawing.Point(15, 125);
            this.rbPartOnly.Name = "rbPartOnly";
            this.rbPartOnly.Size = new System.Drawing.Size(315, 20);
            this.rbPartOnly.TabIndex = 5;
            this.rbPartOnly.CheckedChanged += new System.EventHandler(this.RbRestoreMode_CheckedChanged);
            // 
            // lblRestoreTarget
            // 
            this.lblRestoreTarget.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRestoreTarget.Location = new System.Drawing.Point(15, 145);
            this.lblRestoreTarget.Name = "lblRestoreTarget";
            this.lblRestoreTarget.Size = new System.Drawing.Size(277, 20);
            this.lblRestoreTarget.TabIndex = 6;
            // 
            // cmbTarget
            // 
            this.cmbTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTarget.Location = new System.Drawing.Point(15, 165);
            this.cmbTarget.Name = "cmbTarget";
            this.cmbTarget.Size = new System.Drawing.Size(328, 21);
            this.cmbTarget.TabIndex = 7;
            this.cmbTarget.SelectedIndexChanged += new System.EventHandler(this.CmbTarget_SelectedIndexChanged);
            // 
            // btnRefreshRestore
            // 
            this.btnRefreshRestore.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefreshRestore.Font = new System.Drawing.Font("Arial", 14F, System.Drawing.FontStyle.Bold);
            this.btnRefreshRestore.Location = new System.Drawing.Point(349, 162);
            this.btnRefreshRestore.Name = "btnRefreshRestore";
            this.btnRefreshRestore.Size = new System.Drawing.Size(32, 28);
            this.btnRefreshRestore.TabIndex = 8;
            this.btnRefreshRestore.Text = "⟳";
            this.btnRefreshRestore.Click += new System.EventHandler(this.BtnRefreshRestore_Click);
            // 
            // gbBootMode
            // 
            this.gbBootMode.Controls.Add(this.rbGPT);
            this.gbBootMode.Controls.Add(this.rbMBR);
            this.gbBootMode.Controls.Add(this.lblBootHint);
            this.gbBootMode.Location = new System.Drawing.Point(15, 195);
            this.gbBootMode.Name = "gbBootMode";
            this.gbBootMode.Size = new System.Drawing.Size(366, 60);
            this.gbBootMode.TabIndex = 9;
            this.gbBootMode.TabStop = false;
            this.gbBootMode.Text = "Boot Mode";
            // 
            // rbGPT
            // 
            this.rbGPT.Checked = true;
            this.rbGPT.Location = new System.Drawing.Point(15, 20);
            this.rbGPT.Name = "rbGPT";
            this.rbGPT.Size = new System.Drawing.Size(150, 20);
            this.rbGPT.TabIndex = 0;
            this.rbGPT.TabStop = true;
            // 
            // rbMBR
            // 
            this.rbMBR.Location = new System.Drawing.Point(15, 38);
            this.rbMBR.Name = "rbMBR";
            this.rbMBR.Size = new System.Drawing.Size(150, 20);
            this.rbMBR.TabIndex = 1;
            // 
            // lblBootHint
            // 
            this.lblBootHint.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblBootHint.ForeColor = System.Drawing.Color.Blue;
            this.lblBootHint.Location = new System.Drawing.Point(170, 15);
            this.lblBootHint.Name = "lblBootHint";
            this.lblBootHint.Size = new System.Drawing.Size(180, 40);
            this.lblBootHint.TabIndex = 2;
            this.lblBootHint.Visible = false;
            // 
            // lblBootInfo
            // 
            this.lblBootInfo.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblBootInfo.ForeColor = System.Drawing.Color.Red;
            this.lblBootInfo.Location = new System.Drawing.Point(15, 285);
            this.lblBootInfo.Name = "lblBootInfo";
            this.lblBootInfo.Size = new System.Drawing.Size(366, 30);
            this.lblBootInfo.TabIndex = 10;
            // 
            // lblFirmwareInfo
            // 
            this.lblFirmwareInfo.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.lblFirmwareInfo.ForeColor = System.Drawing.Color.DarkBlue;
            this.lblFirmwareInfo.Location = new System.Drawing.Point(224, 262);
            this.lblFirmwareInfo.Name = "lblFirmwareInfo";
            this.lblFirmwareInfo.Size = new System.Drawing.Size(157, 18);
            this.lblFirmwareInfo.TabIndex = 11;
            this.lblFirmwareInfo.Text = "Firmware: ...";
            // 
            // cbCreateBoot
            // 
            this.cbCreateBoot.Checked = true;
            this.cbCreateBoot.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbCreateBoot.Location = new System.Drawing.Point(15, 257);
            this.cbCreateBoot.Name = "cbCreateBoot";
            this.cbCreateBoot.Size = new System.Drawing.Size(220, 25);
            this.cbCreateBoot.TabIndex = 12;
            this.cbCreateBoot.CheckedChanged += new System.EventHandler(this.CbCreateBoot_CheckedChanged);
            // 
            // gbPartitionLayout
            // 
            this.gbPartitionLayout.Controls.Add(this.lblDiskSizeInfo);
            this.gbPartitionLayout.Controls.Add(this.lblBootSize);
            this.gbPartitionLayout.Controls.Add(this.numBootSize);
            this.gbPartitionLayout.Controls.Add(this.lblWinSize);
            this.gbPartitionLayout.Controls.Add(this.numWinSize);
            this.gbPartitionLayout.Controls.Add(this.lblDataSizeInfo);
            this.gbPartitionLayout.Controls.Add(this.lblDataSizeValue);
            this.gbPartitionLayout.Enabled = false;
            this.gbPartitionLayout.Location = new System.Drawing.Point(15, 318);
            this.gbPartitionLayout.Name = "gbPartitionLayout";
            this.gbPartitionLayout.Size = new System.Drawing.Size(366, 102);
            this.gbPartitionLayout.TabIndex = 14;
            this.gbPartitionLayout.TabStop = false;
            this.gbPartitionLayout.Text = "Partition Layout";
            // 
            // lblDiskSizeInfo
            // 
            this.lblDiskSizeInfo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblDiskSizeInfo.Location = new System.Drawing.Point(10, 20);
            this.lblDiskSizeInfo.Name = "lblDiskSizeInfo";
            this.lblDiskSizeInfo.Size = new System.Drawing.Size(304, 19);
            this.lblDiskSizeInfo.TabIndex = 0;
            // 
            // lblBootSize
            // 
            this.lblBootSize.Location = new System.Drawing.Point(10, 45);
            this.lblBootSize.Name = "lblBootSize";
            this.lblBootSize.Size = new System.Drawing.Size(70, 20);
            this.lblBootSize.TabIndex = 1;
            this.lblBootSize.Text = "Boot (MB):";
            // 
            // numBootSize
            // 
            this.numBootSize.Location = new System.Drawing.Point(80, 42);
            this.numBootSize.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
            this.numBootSize.Minimum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.numBootSize.Name = "numBootSize";
            this.numBootSize.Size = new System.Drawing.Size(60, 20);
            this.numBootSize.TabIndex = 2;
            this.numBootSize.Value = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.numBootSize.ValueChanged += new System.EventHandler(this.NumPartitionSize_ValueChanged);
            // 
            // lblWinSize
            // 
            this.lblWinSize.Location = new System.Drawing.Point(150, 45);
            this.lblWinSize.Name = "lblWinSize";
            this.lblWinSize.Size = new System.Drawing.Size(90, 20);
            this.lblWinSize.TabIndex = 3;
            this.lblWinSize.Text = "Windows (GB):";
            // 
            // numWinSize
            // 
            this.numWinSize.Location = new System.Drawing.Point(240, 42);
            this.numWinSize.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.numWinSize.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numWinSize.Name = "numWinSize";
            this.numWinSize.Size = new System.Drawing.Size(60, 20);
            this.numWinSize.TabIndex = 4;
            this.numWinSize.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numWinSize.ValueChanged += new System.EventHandler(this.NumPartitionSize_ValueChanged);
            // 
            // lblDataSizeInfo
            // 
            this.lblDataSizeInfo.Location = new System.Drawing.Point(10, 75);
            this.lblDataSizeInfo.Name = "lblDataSizeInfo";
            this.lblDataSizeInfo.Size = new System.Drawing.Size(50, 20);
            this.lblDataSizeInfo.TabIndex = 5;
            this.lblDataSizeInfo.Text = "Data:";
            // 
            // lblDataSizeValue
            // 
            this.lblDataSizeValue.ForeColor = System.Drawing.Color.Green;
            this.lblDataSizeValue.Location = new System.Drawing.Point(60, 75);
            this.lblDataSizeValue.Name = "lblDataSizeValue";
            this.lblDataSizeValue.Size = new System.Drawing.Size(200, 20);
            this.lblDataSizeValue.TabIndex = 6;
            this.lblDataSizeValue.Text = "0 GB";
            // 
            // gbBackup
            // 
            this.gbBackup.Controls.Add(this.lblSource);
            this.gbBackup.Controls.Add(this.cmbSource);
            this.gbBackup.Controls.Add(this.btnRefresh);
            this.gbBackup.Controls.Add(this.lblSavePath);
            this.gbBackup.Controls.Add(this.txtSavePath);
            this.gbBackup.Controls.Add(this.btnBrowse);
            this.gbBackup.Controls.Add(this.lblCompress);
            this.gbBackup.Controls.Add(this.cbCompress);
            this.gbBackup.Controls.Add(this.btnBackup);
            this.gbBackup.Controls.Add(this.btnExit);
            this.gbBackup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbBackup.Location = new System.Drawing.Point(403, 3);
            this.gbBackup.Name = "gbBackup";
            this.gbBackup.Size = new System.Drawing.Size(394, 464);
            this.gbBackup.TabIndex = 1;
            this.gbBackup.TabStop = false;
            this.gbBackup.Resize += new System.EventHandler(this.GbBackup_Resize);
            // 
            // lblSource
            // 
            this.lblSource.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSource.Location = new System.Drawing.Point(15, 20);
            this.lblSource.Name = "lblSource";
            this.lblSource.Size = new System.Drawing.Size(275, 20);
            this.lblSource.TabIndex = 0;
            // 
            // cmbSource
            // 
            this.cmbSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSource.Location = new System.Drawing.Point(15, 45);
            this.cmbSource.Name = "cmbSource";
            this.cmbSource.Size = new System.Drawing.Size(312, 21);
            this.cmbSource.TabIndex = 1;
            // 
            // btnRefresh
            // 
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("Arial", 14F, System.Drawing.FontStyle.Bold);
            this.btnRefresh.Location = new System.Drawing.Point(333, 43);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(35, 32);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "⟳";
            this.btnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
            // 
            // lblSavePath
            // 
            this.lblSavePath.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSavePath.Location = new System.Drawing.Point(15, 80);
            this.lblSavePath.Name = "lblSavePath";
            this.lblSavePath.Size = new System.Drawing.Size(220, 20);
            this.lblSavePath.TabIndex = 3;
            // 
            // txtSavePath
            // 
            this.txtSavePath.BackColor = System.Drawing.Color.White;
            this.txtSavePath.Location = new System.Drawing.Point(15, 103);
            this.txtSavePath.Name = "txtSavePath";
            this.txtSavePath.ReadOnly = true;
            this.txtSavePath.Size = new System.Drawing.Size(272, 20);
            this.txtSavePath.TabIndex = 4;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(293, 100);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 25);
            this.btnBrowse.TabIndex = 5;
            this.btnBrowse.Click += new System.EventHandler(this.BtnBrowse_Click);
            // 
            // lblCompress
            // 
            this.lblCompress.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCompress.Location = new System.Drawing.Point(15, 140);
            this.lblCompress.Name = "lblCompress";
            this.lblCompress.Size = new System.Drawing.Size(320, 20);
            this.lblCompress.TabIndex = 6;
            // 
            // cbCompress
            // 
            this.cbCompress.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCompress.Location = new System.Drawing.Point(15, 160);
            this.cbCompress.Name = "cbCompress";
            this.cbCompress.Size = new System.Drawing.Size(353, 21);
            this.cbCompress.TabIndex = 7;
            // 
            // btnBackup
            // 
            this.btnBackup.BackColor = System.Drawing.Color.LightBlue;
            this.btnBackup.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnBackup.Location = new System.Drawing.Point(15, 210);
            this.btnBackup.Name = "btnBackup";
            this.btnBackup.Size = new System.Drawing.Size(353, 45);
            this.btnBackup.TabIndex = 8;
            this.btnBackup.UseVisualStyleBackColor = false;
            this.btnBackup.Click += new System.EventHandler(this.BtnBackup_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(15, 365);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(353, 45);
            this.btnExit.TabIndex = 9;
            this.btnExit.Click += new System.EventHandler(this.BtnExit_Click);
            // 
            // chkPostAction
            // 
            this.chkPostAction.Location = new System.Drawing.Point(20, 526);
            this.chkPostAction.Name = "chkPostAction";
            this.chkPostAction.Size = new System.Drawing.Size(98, 20);
            this.chkPostAction.TabIndex = 4;
            this.chkPostAction.Text = "On Finish:";
            this.chkPostAction.CheckedChanged += new System.EventHandler(this.ChkPostAction_CheckedChanged);
            // 
            // cmbPostAction
            // 
            this.cmbPostAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPostAction.Enabled = false;
            this.cmbPostAction.Items.AddRange(new object[] {
            "Shutdown",
            "Restart"});
            this.cmbPostAction.Location = new System.Drawing.Point(136, 524);
            this.cmbPostAction.Name = "cmbPostAction";
            this.cmbPostAction.Size = new System.Drawing.Size(136, 21);
            this.cmbPostAction.TabIndex = 5;
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(278, 524);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(513, 20);
            this.progressBar.TabIndex = 6;
            // 
            // lblProgress
            // 
            this.lblProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblProgress.Location = new System.Drawing.Point(795, 527);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(27, 17);
            this.lblProgress.TabIndex = 7;
            this.lblProgress.Text = "0%";
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.BackColor = System.Drawing.Color.Black;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.ForeColor = System.Drawing.Color.Lime;
            this.txtLog.Location = new System.Drawing.Point(20, 550);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(800, 132);
            this.txtLog.TabIndex = 8;
            this.txtLog.Text = "";
            this.txtLog.WordWrap = false;
            // 
            // lnkWeb
            // 
            this.lnkWeb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lnkWeb.AutoSize = true;
            this.lnkWeb.Location = new System.Drawing.Point(0, -54);
            this.lnkWeb.Name = "lnkWeb";
            this.lnkWeb.Size = new System.Drawing.Size(0, 13);
            this.lnkWeb.TabIndex = 9;
            this.lnkWeb.Click += new System.EventHandler(this.LnkWeb_Click);
            // 
            // lnkAbout
            // 
            this.lnkAbout.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.lnkAbout.AutoSize = true;
            this.lnkAbout.Location = new System.Drawing.Point(0, -54);
            this.lnkAbout.Name = "lnkAbout";
            this.lnkAbout.Size = new System.Drawing.Size(0, 13);
            this.lnkAbout.TabIndex = 10;
            this.lnkAbout.Click += new System.EventHandler(this.LnkAbout_Click);
            // 
            // lnkGit
            // 
            this.lnkGit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lnkGit.AutoSize = true;
            this.lnkGit.Location = new System.Drawing.Point(0, -54);
            this.lnkGit.Name = "lnkGit";
            this.lnkGit.Size = new System.Drawing.Size(0, 13);
            this.lnkGit.TabIndex = 11;
            this.lnkGit.Click += new System.EventHandler(this.LnkGit_Click);
            // 
            // MainForm
            // 
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(834, 707);
            this.Controls.Add(this.lblHeader);
            this.Controls.Add(this.lblLang);
            this.Controls.Add(this.cbLang);
            this.Controls.Add(this.tlpContent);
            this.Controls.Add(this.chkPostAction);
            this.Controls.Add(this.cmbPostAction);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.lnkWeb);
            this.Controls.Add(this.lnkAbout);
            this.Controls.Add(this.lnkGit);
            this.MinimumSize = new System.Drawing.Size(850, 600);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.tlpContent.ResumeLayout(false);
            this.gbRestore.ResumeLayout(false);
            this.gbRestore.PerformLayout();
            this.gbBootMode.ResumeLayout(false);
            this.gbPartitionLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numBootSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numWinSize)).EndInit();
            this.gbBackup.ResumeLayout(false);
            this.gbBackup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void AlignFooterLinks()
        {
            // Dynamic footer Y based on client height
            int footerY = this.ClientSize.Height - 25; 
            lnkWeb.Location = new Point(20, footerY);
            lnkAbout.Location = new Point((this.ClientSize.Width - lnkAbout.Width) / 2, footerY);
            lnkGit.Location = new Point(this.ClientSize.Width - lnkGit.Width - 20, footerY);
        }

        private void UpdateButtonsLayout()
        {
            if (btnRestore != null && gbRestore != null)
                btnRestore.Location = new Point((gbRestore.Width - btnRestore.Width) / 2, 400);

            if (btnBackup != null && gbBackup != null)
                btnBackup.Location = new Point((gbBackup.Width - btnBackup.Width) / 2, 210);

            if (btnExit != null && gbBackup != null)
                btnExit.Location = new Point((gbBackup.Width - btnExit.Width) / 2, 365);
        }


        private void DetectBootModeWinPE(out bool isUEFI)
        {
            isUEFI = false;
            try
            {
                // Method 1: Official GetFirmwareType API (Most reliable)
                FIRMWARE_TYPE fwType;
                if (GetFirmwareType(out fwType))
                {
                    if (fwType == FIRMWARE_TYPE.Uefi)
                    {
                        isUEFI = true;
                        return;
                    }
                    else if (fwType == FIRMWARE_TYPE.Bios)
                    {
                        isUEFI = false;
                        return;
                    }
                }

                // Method 2: Check SecureBoot registry (fallback)
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State"))
                {
                    if (key != null)
                    {
                        isUEFI = true;
                        return;
                    }
                }
                
                // Method 3: Check firmware environment variable (fallback)
                try
                {
                    GetFirmwareEnvironmentVariable("", "{00000000-0000-0000-0000-000000000000}", IntPtr.Zero, 0);
                    int error = Marshal.GetLastWin32Error();
                    isUEFI = (error != 1); 
                }
                catch
                {
                    isUEFI = false;
                }
            }
            catch { isUEFI = false; }
        }
        
        [DllImport("kernel32.dll")]
        static extern bool GetFirmwareType(out FIRMWARE_TYPE FirmwareType);

        enum FIRMWARE_TYPE
        {
            Unknown = 0,
            Bios = 1,
            Uefi = 2,
            Max = 3
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetFirmwareEnvironmentVariable(string lpName, string lpGuid, IntPtr pBuffer, uint nSize);

        private void RefreshSourcePartitions()
        {
            cmbSource.Items.Clear();
            cmbTarget.Items.Clear();

            // 1. Populate Backup Sources & Restore Partitions
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Volume WHERE DriveType=3");
                foreach (ManagementObject vol in searcher.Get())
                {
                    try
                    {
                        string letter = vol["DriveLetter"] != null ? vol["DriveLetter"].ToString() : null;
                        string label = vol["Label"] != null ? vol["Label"].ToString() : "";
                        object capObj = vol["Capacity"];
                        long sizeGB = (capObj != null) ? Convert.ToInt64(capObj) / (1024 * 1024 * 1024) : 0;
                        
                        string dIndex, pIndex;
                        GetDiskAndPartitionIndices(letter ?? vol["Name"].ToString(), out dIndex, out pIndex);
                        
                        PartitionItem item = new PartitionItem {
                            DisplayText = string.Format("{0} [{1}] ({2} GB)", letter ?? "(Hidden)", label, sizeGB),
                            DrivePath = letter != null ? letter + "\\" : vol["DeviceID"].ToString(),
                            HasLetter = letter != null,
                            VolumeID = vol["DeviceID"].ToString(),
                            DiskIndex = dIndex,
                            PartitionIndex = pIndex
                        };
                        cmbSource.Items.Add(item);
                        
                        if (rbPartOnly.Checked) cmbTarget.Items.Add(item);
                    } catch { }
                }
            } catch { }

            // 2. Populate Physical Disks for Whole Disk Restore
            if (rbWholeDisk.Checked)
            {
                try
                {
                    ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                    foreach (ManagementObject disk in diskSearcher.Get())
                    {
                        try
                        {
                            string model = disk["Model"] != null ? disk["Model"].ToString() : GetStr("UnknownDisk");
                            string index = disk["Index"] != null ? disk["Index"].ToString() : "?";
                            long sizeGB = disk["Size"] != null ? Convert.ToInt64(disk["Size"]) / (1024 * 1024 * 1024) : 0;
                            
                            DiskItem item = new DiskItem {
                                DisplayText = string.Format("Disk {0}: {1} ({2} GB)", index, model, sizeGB),
                                DiskID = index
                            };
                            cmbTarget.Items.Add(item);
                        } catch { }
                    }
                } catch { }
            }

            // Defaults
            if (cmbSource.Items.Count > 0) cmbSource.SelectedIndex = 0;
            if (cmbTarget.Items.Count > 0) cmbTarget.SelectedIndex = 0;
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Windows Image (*.wim)|*.wim|Electronic Software Distribution (*.esd)|*.esd";
            sfd.FileName = "install.wim";
            sfd.Title = GetStr("SelectBackupLocation");
            
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string destDrive = Path.GetPathRoot(sfd.FileName);
                
                PartitionItem selItem = cmbSource.SelectedItem as PartitionItem;
                if (selItem != null && selItem.HasLetter && selItem.DrivePath.StartsWith(destDrive, StringComparison.OrdinalIgnoreCase))
                {
                   MessageBox.Show(GetStr("SameDriveErr"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                   return;
                }
                txtSavePath.Text = sfd.FileName;
                
                // Disable compression dropdown for ESD (always uses LZMS+Solid)
                bool isESD = sfd.FileName.ToUpper().EndsWith(".ESD");
                cbCompress.Enabled = !isESD;
                if (isESD) cbCompress.SelectedIndex = 2; // Show "Maximum" as visual indicator
            }
        }

        private void Log(string msg)
        {
            LogEntry entry = new LogEntry { RawText = msg, Timestamp = DateTime.Now };
            logHistory.Add(entry);
            if (txtLog.InvokeRequired) txtLog.Invoke(new Action(() => LogAppend(msg)));
            else LogAppend(msg);
        }

        private void LogAppend(string msg)
            {
                Match m = Regex.Match(msg, @"(\d+\.?\d*)\%");
                if (m.Success)
                {
                   double val = 0;
                   if (double.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                   {
                       int p = (int)val;
                       if(p > 100) p = 100;
                       progressBar.Value = p;
                       lblProgress.Text = p + "%";
                       
                       // Find last progress line using text search
                       string text = txtLog.Text;
                       int lastBracketIdx = text.LastIndexOf("[");
                       
                       if (lastBracketIdx >= 0)
                       {
                           // Find start of that line
                           int lineStart = text.LastIndexOf('\n', lastBracketIdx);
                           if (lineStart < 0) lineStart = 0;
                           else lineStart++; // Move past the \n
                           
                           // Check if this line contains percentage (is progress)
                           int lineEnd = text.IndexOf('\n', lastBracketIdx);
                           if (lineEnd < 0) lineEnd = text.Length;
                           
                           string lastLine = text.Substring(lineStart, lineEnd - lineStart);
                           if (lastLine.Contains("%"))
                           {
                               // Replace from line start to end of text with new progress
                               string newText = text.Substring(0, lineStart) + 
                                              DateTime.Now.ToString("HH:mm:ss") + " > " + msg + "\n";
                               txtLog.Text = newText;
                               txtLog.SelectionStart = txtLog.Text.Length;
                               txtLog.ScrollToCaret();
                               return;
                           }
                       }
                   }
                }

                if (msg.Contains("Version:")) return;
                if (msg.Contains("Deployment Image")) return;
                
                txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " > " + msg + "\n");
                txtLog.ScrollToCaret();
            }

        private void BtnBackup_Click(object sender, EventArgs e)
        {
            if (cmbSource.SelectedItem == null || string.IsNullOrEmpty(txtSavePath.Text))
            {
                MessageBox.Show(GetStr("SelectFile"), GetStr("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            txtLog.Clear(); 
            progressBar.Value = 0;
            lblProgress.Text = "0%";

            PartitionItem selItem = cmbSource.SelectedItem as PartitionItem;
            if (selItem == null) return;
            
            string sourcePath = selItem.DrivePath;
            string targetFile = txtSavePath.Text;
            string ext = Path.GetExtension(targetFile).ToUpper();
            
            // Determine display format/compression
            string format = (ext == ".ESD") ? "ESD (Recovery)" : "WIM (" + cbCompress.Text + ")";
            string mode = isWinPE ? "WinPE (Direct)" : "Windows (VSS)";

            string msg = string.Format(GetStr("ConfirmBackup"), targetFile, sourcePath, format, mode);
            
            if (MessageBox.Show(msg, GetStr("ConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                EnableControls(false);
                Thread t = new Thread(() => RunBackupProcess(sourcePath, targetFile));
                t.IsBackground = true;
                t.Start();
            }
        }

        private void EnableControls(bool enable)
        {
            // Restore Section (Left) - Disable entirely
            gbRestore.Enabled = enable;

            // Backup Section (Right) - Disable specific items, keep Exit active
            btnBackup.Enabled = enable;
            cmbSource.Enabled = enable;
            btnBrowse.Enabled = enable; // Save path browse
            cbCompress.Enabled = enable;
            btnRefresh.Enabled = enable; // Refresh sources
            txtSavePath.Enabled = enable;
            
            // Top/Bottom Links - KEEP ENABLED
            cbLang.Enabled = enable;

            // Keep Web, Git, About links ALWAYS enabled
            if (lnkWeb != null) lnkWeb.Enabled = true;
            if (lnkGit != null) lnkGit.Enabled = true;
            if (lnkAbout != null) lnkAbout.Enabled = true;

            // Exit button MUST remain enabled
            btnExit.Enabled = true;

            isOperationRunning = !enable;  // Track operation state
        }

        private string AssignTempLetterToGUID(string guidPath)
        {
            try
            {
                // Try S: first, then R:
                string[] letters = { "S", "R" };
                
                foreach (string letter in letters)
                {
                    if (Directory.Exists(letter + ":\\")) continue; // Already in use
                    
                    // Use WMI to assign letter
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Volume WHERE DeviceID='" + guidPath.Replace("\\", "\\\\") + "'"))
                    {
                        foreach (ManagementObject vol in searcher.Get())
                        {
                            try
                            {
                                vol["DriveLetter"] = letter + ":";
                                vol.Put();
                                
                                Thread.Sleep(1000); // Wait for assignment
                                
                                if (Directory.Exists(letter + ":\\"))
                                {
                                    Log(string.Format(GetStr("TempLetter"), letter + ":"));
                                    return letter + ":\\";
                                }
                            }
                            finally { vol.Dispose(); }
                        }
                    }
                }
            }
            catch { }
            
            Log(GetStr("TempLetterFail"));
            return null;
        }

        private void RemoveTempLetter(string letter)
        {
            try
            {
                if (string.IsNullOrEmpty(letter) || letter.Length < 2) return;
                
                string driveLetter = letter.Substring(0, 2); // "S:"
                
                // Use mountvol to remove the letter assignment
                Process p = new Process();
                p.StartInfo.FileName = "mountvol.exe";
                p.StartInfo.Arguments = driveLetter + " /D";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
                
                Log("Temp letter removed: " + driveLetter);
            }
            catch { }
        }

        // v91: Track State for Abort
        private volatile bool isBackupAborted = false;
        private volatile bool isRestoreAborted = false; // v92: Restore Abort

        private void RunBackupProcess(string sourceRoot, string targetFile)
        {
            this.isBackupRunning = true;
            this.isBackupAborted = false;
            this.currentBackupTarget = targetFile;
            
            // Toggle Button Text
            this.Invoke(new Action(() => { btnExit.Text = GetStr("BtnCancel"); }));

            LogKey("BackupStart");
            bool backupSuccess = false;
            string vssMountPath = Path.Combine(Path.GetTempPath(), "RecToolVss_" + DateTime.Now.Ticks); 
            string stagingPath = Path.Combine(Path.GetTempPath(), "RecToolStaging_" + DateTime.Now.Ticks);
            Directory.CreateDirectory(vssMountPath);

            int vssStatus = 0; 
            bool useStaging = false;
            string tempLetter = null;
            bool needsLetterCleanup = false;

            try
            {
                string captureSource = sourceRoot;
                
                // GUID Path Handling - Assign temp letter first
                if (sourceRoot.StartsWith("\\\\?\\Volume"))
                {
                    Log(GetStr("LogHiddenPart"));
                    tempLetter = AssignTempLetterToGUID(sourceRoot);
                    
                    if (tempLetter != null)
                    {
                        sourceRoot = tempLetter;
                        captureSource = tempLetter;
                        needsLetterCleanup = true;
                    }
                    else
                    {
                        LogKey("TempLetterFail");
                        return;
                    }
                }

                // ---------------------------------------------------------
                // VSS SNAPSHOT LOGIC (v92 - wimlib native)
                // ---------------------------------------------------------
                
                // Define outside if block for scope visibility
                string snapshotArg = "";
                
                if (!isWinPE && Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    snapshotArg = " --snapshot";
                    Log("Using wimlib native VSS snapshot...");
                }
                else
                {
                    LogKey("SkippingVSS");
                }

                // Get compression level from ComboBox - Map to wimlib compression
                string wimCompress = "fast"; // default
                if (cbCompress != null)
                {
                    if (cbCompress.SelectedIndex == 0) wimCompress = "none";
                    else if (cbCompress.SelectedIndex == 1) wimCompress = "fast";
                    else if (cbCompress.SelectedIndex == 2) wimCompress = "maximum";
                }
                
                // ESD always uses LZMS (recovery) compression
                if (targetFile.ToUpper().EndsWith(".ESD")) wimCompress = "LZMS";

                if (!captureSource.EndsWith("\\")) captureSource += "\\";

                // wimlib executable path
                string wimlibPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin\\wimlib-imagex.exe");

                string tempBat = Path.Combine(Path.GetTempPath(), "run_wimlib_" + DateTime.Now.Ticks + ".bat");
                
                // ---------------------------------------------------------
                // EXCLUSION LIST LOGIC (v89) - wimlib uses --config
                // ---------------------------------------------------------
                string configFilePath = "";
                try
                {
                    string exclusionTemplate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin\\wim_exclusions.ini");
                    if (File.Exists(exclusionTemplate))
                    {
                        StringBuilder sbEx = new StringBuilder();
                        string[] lines = File.ReadAllLines(exclusionTemplate);
                        // Rebuild content and insert dynamic paths after [ExclusionList]
                        foreach (string line in lines)
                        {
                            sbEx.AppendLine(line);
                            if (line.Trim().Equals("[ExclusionList]", StringComparison.OrdinalIgnoreCase))
                            {
                                // Dynamic User Scan (Cloud Folders) - INSERT HERE
                                if (Directory.Exists(captureSource + "Users"))
                                {
                                    try
                                    {
                                        string[] users = Directory.GetDirectories(captureSource + "Users");
                                        foreach (string userDir in users)
                                        {
                                            string userName = Path.GetFileName(userDir);
                                            string[] cloudApps = { "OneDrive", "SkyDrive", "Google Drive", "Dropbox" };
                                            
                                            foreach (string app in cloudApps)
                                            {
                                                if (Directory.Exists(Path.Combine(userDir, app)))
                                                {
                                                    sbEx.AppendLine("\\Users\\" + userName + "\\" + app);
                                                }
                                            }
                                            
                                            if (Directory.Exists(Path.Combine(userDir, "AppData\\Local\\Temp")))
                                                sbEx.AppendLine("\\Users\\" + userName + "\\AppData\\Local\\Temp");
                                        }
                                        Log(GetStr("LogDynamicExclusions"));
                                    }
                                    catch {}
                                }
                            }
                        }

                        configFilePath = Path.Combine(Path.GetTempPath(), "wimlib_config_" + DateTime.Now.Ticks + ".ini");
                        // Use UTF-8 encoding for wimlib compatibility
                        File.WriteAllText(configFilePath, sbEx.ToString(), Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    Log(string.Format(GetStr("LogExclusionWarning"), ex.Message));
                }

                string exclusionArg = !string.IsNullOrEmpty(configFilePath) ? " --config=\"" + configFilePath + "\"" : "";

                // ESD requires --solid for proper compression
                string solidArg = targetFile.ToUpper().EndsWith(".ESD") ? " --solid" : "";

                // wimlib capture command
                // --snapshot: Create VSS snapshot (Online only)
                // --check: Verify integrity
                string wimCmd = string.Format("\"{0}\" capture \"{1}\" \"{2}\" \"WindowsBackup\" \"Created_by_RecoveryTool\" --compress={3} --check{4}{5}{6}", 
                                               wimlibPath, captureSource.TrimEnd('\\'), targetFile, wimCompress, solidArg, exclusionArg, snapshotArg);
                                               
                // Show command, then message, then run
                string analyzeMsg = GetStr("WimlibAnalyze");
                string batContent = "@echo off\r\n" +
                                   "echo " + wimCmd + "\r\n" +
                                   "echo.\r\n" +
                                   "echo " + analyzeMsg + "\r\n" +
                                   wimCmd + "\r\n";
                File.WriteAllText(tempBat, batContent);
                
                LogKey("RunningWimlib");
                bool success = RunProcess("cmd.exe", "/c \"" + tempBat + "\"");
                
                if(File.Exists(tempBat)) File.Delete(tempBat);

                if (success) 
                {
                    LogKey("BackupDone");
                    backupSuccess = true;
                }
                else 
                {
                    if (!isBackupAborted) LogKey("BackupFail");
                    
                    // Delete incomplete WIM file on failure
                    if (File.Exists(targetFile))
                    {
                        try 
                        { 
                            File.Delete(targetFile); 
                            LogKey("IncompleteBackupDeleted", targetFile);
                        } 
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LogKey("ExecError", ex.Message);
                LogKey("LogStackTrace", ex.StackTrace);
                if (ex.InnerException != null) LogKey("LogInnerException", ex.InnerException.Message);
            }
            finally
            {
                this.isBackupRunning = false; // v91: Reset
                this.currentBackupTarget = "";
                
                // vssStatus and vssMountPath are no longer used with wimlib --snapshot
                
                if (useStaging && Directory.Exists(stagingPath))
                {
                    LogKey("CleaningStaging");
                    try { Directory.Delete(stagingPath, true); } catch {}
                }

                if (needsLetterCleanup && tempLetter != null)
                {
                    RemoveTempLetter(tempLetter);
                }

                this.Invoke(new Action(() => {
                    EnableControls(true);
                    btnExit.Text = GetStr("Exit"); // Fix: Use correct key 'Exit'
                    if (backupSuccess)
                    {
                        if (chkPostAction.Checked)
                        {
                            LogKey("BackupDone");
                            CheckPostAction();
                        }
                        else
                        {
                            MessageBox.Show(GetStr("BackupDone"), GetStr("ConfirmTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        }
                    }
                    else
                    {
                        if (!isBackupAborted)
                        {
                            MessageBox.Show(GetStr("BackupFail"), GetStr("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        }
                        else
                        {
                            ResetLogState();
                        }
                    }
                }));
            }
        }

        private string CreateShadowCopyPS(string drive, out string errorMsg)
        {
            errorMsg = "";
            string ps1File = Path.Combine(Path.GetTempPath(), "vss_create_" + DateTime.Now.Ticks + ".ps1");
            
            try {
                // Create PS1 file exactly like batch does
                string ps1Content = "$drive = '" + drive + "'\r\n" +
                                   "$s = (Get-WmiObject -List Win32_ShadowCopy).Create($drive, 'ClientAccessible')\r\n" +
                                   "if ($s.ReturnValue -eq 0) {\r\n" +
                                   "    $shadow = Get-WmiObject Win32_ShadowCopy | Where-Object { $_.ID -eq $s.ShadowID }\r\n" +
                                   "    Write-Output $shadow.DeviceObject\r\n" +
                                   "} else {\r\n" +
                                   "    Write-Error \"VSS Error Code: $($s.ReturnValue)\"\r\n" +
                                   "    exit 1\r\n" +
                                   "}";
                
                File.WriteAllText(ps1File, ps1Content);
                
                Process p = new Process();
                p.StartInfo.FileName = "powershell";
                p.StartInfo.Arguments = "-ExecutionPolicy Bypass -NoProfile -File \"" + ps1File + "\"";
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                
                string outStr = p.StandardOutput.ReadToEnd().Trim();
                string errStr = p.StandardError.ReadToEnd().Trim();
                p.WaitForExit();

                if (File.Exists(ps1File)) File.Delete(ps1File);

                if (!string.IsNullOrEmpty(errStr)) 
                {
                    errorMsg = errStr;
                    return null;
                }
                
                if (!string.IsNullOrEmpty(outStr) && outStr.Contains("HarddiskVolumeShadowCopy"))
                {
                    return outStr;
                }
                
                errorMsg = "VSS returned empty path";
                return null;
                
            } catch (Exception ex) { 
                errorMsg = ex.Message;
                if (File.Exists(ps1File)) try { File.Delete(ps1File); } catch { }
                return null;
            }
        }

        private string GetShadowDevicePath(string shadowID)
        {
            // No longer needed - VSS now returns DeviceObject directly
            return null;
        }

        private void CleanupMount(string path)
        {
            if (Directory.Exists(path)) try { Directory.Delete(path); } catch { }
        }


        private bool RunProcess(string exe, string args)
        {
            try
            {
                using (Process p = new Process())
                {
                string fullExe = exe;
                if (!exe.Contains("\\") && (exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !exe.Contains(".")))
                {
                    string sys32 = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), "System32");
                    string target = Path.Combine(sys32, exe.EndsWith(".exe") ? exe : exe + ".exe");
                    
                    if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                    {
                        string sysnative = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), "Sysnative");
                        string nativeTarget = Path.Combine(sysnative, exe.EndsWith(".exe") ? exe : exe + ".exe");
                        if (File.Exists(nativeTarget)) fullExe = nativeTarget;
                        else if (File.Exists(target)) fullExe = target;
                    }
                    else if (File.Exists(target))
                    {
                        fullExe = target;
                    }
                }
                
                // If simple file name, try to find in Sysnative explicitly
                if (!exe.Contains("\\") && Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                {
                     string sysnative = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), "Sysnative");
                     string nativeCandidate = Path.Combine(sysnative, exe.EndsWith(".exe") ? exe : exe + ".exe");
                     if (File.Exists(nativeCandidate)) fullExe = nativeCandidate;
                }

                p.StartInfo.FileName = fullExe;
                p.StartInfo.Arguments = args;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;

                p.OutputDataReceived += (s, d) => { if (d.Data != null) Log(d.Data); };
                p.ErrorDataReceived += (s, d) => { if (d.Data != null) Log(string.Format(GetStr("LogErrorPrefix"), d.Data)); };

                lock(processLock) { this.currentProcess = p; } // v91: Track for kill
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                
                // Timeout for diskpart operations (5 minutes)
                // WinPE can have very slow disk I/O
                bool exited = false;
                if (exe.ToLower().Contains("diskpart"))
                {
                    exited = p.WaitForExit(300000); // 5 minutes timeout
                    if (!exited)
                    {
                        LogKey("DiskPartTimeout");
                        try 
                        { 
                            if (!p.HasExited) 
                            { 
                                p.Kill(); 
                                p.WaitForExit(5000); // Wait for kill
                            }
                        } 
                        catch { }
                        return false;
                    }
                }
                else
                {
                    p.WaitForExit();
                }
                
                if (exe.ToLower().Contains("robocopy")) return p.ExitCode <= 7;
                
                if (p.ExitCode != 0)
                {
                    if (!isBackupAborted) LogKey("ProcessExitCode", p.ExitCode);
                }
                
                lock(processLock) { this.currentProcess = null; } // v91: Release
                return p.ExitCode == 0;
                } // End using
            }
            catch (Exception ex)
            {
                LogKey("ExecException", ex.Message);
                return false;
            }
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtWimPath.Text))
            {
                MessageBox.Show(GetStr("WimUnsetErr"), GetStr("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbTarget.SelectedItem == null)
            {
                MessageBox.Show(GetStr("DiskUnsetErr"), GetStr("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Online check for C:

            // System Disk Protection check
            string systemDisk = GetSystemDiskIndex();
            
            // Partition Mode check
            if (rbPartOnly.Checked)
            {
                PartitionItem pi = cmbTarget.SelectedItem as PartitionItem;
                if (pi != null && pi.HasLetter)
                {
                    string systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)).ToUpper().Substring(0, 2);
                    if (pi.DrivePath.ToUpper().StartsWith(systemDrive))
                    {
                        string message = isWinPE 
                            ? string.Format(GetStr("ErrSamePartPE"), systemDrive)
                            : string.Format(GetStr("ErrSamePartWin"), systemDrive);
                        MessageBox.Show(message, GetStr("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            
            // Whole Disk check
            if (rbWholeDisk.Checked)
            {
                DiskItem di = cmbTarget.SelectedItem as DiskItem;
                if (di != null && systemDisk != null && di.DiskID == systemDisk)
                {
                    string message = isWinPE 
                        ? string.Format(GetStr("ErrSameDiskPE"), di.DiskID)
                        : string.Format(GetStr("ErrSameDiskWin"), di.DiskID);
                    MessageBox.Show(message, GetStr("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Online Whole Disk safety check

    if (MessageBox.Show(GetStr("ConfirmRestore"), GetStr("ConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                EnableControls(false);
                Thread t = new Thread(RunRestoreProcess);
                t.IsBackground = true;
                t.Start();
            }
        }

        private void RunRestoreProcess()
        {
            isOperationRunning = true;
            bool restoreSuccess = false;
            try
            {
                string wimFile = "";
                bool isWholeDisk = false;
                string targetID = "";
                bool useGPT = false;
                bool doBoot = false;

                this.Invoke(new Action(() => {
                    wimFile = txtWimPath.Text;
                    isWholeDisk = rbWholeDisk.Checked;
                    
                    // v108: For Partition Restore, auto-detect GPT/MBR from actual disk
                    // For Whole Disk restore, use UI selection
                    if (isWholeDisk)
                    {
                        useGPT = rbGPT.Checked;
                    }
                    else
                    {
                        // Auto-detect partition table type from target disk
                        string diskIdx = (cmbTarget.SelectedItem as PartitionItem).DiskIndex;
                        useGPT = IsDiskGPT(diskIdx);
                        Log("Auto-detected disk " + diskIdx + " partition table: " + (useGPT ? "GPT (UEFI)" : "MBR (BIOS)"));
                    }
                    
                    doBoot = cbCreateBoot.Checked;
                    if (isWholeDisk) targetID = (cmbTarget.SelectedItem as DiskItem).DiskID;
                    else targetID = (cmbTarget.SelectedItem as PartitionItem).DiskIndex;
                    txtLog.Clear();
                }));

                LogKey("RestoreStart");
                string windowsDrive = "";
                string bootDrive = "";
                string wimIndex = "1";

                this.Invoke(new Action(() => {
                    if (cmbWimIndex.SelectedItem is WimIndexItem)
                        wimIndex = (cmbWimIndex.SelectedItem as WimIndexItem).Index;
                }));

                if (isWholeDisk)
                {
                    if (doBoot)
                    {
                        Log(GetStr("Formatting") + " (Disk " + targetID + ")");
                        long bootSizeMB = 0;
                        long winSizeGB = 0;
                        long totalDiskGB = 0;

                        this.Invoke(new Action(() => {
                            bootSizeMB = (long)numBootSize.Value;
                            winSizeGB = (long)numWinSize.Value;
                            totalDiskGB = currentDiskSizeGB;
                        }));


                        // ---------------------------------------------------------
                        // STRATEGY: DYNAMIC DRIVE LETTER ASSIGNMENT
                        // ---------------------------------------------------------
                        
                        string targetWinLetter = "W";
                        string targetBootLetter = "S";
                        string targetDataLetter = "D";


                        // 1. Windows Letter Logic
                        LogKey("LetterLogicStart", isWinPE);
                        if (isWinPE)
                        {
                            // In WinPE, if C is free OR if C belongs to the disk we are about to wipe, USE IT.
                            bool cIsAvailable = IsDriveLetterFree("C");
                            if (!cIsAvailable)
                            {
                                string cDisk = "";
                                string cPart = "";
                                GetDiskAndPartitionIndices("C:", out cDisk, out cPart);
                                if (cDisk == targetID) 
                                {
                                    cIsAvailable = true;
                                    LogKey("C_is_OwnedByTarget");
                                }
                            }
                            LogKey("CheckLetterC", cIsAvailable);
                            if (cIsAvailable) targetWinLetter = "C";
                        }
                        else
                        {
                             // Online logic
                        }
                        
                        // Capture existing letter if possible (Best Effort)
                        string originalLetter = GetFirstDriveLetterOnDisk(targetID);
                        LogKey("OriginalLetterCheck", originalLetter ?? "null");
                        if (!string.IsNullOrEmpty(originalLetter) && !isWinPE)
                        {
                             // Clean up colon
                             targetWinLetter = originalLetter.Substring(0, 1).ToUpper();
                             LogKey("PreserveLetter", targetWinLetter);
                        }
                        if (!string.IsNullOrEmpty(originalLetter) && !isWinPE)
                        {
                             // Clean up colon
                             targetWinLetter = originalLetter.Substring(0, 1).ToUpper();
                        }



                        // Conflict Check: Ensure S/D are not taken. If taken, find next free.
                        if (!IsDriveLetterFree(targetBootLetter)) targetBootLetter = GetFirstFreeDriveLetterChecked(new List<string> { targetWinLetter });
                        if (!IsDriveLetterFree(targetDataLetter)) targetDataLetter = GetFirstFreeDriveLetterChecked(new List<string> { targetWinLetter, targetBootLetter });


                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("SELECT DISK " + targetID);
                        sb.AppendLine("CLEAN");
                        
                        if (useGPT)
                        {
                            sb.AppendLine("CONVERT GPT");
                            sb.AppendLine("CREATE PARTITION EFI SIZE=" + bootSizeMB);
                            sb.AppendLine("FORMAT QUICK FS=FAT32 LABEL=\"EFI_BOOT\"");
                            sb.AppendLine("ASSIGN LETTER=" + targetBootLetter);
                            sb.AppendLine("CREATE PARTITION MSR SIZE=16");
                            
                            long usedGB = (long)Math.Ceiling((double)bootSizeMB / 1024.0);
                            if (winSizeGB < (totalDiskGB - usedGB))
                            {
                                sb.AppendLine("CREATE PARTITION PRIMARY SIZE=" + (winSizeGB * 1024));
                            }
                            else
                            {
                                sb.AppendLine("CREATE PARTITION PRIMARY"); 
                            }
                            sb.AppendLine("FORMAT QUICK FS=NTFS LABEL=\"Win_OS\"");
                            sb.AppendLine("ASSIGN LETTER=" + targetWinLetter);

                            if (winSizeGB < (totalDiskGB - usedGB))
                            {
                                sb.AppendLine("CREATE PARTITION PRIMARY");
                                sb.AppendLine("FORMAT QUICK FS=NTFS LABEL=\"Files\"");
                                sb.AppendLine("ASSIGN LETTER=" + targetDataLetter);
                            }
                        }
                        else
                        {
                            sb.AppendLine("CONVERT MBR");
                            sb.AppendLine("CREATE PARTITION PRIMARY SIZE=" + bootSizeMB);
                            sb.AppendLine("FORMAT QUICK FS=NTFS LABEL=\"MBR_BOOT\"");
                            sb.AppendLine("ACTIVE");
                            sb.AppendLine("ASSIGN LETTER=" + targetBootLetter);
                            
                            long usedGB = (long)Math.Ceiling((double)bootSizeMB / 1024.0);
                            if (winSizeGB < (totalDiskGB - usedGB))
                            {
                                sb.AppendLine("CREATE PARTITION PRIMARY SIZE=" + (winSizeGB * 1024));
                            }
                            else
                            {
                                 sb.AppendLine("CREATE PARTITION PRIMARY"); 
                            }
                            sb.AppendLine("FORMAT QUICK FS=NTFS LABEL=\"Win_OS\"");
                            sb.AppendLine("ASSIGN LETTER=" + targetWinLetter);

                            if (winSizeGB < (totalDiskGB - usedGB))
                            {
                                sb.AppendLine("CREATE PARTITION PRIMARY");
                                sb.AppendLine("FORMAT QUICK FS=NTFS LABEL=\"Files\"");
                                sb.AppendLine("ASSIGN LETTER=" + targetDataLetter);
                            }
                        }

                        string scriptPath = Path.Combine(Path.GetTempPath(), "diskpart_gen_" + DateTime.Now.Ticks + ".txt");
                        File.WriteAllText(scriptPath, sb.ToString());


                        // ---------------------------------------------------------
                        // End of Letter Logic
                        // ---------------------------------------------------------        }

                        // 1. Separate Initialization Step (Non-Fatal)
                        // This ensures the disk is online and writable before the main script runs.
                        // We toggle OFFLINE/ONLINE to break potential locks in WinPE.
                        string initScriptPath = Path.Combine(Path.GetTempPath(), "init_disk.txt");
                        File.WriteAllText(initScriptPath, string.Format("SELECT DISK {0}\r\nOFFLINE DISK\r\nONLINE DISK\r\nATTRIBUTES DISK CLEAR READONLY", targetID));
                        LogKey("InitializingDisk");
                        RunProcess("diskpart.exe", "/s \"" + initScriptPath + "\"");

                        LogKey("RunningDiskPart");
                        if (!RunProcess("diskpart.exe", "/s \"" + scriptPath + "\"")) 
                        { 
                            LogKey("DiskPartFatal", targetID);
                            return; 
                        }
                        
                        Thread.Sleep(2000); 
                        
                        // ---------------------------------------------------------
                        // DISCOVERY PHASE - Using DiskPart Parsing (Bypassing WMI Lag)
                        // ---------------------------------------------------------
                        
                        // 1. Find Windows Partition (Win_OS)
                        windowsDrive = GetDriveLetterFromDiskPart("Win_OS");
                        
                        // If Windows partition didn't get a letter (preserved letter may be in use), assign a free one
                        if (string.IsNullOrEmpty(windowsDrive))
                        {
                            LogKey("WinPartNoLetter");
                            int winVolNum = GetVolumeNumberByLabel("Win_OS");
                            if (winVolNum != -1)
                            {
                                string freeLetter = GetFirstFreeDriveLetter();
                                if (freeLetter == null) freeLetter = "Y"; // Fallback
                                
                                string assignScript = string.Format("select volume {0}\nassign letter={1}", winVolNum, freeLetter);
                                string assignPath = Path.Combine(Path.GetTempPath(), "assign_win.txt");
                                File.WriteAllText(assignPath, assignScript);
                                RunProcess("diskpart.exe", "/s \"" + assignPath + "\"");
                                windowsDrive = freeLetter + ":\\";
                                LogKey("AssignedLetter", freeLetter);
                            }
                            else
                            {
                                LogKey("CantFindWinVol");
                            }
                        }
                        
                        
                    }
                    else
                    {
                        // Apply Only Mode
                    }
                }
                else
                {
                    PartitionItem pi = null;
                    this.isRestoreAborted = false; // Reset Abort Flag
                    this.Invoke(new Action(() => {
                       EnableControls(false); 
                       btnExit.Text = GetStr("BtnCancel"); // Change to Cancel
                       pi = cmbTarget.SelectedItem as PartitionItem;
                    }));
                    
                    // v107: Skip format if doBoot is enabled (reconstruction will handle partition creation & format)
                    if (!doBoot)
                    {
                        LogKey("FormatPart", pi.DisplayText);
                        string letter = pi.DrivePath.TrimEnd('\\');
                        string formatCmd = string.Format("/c format {0} /q /y /fs:ntfs /v:Win_OS", letter);
                        if (!RunProcess("cmd.exe", formatCmd)) 
                        { 
                            LogKey("FormatError");
                            return; 
                        }
                        windowsDrive = pi.DrivePath;
                    }
                    else
                    {
                        Log("Skipping format (reconstruction will create & format partitions)...");
                        windowsDrive = pi.DrivePath; // Will be updated after reconstruction
                    }
                    
                    // Attempt to find existing boot partition on the same physical disk
                    // Add delay to ensure labels are readable after format
                    Thread.Sleep(2000);
                    if (doBoot)
                    {
                        // v124: Use UI selection directly (no auto-detection)
                        int actualPartIndex = int.Parse(pi.PartitionIndex);
                        Log("Using UI selected partition: Index " + actualPartIndex);
                        
                        // v103: Always try to find and format the preceding EFI/Boot partition first
                        bootDrive = FindAndFormatPrecedingBootPartition(pi.DiskIndex, actualPartIndex, useGPT);

                        // v107: Check if reconstruction occurred and update windowsDrive
                        if (!string.IsNullOrEmpty(newWindowsDriveAfterReconstruction))
                        {
                            windowsDrive = newWindowsDriveAfterReconstruction;
                            Log("Using reconstructed Windows partition: " + windowsDrive);
                            newWindowsDriveAfterReconstruction = null; // Reset for next operation
                        }

                        // v111: Even if reconstruction reported failure, EFI partition may have been created
                        // Try to find it by label before giving up
                        if (string.IsNullOrEmpty(bootDrive))
                        {
                            Log("Reconstruction returned null, searching for EFI partition by label...");
                            Thread.Sleep(2000); // Allow partition to settle
                            bootDrive = FindBootPartitionOnSameDisk(pi.DiskIndex, useGPT); 
                        }

                        if (string.IsNullOrEmpty(bootDrive))
                        {
                            LogKey("BootPartNotFound");
                            LogKey("Repartitioning", pi.PartitionIndex, pi.DiskIndex);

                            // Requires two free letters: one for Boot, one for the new Windows partition
                            string freeBoot = GetFirstFreeDriveLetterChecked();
                            
                            // Preserve original letter if exists, otherwise find free
                            string originalLetter = pi.HasLetter ? pi.DrivePath.Substring(0, 1) : null;
                            string freeWin = originalLetter ?? GetFirstFreeDriveLetterChecked(new List<string> { freeBoot });

                            // Script to delete partition and create Boot + Windows
                            string splitScript = string.Format(
                                "select disk {0}\n" +
                                "select partition {1}\n" +
                                "delete partition override\n" +
                                // Create Boot (500MB)
                                "{2}" +
                                // Create Windows (Remaining)
                                "create partition primary\n" +
                                "format quick fs=ntfs label=\"Win_OS\"\n" +
                                "assign letter={3}",
                                    pi.DiskIndex,
                                    pi.PartitionIndex,
                                    useGPT ? 
                                        "create partition efi size=200\nformat quick fs=fat32 label=\"EFI_BOOT\"\nassign letter=" + freeBoot + "\n" :
                                        "create partition primary size=200\nformat quick fs=ntfs label=\"MBR_BOOT\"\nactive\nassign letter=" + freeBoot + "\n",
                                    freeWin
                                );

                            string scriptPath = Path.Combine(Path.GetTempPath(), "split_script.txt");
                            File.WriteAllText(scriptPath, splitScript);

                            if (RunProcess("diskpart.exe", "/s \"" + scriptPath + "\""))
                            {
                                Thread.Sleep(2000); // Wait for volumes
                                if (Directory.Exists(freeBoot + ":\\") && Directory.Exists(freeWin + ":\\"))
                                {
                                    bootDrive = freeBoot + ":\\";
                                    windowsDrive = freeWin + ":\\";
                                    LogKey("RepartitionSuccess", bootDrive, windowsDrive);
                                }
                                else
                                {
                                    LogKey("RepartitionWarning");
                                    // Fallback checks
                                    if (Directory.Exists(freeWin + ":\\")) windowsDrive = freeWin + ":\\";
                                    if (Directory.Exists(freeBoot + ":\\")) bootDrive = freeBoot + ":\\";
                                }
                            }
                            else
                            {
                                Log(GetStr("RepartitionError"));
                            }
                        }
                    }

                }

                // ---------------------------------------------------------
                // COMMON APPLY & BOOT LOGIC
                // ---------------------------------------------------------
                
                if (!File.Exists(wimFile))
                {
                    LogKey("WimNotFound", wimFile);
                    return;
                }

                // Wait for drive to be ready (Handling race condition) - v40 logic kept
                int maxRetries = 20; 
                bool driveReady = false;
                for (int i = 0; i < maxRetries; i++)
                {
                    if (Directory.Exists(windowsDrive))
                    {
                        driveReady = true;
                        break;
                    }
                    LogKey("WaitingTarget", windowsDrive);
                    Thread.Sleep(500);
                }

                if (!driveReady)
                {
                     LogKey("TargetUnreachable", windowsDrive);
                     return;
                }
                
                  
                string selectedIndex = "1";
                this.Invoke(new Action(() => {
                    WimIndexItem sel = cmbWimIndex.SelectedItem as WimIndexItem;
                    if (sel != null) selectedIndex = sel.Index;
                }));

                // wimlib Apply
                Log(GetStr("ApplyingImage") + " (Index: " + wimIndex + ")");
                
                string wimlibPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin\\wimlib-imagex.exe");
                string wimlibArgs = string.Format("apply \"{0}\" {1} \"{2}\" --check", wimFile, wimIndex, windowsDrive.TrimEnd('\\'));
                if (!RunProcess(wimlibPath, wimlibArgs)) 
                { 
                    LogKey("WimlibApplyError"); 
                    return; 
                }

                // Boot Record
                if (doBoot)
                {
                    LogKey("CreatingBoot");

                    // For BIOS Partition-only restore, ensure partition is active
                    if (!useGPT && !isWholeDisk)
                    {
                        try {
                            LogKey("MarkingActive");
                            string activeScript = string.Format("select volume {0}\nactive", windowsDrive.TrimEnd('\\'));
                            string activePath = Path.Combine(Path.GetTempPath(), "active.txt");
                            File.WriteAllText(activePath, activeScript);
                            RunProcess("diskpart.exe", "/s \"" + activePath + "\"");
                        } catch {}
                    }

                    string winPath = windowsDrive.TrimEnd('\\') + "\\Windows";
                    
                    // DEBUG: Log bootDrive status before selecting bootTarget
                    if (isWholeDisk)
                    {
                        // For whole disk mode, boot partition was assigned a letter during formatting
                        // via template (#BOOTLETTER# replacement). Now we just need to FIND it.
                        string bootLabel = useGPT ? "EFI_BOOT" : "MBR_BOOT";
                        bootDrive = GetDriveLetterFromDiskPart(bootLabel);
                        

                    }
                    
                    string bootTarget = !string.IsNullOrEmpty(bootDrive) ? bootDrive.TrimEnd('\\') : windowsDrive.TrimEnd('\\');
                    

                    
                    // Verify boot target is accessible before running BCDBoot (critical for EFI)
                    if (!Directory.Exists(bootTarget))
                    {
                        LogKey("BootTargetUnreachable", bootTarget);
                        LogKey("WaitingBootPart");
                        Thread.Sleep(2000);
                        
                        if (!Directory.Exists(bootTarget))
                        {
                            LogKey("BootTargetStillUnreachable");
                        }
                    }
                    
                    // bcdboot source /s target /f firmware
                    string bcdArgs = string.Format("\"{0}\" /s {1} /f {2}", winPath, bootTarget, useGPT ? "UEFI" : "BIOS");
                    
                    LogKey("BCDBootCmd", bcdArgs);

                    if (!RunProcess("bcdboot.exe", bcdArgs)) 
                    { 
                        LogKey("BCDBootError");
                        string bcdArgsAll = string.Format("\"{0}\" /s {1} /f ALL", winPath, bootTarget);
                        RunProcess("bcdboot.exe", bcdArgsAll);
                    }
                    
                    // Verify boot files were created (EFI check)
                    if (useGPT && !string.IsNullOrEmpty(bootDrive))
                    {
                        // Wait for filesystem to flush (BCDBoot writes may be cached)
                        Thread.Sleep(3000);
                        
                        string efiBootFile = bootTarget + "\\EFI\\Microsoft\\Boot\\bootmgfw.efi";
                        
                        // Try multiple times with increasing delays
                        bool fileFound = false;
                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            if (File.Exists(efiBootFile))
                            {
                                fileFound = true;
                                break;
                            }
                            
                            if (attempt < 2)
                            {
                                LogKey("BootFileNotFound", (attempt + 1));
                                Thread.Sleep(2000);
                            }
                        }
                        
                        if (fileFound)
                        {
                            Log(string.Format(GetStr("BootFilesVerified"), bootTarget));
                            
                            // List files to confirm they're actually there
                            try
                            {
                                string efiDir = bootTarget + "\\EFI\\Microsoft\\Boot";
                                if (Directory.Exists(efiDir))
                                {
                                    string[] files = Directory.GetFiles(efiDir);
                                    Log(string.Format(GetStr("BootDirStats"), files.Length));
                                    foreach (string f in files)
                                    {
                                        Log("  - " + Path.GetFileName(f));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log(string.Format(GetStr("LogBootListError"), ex.Message));
                            }
                            
                            // Force flush to disk before removing letter
                            try
                            {
                                Log(GetStr("LogFlushDisk"));
                                RunProcess("diskpart.exe", "/s \"" + Path.Combine(Path.GetTempPath(), "flush.txt") + "\"");
                            }

                            catch { }

                            // ---------------------------------------------------------
                            // FALLBACK: Create EFI\BOOT\BOOTX64.EFI (v87)
                            // ---------------------------------------------------------
                            try
                            {
                                string fallbackDir = bootTarget + "\\EFI\\BOOT";
                                string fallbackFile = fallbackDir + "\\BOOTX64.EFI";
                                string sourceEfi = bootTarget + "\\EFI\\Microsoft\\Boot\\bootmgfw.efi";

                                if (!Directory.Exists(fallbackDir)) Directory.CreateDirectory(fallbackDir);

                                if (File.Exists(sourceEfi) && !File.Exists(fallbackFile))
                                {
                                    File.Copy(sourceEfi, fallbackFile);
                                    Log(string.Format(GetStr("LogFallbackBootCreated"), "EFI\\BOOT\\BOOTX64.EFI"));
                                }
                            }
                            catch (Exception ex)
                            {
                                Log(string.Format(GetStr("LogFallbackBootFailed"), ex.Message));
                            } 
                            }

                        else
                        {
                            Log(string.Format(GetStr("LogBootFileNotFoundArgs"), efiBootFile));
                            Log(GetStr("LogSystemBootWarning"));
                        }
                    }

                    // Remove temporary boot letter (for both whole disk and partition modes)
                    if (!string.IsNullOrEmpty(bootDrive) && !bootDrive.Equals(windowsDrive, StringComparison.OrdinalIgnoreCase))
                    {
                        string tempLetter = bootDrive.TrimEnd('\\').Replace(":", "");
                        if (tempLetter.Length == 1)
                        {
                            Log(string.Format(GetStr("LogRemoveTempLetter"), tempLetter));
                            RemoveDriveLetter(tempLetter);
                        }
                    }
                }


                Log(GetStr("RestoreDone"));
                restoreSuccess = true;
            }
            catch (Exception ex)
            {
                Log(string.Format(GetStr("LogRestoreException"), ex.Message));
                LogKey("LogStackTrace", ex.StackTrace);
                if (ex.InnerException != null) LogKey("LogInnerException", ex.InnerException.Message);
            }
            finally
            {
                isOperationRunning = false;
                this.Invoke(new Action(() => {
                    EnableControls(true);
                    btnExit.Text = GetStr("Exit"); // Fix: Use correct key 'Exit'
                    RefreshSourcePartitions();
                    if (restoreSuccess)
                    {
                        if (chkPostAction.Checked)
                        {
                            LogKey("RestoreDone");
                            CheckPostAction();
                        }
                        else
                        {
                            MessageBox.Show(GetStr("RestoreDone"), GetStr("ConfirmTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        }
                    }
                    else
                    {
                        if (!isRestoreAborted)
                        {
                            MessageBox.Show(GetStr("RestoreFail"), GetStr("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        }
                        else
                        {
                            ResetLogState();
                        }
                    }
                }));
            }
        }

        private void GetDiskAndPartitionIndices(string volumeName, out string diskIndex, out string partIndex)
        {
            diskIndex = null;
            partIndex = "1"; // Default, will be updated

            // Tier 1: Native IOCTL for Disk Index
            try
            {
                string path = volumeName.TrimEnd('\\');
                if (path.StartsWith("\\\\?\\")) path = path.Replace("\\\\?\\", "\\\\.\\");
                else if (path.Length == 2 && path[1] == ':') path = "\\\\.\\" + path;

                IntPtr hVol = CreateFile(path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (hVol.ToInt64() == -1) 
                    hVol = CreateFile(path, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

                if (hVol != IntPtr.Zero && hVol.ToInt64() != -1)
                {
                    uint bytesReturned;
                    int size = Marshal.SizeOf(typeof(VOLUME_DISK_EXTENTS));
                    IntPtr ptr = Marshal.AllocHGlobal(size);
                    try
                    {
                        if (DeviceIoControl(hVol, IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS, IntPtr.Zero, 0, ptr, (uint)size, out bytesReturned, IntPtr.Zero))
                        {
                            int numExtents = Marshal.ReadInt32(ptr);
                            if (numExtents > 0)
                            {
                                VOLUME_DISK_EXTENTS vde = (VOLUME_DISK_EXTENTS)Marshal.PtrToStructure(ptr, typeof(VOLUME_DISK_EXTENTS));
                                diskIndex = vde.Extents[0].DiskNumber.ToString();
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                        CloseHandle(hVol);
                    }
                }
            } catch { }

            // Tier 2: WMI / DiskPart to get both Disk and Partition Index accurately
            // WMI can be unreliable in WinPE, so we rely on DiskPart 'list partition' behavior
            // When a volume is selected, the underlying partition is selected in 'list partition' with '*'
            
            try
            {
                string scriptPath = Path.Combine(Path.GetTempPath(), "info_part.txt");
                
                string volIdForDiskPart = volumeName;
                if (volumeName.StartsWith("\\\\?\\") && !volumeName.EndsWith("\\")) volIdForDiskPart = volumeName + "\\";
                else if (!volumeName.StartsWith("\\\\?\\")) volIdForDiskPart = volumeName.TrimEnd('\\');

                // If we know the disk index, select it first to be safe, though select volume usually switches context
                string script = "select volume \"" + volIdForDiskPart + "\"\nlist disk\nlist partition";
                
                File.WriteAllText(scriptPath, script);
                
                Process p = new Process();
                p.StartInfo.FileName = "diskpart.exe";
                p.StartInfo.Arguments = "/s \"" + scriptPath + "\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                using (StringReader sr = new StringReader(output))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Trim();
                        // Find Disk Index (marked with *)
                        if (line.StartsWith("*") && line.ToLower().Contains("disk"))
                        {
                            Match m = Regex.Match(line, @"Disk\s+(\d+)");
                            if (m.Success) diskIndex = m.Groups[1].Value;
                        }
                        // Fallback if not marked but previous method failed
                        else if (diskIndex == null && line.ToLower().Contains("disk") && line.Contains("Online"))
                        {
                             // This is risky without selection, but 'list disk' with volume selected should mark it
                        }

                        // Find Partition Index (marked with *)
                        if (line.StartsWith("*") && line.ToLower().Contains("partition"))
                        {
                            Match m = Regex.Match(line, @"Partition\s+(\d+)");
                            if (m.Success) partIndex = m.Groups[1].Value;
                        }
                    }
                }
            } catch { }
            
            if (diskIndex == null) diskIndex = "0"; // Last resort
        }

        private string FindAndFormatPrecedingBootPartition(string diskIndex, int targetPartIndex, bool useGPT)
        {
            // v107: Complete "Clean" Reconstruction Logic
            // Strategy: Delete ALL preceding partitions + Target partition, then create fresh EFI + Windows.
            //
            // Steps:
            // 1. Delete all partitions BEFORE target
            // 2. Delete TARGET partition itself
            // 3. Create NEW EFI/Boot partition (200MB)
            // 4. Create NEW Windows partition (remaining space)
            // 5. Format Windows as NTFS
            // 6. Return EFI drive letter (for boot files) + Update windowsDrive reference
            
            // CRITICAL: This means we need to pass back BOTH boot drive AND new windows drive.
            // But method signature only returns string (bootDrive).
            // Solution: Use a class-level variable to communicate new Windows drive, OR
            // change the entire flow.
            
            // For now, let's try a different approach:
            // This method will ONLY create the EFI partition if conditions are met.
            // If we need full reconstruction, return NULL and let the fallback "Repartitioning" logic handle it.
            
            // Actually, looking at the code flow (line 2215), there IS already a fallback repartitioning logic!
            // So let's just return NULL here if we detect that target needs to be deleted.
            
            // But user wants THIS function to do the work, not the fallback.
            // OK, let's do full reconstruction HERE.
            
            try
            {
                Log("Starting FULL Reconstruction Logic for Disk " + diskIndex + ", Target Index " + targetPartIndex);
                
                // v110: Delete preceding partitions + target, then create fresh EFI + Windows
                // IMPORTANT: This preserves partitions AFTER the target (e.g., Data drives on D:, E:)
                
                // --- Step 0: Get target partition info ---
                var allPartitions = GetDiskPartitions(diskIndex);
                var targetPartition = allPartitions.FirstOrDefault(p => p.Index == targetPartIndex);
                long targetSizeMB = targetPartition != null ? targetPartition.SizeMB : 0;
                string targetType = targetPartition != null ? targetPartition.Type : "";
                Log("Selected partition: Index " + targetPartIndex + ", Type: " + targetType + ", Size: " + targetSizeMB + " MB");
                
                
                // v121: Force remove C: from any remaining partitions to avoid conflicts
                // v125: EXACT USER LOGIC - Delete Target + Preceding partitions manually
                // User example: Select Vol 3 (EFI) -> del, Select Vol 1 (Windows) -> del
                // Then create new
                
                // 1. Delete Target Partition (e.g. Windows)
                Log("Deleting Target Partition: " + targetPartIndex);
                string delTargetScript = "select disk " + diskIndex + "\nselect partition " + targetPartIndex + "\ndelete partition override";
                string delTargetPath = Path.Combine(Path.GetTempPath(), "del_target.txt");
                File.WriteAllText(delTargetPath, delTargetScript);
                RunProcess("diskpart.exe", "/s \"" + delTargetPath + "\"");
                
                // 2. Delete Preceding Partitions (e.g. EFI, MSR) to clear space at front
                // Iterate backwards from target-1 down to 1
                for (int idx = targetPartIndex - 1; idx >= 1; idx--)
                {
                     Log("Deleting Preceding Partition: " + idx);
                     string delPrecScript = "select disk " + diskIndex + "\nselect partition " + idx + "\ndelete partition override";
                     File.WriteAllText(Path.Combine(Path.GetTempPath(), "del_prec.txt"), delPrecScript);
                     RunProcess("diskpart.exe", "/s \"" + Path.Combine(Path.GetTempPath(), "del_prec.txt") + "\"");
                }
                
                // 3. Rescan
                Log("Rescanning disk...");
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "rescan.txt"), "rescan");
                RunProcess("diskpart.exe", "/s \"" + Path.Combine(Path.GetTempPath(), "rescan.txt") + "\"");
                Thread.Sleep(3000);

                // 4. Create new layout
                Log("Creating new partition layout...");
                string bootLabel = "EFI_BOOT";
                
                // v127: Dynamic Letter Assignment
                string dynamicBootLet = GetFirstFreeDriveLetterChecked(); // e.g. G
                string dynamicWinLet = GetFirstFreeDriveLetterChecked(new List<string> { dynamicBootLet, "C" }); // e.g. H (Skip C)
                
                if (dynamicBootLet == null || dynamicWinLet == null)
                {
                    Log("Error: Could not find free drive letters.");
                    return null;
                }

                Log("Using temporary letters: Boot=" + dynamicBootLet + " Win=" + dynamicWinLet);

                string script;
                if (useGPT)
                {
                    script = string.Format(
                        "select disk {0}\n" +
                        "create partition efi size=200\n" +
                        "format quick fs=fat32 label=\"{1}\" override\n" +
                        "assign letter={2}\n" +
                        "create partition msr size=16\n" +
                        "create partition primary\n" + 
                        "format quick fs=ntfs label=\"Win_OS\" override\n" +
                        "assign letter={3}",
                        diskIndex,
                        bootLabel,
                        dynamicBootLet,
                        dynamicWinLet
                    );
                }
                else
                {
                    script = string.Format(
                        "select disk {0}\n" +
                        "create partition primary size=200\n" +
                        "active\n" +
                        "format quick fs=fat32 label=\"{1}\" override\n" +
                        "assign letter={2}\n" +
                        "create partition primary\n" + 
                        "format quick fs=ntfs label=\"Win_OS\" override\n" +
                        "assign letter={3}",
                        diskIndex,
                        bootLabel,
                        dynamicBootLet,
                        dynamicWinLet
                    );
                }

                string scriptPath = Path.Combine(Path.GetTempPath(), "create_efi_win.txt");
                File.WriteAllText(scriptPath, script);
                
                Log("Creating partitions with OVERRIDE...");
                Log("DiskPart CREATE script content:\n" + script);
                
                bool success = RunProcess("diskpart.exe", "/s \"" + scriptPath + "\"");
                
                if (success)
                {
                     // Wait for mounting
                     for (int i = 0; i < 5; i++)
                     {
                        Thread.Sleep(1000);
                        if (Directory.Exists(dynamicBootLet + ":\\") && Directory.Exists(dynamicWinLet + ":\\"))
                        {
                            Log("SUCCESS: Boot=" + dynamicBootLet + ":, Windows=" + dynamicWinLet + ": (temp)");
                            
                            // Reassign Windows -> C:
                            Log("Reassigning Windows partition from " + dynamicWinLet + ": to C:...");
                            string reassignScript = "select volume " + dynamicWinLet + "\nremove\nassign letter=C";
                            string reassignPath = Path.Combine(Path.GetTempPath(), "reassign.txt");
                            File.WriteAllText(reassignPath, reassignScript);
                            RunProcess("diskpart.exe", "/s \"" + reassignPath + "\"");
                            Thread.Sleep(2000);
                            
                            newWindowsDriveAfterReconstruction = "C:\\";
                            return dynamicBootLet + ":\\";
                        }
                     }
                     // Fallback
                     newWindowsDriveAfterReconstruction = "C:\\"; 
                     return dynamicBootLet + ":\\"; 
                }
                
                Log("Reconstruction failed.");
            }
            catch (Exception ex)
            {
                 Log("Reconstruction Error: " + ex.Message);
            }
            return null;
        }
        
        // Class-level variable to communicate new Windows drive after reconstruction
        private string newWindowsDriveAfterReconstruction = null;

        private class DiskPartInfo {
            public int Index;
            public string Type;
            public long SizeMB;
        }

        private List<DiskPartInfo> GetDiskPartitions(string diskIndex)
        {
            List<DiskPartInfo> list = new List<DiskPartInfo>();
            try {
                string script = "select disk " + diskIndex + "\nlist partition";
                string scriptPath = Path.Combine(Path.GetTempPath(), "list_part.txt");
                File.WriteAllText(scriptPath, script);
                
                Process p = new Process();
                p.StartInfo.FileName = "diskpart.exe";
                p.StartInfo.Arguments = "/s \"" + scriptPath + "\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                
                // DEBUG: Log raw output to see what we're parsing
                Log("=== DiskPart List Partition Output (Disk " + diskIndex + ") ===");
                Log(output);
                Log("=== End DiskPart Output ===");

                using (StringReader sr = new StringReader(output))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // Match lines like: "  Partition 1    System             100 MB  1024 KB"
                        // Or Turkish: "  Bölüm 1        Sistem             ..."
                        
                        // Skip header separator lines "  -------------  ..."
                        if (line.Trim().StartsWith("-")) continue;

                        try {
                            // Split by whitespace
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length < 3) continue;

                            // Standard column 2 is Index (if header is Partition ### Type Size ...)
                            // Actually tokens: [Keyword] [Index] [Type] ...
                            
                            int idx;
                            if (!int.TryParse(parts[1], out idx)) continue; // Ensure 2nd token is a number

                            string type = parts[2]; 
                            
                            // Find size pair (Value + Unit)
                            // DiskPart output: "Partition 1  System  200 MB  1024 KB"
                            // We want the LARGEST value in MB (200 MB, not 1024 KB)
                            long sizeVal = 0;
                            for(int i=2; i<parts.Length-1; i++) {
                                string unit = parts[i+1].ToUpper();
                                if (unit == "MB" || unit == "GB" || unit == "KB") {
                                    long val = 0;
                                    if(long.TryParse(parts[i], out val)) {
                                        // Convert to MB
                                        if (unit == "GB") val *= 1024;
                                        else if (unit == "KB") val /= 1024;
                                        
                                        // Keep the largest value (actual partition size, not offset)
                                        if (val > sizeVal) sizeVal = val;
                                    }
                                }
                            }
                            
                            list.Add(new DiskPartInfo { Index = idx, Type = type, SizeMB = sizeVal });
                        } catch {}
                    }
                }
            } catch {}
            return list;
        }

        private bool IsDiskGPT(string diskIndex)
        {
            // Check if disk uses GPT or MBR partition table
            // Uses DiskPart "list disk" command and checks for "Gpt" column marker (*)
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "diskpart.exe";
                p.StartInfo.Arguments = "";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                
                p.StandardInput.WriteLine("list disk");
                p.StandardInput.WriteLine("exit");
                p.StandardInput.Close();
                
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                
                // Parse output looking for our disk and checking if it has "*" in Gpt column
                // Example output:
                //   Disk ###  Status         Size     Free     Dyn  Gpt
                //   --------  -------------  -------  -------  ---  ---
                //   Disk 0    Online          100 GB      0 B        *
                //   Disk 1    Online          500 GB      0 B
                
                using (StringReader sr = new StringReader(output))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Trim().StartsWith("Disk " + diskIndex))
                        {
                            // Check if line contains "*" in rightmost area (indicates GPT)
                            // Usually the last character or near it
                            if (line.TrimEnd().EndsWith("*"))
                            {
                                return true; // GPT
                            }
                            return false; // MBR
                        }
                    }
                }
            }
            catch { }
            
            // Default fallback: assume GPT for modern systems
            return true;
        }


        private string FindBootPartitionOnSameDisk(string diskIndex, bool useGPT)
        {
            try
            {
                // look for partitions with boot labels across all disks first
                string[] bootLabels = useGPT ? new[] { "EFI_BOOT", "System", "EFI" } : new[] { "MBR_BOOT", "System Reserved" };
                foreach (string lbl in bootLabels)
                {
                    string drv = GetDriveLetterByLabel(lbl);
                    if (!string.IsNullOrEmpty(drv)) return drv;
                }
            } catch { }
            return null;
        }

        private string GetDriveLetterByLabel(string label)
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Volume WHERE Label='" + label + "'");
                foreach (ManagementObject vol in searcher.Get())
                {
                    if (vol["DriveLetter"] != null) return vol["DriveLetter"].ToString() + "\\";
                }
            } catch { }
            return null;
        }

        private bool IsDriveLetterInUse(string letter)
        {
            return Directory.Exists(letter.TrimEnd(':') + ":\\");
        }

        private string GetFirstFreeDriveLetter(List<string> exclude = null)
        {
            var used = DriveInfo.GetDrives().Select(d => d.Name.Substring(0, 1).ToUpper()).ToList();
            
            for (char c = 'Z'; c >= 'C'; c--)
            {
                string letter = c.ToString();
                if (exclude != null && exclude.Contains(letter)) continue;
                if (!used.Contains(letter) && !Directory.Exists(letter + ":\\"))
                {
                    return letter;
                }
            }
            return null;
        }

        private void AssignDriveLetter(string currentPath, string newLetter)
        {
            string script = string.Format("select volume {0}\nassign letter={1}", currentPath.TrimEnd('\\'), newLetter.TrimEnd(':'));
            string scriptPath = Path.Combine(Path.GetTempPath(), "assign_letter.txt");
            WriteTextSafe(scriptPath, script);
            RunProcess("diskpart.exe", "/s \"" + scriptPath + "\"");
        }

        private bool AssignDriveLetterByDeviceID(string deviceID, string newLetter)
        {
            // DiskPart cannot reliably select by Volume GUID.
            // We use the simpler method: call bcdboot directly on the volume if needed,
            // or assign by index. Since whole disk restore handles its own assignments,
            // this is mostly for edge cases.
            return false;
        }

        private bool IsDriveLetterFree(string letter)
        {
            string root = letter.TrimEnd('\\', ':') + ":\\";
            // Check if drive exists
            return !Directory.Exists(root);
        }



        private bool AssignLetterToDiskPartition(string diskIndex, int partIndex, string letter)
        {
            // To be robust, we select disk, select partition, then select volume
            string script = string.Format("select disk {0}\nselect partition {1}\nassign letter={2}", diskIndex, partIndex, letter.TrimEnd(':'));
            // If Partition 1 is a hidden type, we might need to select it specifically
            string scriptPath = Path.Combine(Path.GetTempPath(), "assign_letter_idx.txt");
            WriteTextSafe(scriptPath, script);
            return RunProcess("diskpart.exe", "/s \"" + scriptPath + "\"");
        }

        private void RemoveDriveLetter(string letter)
        {
            try {
                string script = string.Format("select volume {0}:\nremove letter={0}", letter.TrimEnd(':'));
                string scriptPath = Path.Combine(Path.GetTempPath(), "remove_letter.txt");
                WriteTextSafe(scriptPath, script);
                RunProcess("diskpart.exe", "/s \"" + scriptPath + "\"");
            } catch { }
        }

        private void ForceClearDriveLetter(string letter)
        {
            try
            {
                // Method 1: DiskPart (Standard)
                RemoveDriveLetter(letter);

                // Method 2: WMI (Aggressive)
                // Select all volumes with the given drive letter
                string query = string.Format("SELECT * FROM Win32_Volume WHERE DriveLetter = '{0}:'", letter.TrimEnd(':'));
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject vol in searcher.Get())
                    {
                        try
                        {
                            Log(string.Format(GetStr("LogWMIRemoveLetter"), letter));
                            // This removes the drive letter by setting it to null
                            vol["DriveLetter"] = null;
                            vol.Put();
                            Log(GetStr("LogWMISuccess"));
                        }
                        catch (Exception ex) { Log(string.Format(GetStr("LogWMIClearError"), ex.Message)); }
                        finally { vol.Dispose(); }
                    }
                }
                
                // Method 3: PowerShell (Nuclear) - Silence errors if not found
                string psCmd = string.Format("Get-Partition -DriveLetter {0} -ErrorAction SilentlyContinue | Remove-PartitionAccessPath -DriveLetter {0} -ErrorAction SilentlyContinue", letter.TrimEnd(':'));
                RunProcess("powershell.exe", "-Command \"" + psCmd + "\"");
            }
            catch { }
        }

        private int GetPartitionCount(string diskIndex)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskPartition WHERE DiskIndex=" + diskIndex))
                {
                    ManagementObjectCollection coll = searcher.Get();
                    return coll.Count;
                }
            } catch { return 0; }
        }

        private string GetSystemDiskIndex()
        {
            try
            {
                // Method 1: WMI Associators (Normal Windows)
                string systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)).ToUpper().Substring(0, 2);
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(string.Format("ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{0}'}} WHERE AssocClass=Win32_LogicalDiskToPartition", systemDrive)))
                {
                    foreach (ManagementObject part in searcher.Get())
                    {
                        try
                        {
                            string partID = part["DeviceID"].ToString();
                            Match m = Regex.Match(partID, @"Disk #(\d+),");
                            if (m.Success) return m.Groups[1].Value;
                        }
                        finally { part.Dispose(); }
                    }
                }

                // Method 2: BCDEdit Fallback (WinPE RAM Disk context)
                // If X: is RAM-disk, find where BOOTMGR/BCD is located
                Process p = new Process();
                p.StartInfo.FileName = "bcdedit.exe";
                p.StartInfo.Arguments = "/get {bootmgr}";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                // Look for: device partition=\Device\HarddiskVolumeN  or  partition=C:
                Match volMatch = Regex.Match(output, @"device\s+partition=(\\Device\\HarddiskVolume(\d+)|([A-Z]:))", RegexOptions.IgnoreCase);
                if (volMatch.Success)
                {
                    // Fallback to searching all Win32_DiskPartition for any association with the boot environment
                    // For DiskPart safety, we can check Disk 0 as the high-probability candidate in BIOS/UEFI systems
                    if (output.Contains("HarddiskVolume")) return "0"; 
                }
            } catch { }
            return null;
        }

        private string GetFirstDriveLetterOnDisk(string diskIndex)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskPartition WHERE DiskIndex=" + diskIndex))
                {
                    foreach (ManagementObject part in searcher.Get())
                    {
                        try
                        {
                            using (ManagementObjectSearcher logSearch = new ManagementObjectSearcher("ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + part["DeviceID"] + "'} WHERE AssocClass=Win32_LogicalDiskToPartition"))
                            {
                                foreach (ManagementObject logDisk in logSearch.Get())
                                {
                                     try { if(logDisk["DeviceID"] != null) return logDisk["DeviceID"].ToString().Replace(":", ""); }
                                     finally { logDisk.Dispose(); }
                                }
                            }
                        }
                        finally { part.Dispose(); }
                    }
                }
            } catch {}
            return null;
        }

        private string GetDriveLetterFromDiskPart(string label)
        {
            try
            {
                 // Run diskpart list volume
                 Process p = new Process();
                 p.StartInfo.FileName = "diskpart.exe";
                 string scriptPath = Path.Combine(Path.GetTempPath(), "list_vol.txt");
                 WriteTextSafe(scriptPath, "list volume");
                 p.StartInfo.Arguments = "/s \"" + scriptPath + "\"";
                 p.StartInfo.UseShellExecute = false;
                 p.StartInfo.RedirectStandardOutput = true;
                 p.StartInfo.CreateNoWindow = true;
                 p.Start();
                 string output = p.StandardOutput.ReadToEnd();
                 p.WaitForExit();

                 // Output format:
                 // Volume ###  Ltr  Label        Fs     Type        Size     Status     Info
                 // ----------  ---  -----------  -----  ----------  -------  ---------  --------
                 // Volume 1     C   Windows      NTFS   Partition    100 GB  Healthy    Boot
                 // Volume 2         EFI_BOOT     FAT32  Partition    100 MB  Healthy    System
                 
                 using (StringReader sr = new StringReader(output))
                 {
                     string line;
                     while ((line = sr.ReadLine()) != null)
                     {
                         if (line.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0)
                         {
                             // We found the line with the label. Now find the Ltr column.
                             // Ltr is usually the 2nd column. "Volume 1     C   "
                             // Regex to extract Ltr
                             // Volume\s+\d+\s+([A-Z])\s+
                             Match m = Regex.Match(line, @"Volume\s+\d+\s+([A-Z])\s+");
                             if (m.Success) return m.Groups[1].Value + ":\\";
                         }
                     }
                 }
            } catch {}
            return null;
        }

        private int GetVolumeNumberByLabel(string label)
        {
            try
            {
                 Process p = new Process();
                 p.StartInfo.FileName = "diskpart.exe";
                 string scriptPath = Path.Combine(Path.GetTempPath(), "list_vol.txt");
                 WriteTextSafe(scriptPath, "list volume");
                 p.StartInfo.Arguments = "/s \"" + scriptPath + "\"";
                 p.StartInfo.UseShellExecute = false;
                 p.StartInfo.RedirectStandardOutput = true;
                 p.StartInfo.CreateNoWindow = true;
                 p.Start();
                 string output = p.StandardOutput.ReadToEnd();
                 p.WaitForExit();
                 
                 using (StringReader sr = new StringReader(output))
                 {
                     string line;
                     while ((line = sr.ReadLine()) != null)
                     {
                         if (line.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0)
                         {
                             // Parse: Volume <num> <letter> <label> <fs> <type> <size>
                             // Example: "  Volume 3     X        EFI_BOOT    FAT32   Partition    500 MB"
                             Match m = Regex.Match(line, @"Volume\s+(\d+)\s");
                             if (m.Success)
                             {
                                 int volNum = int.Parse(m.Groups[1].Value);
                                 
                                 // Extract size (e.g., "500 MB", "100 GB")
                                 Match sizeMatch = Regex.Match(line, @"(\d+)\s+(MB|GB|KB)", RegexOptions.IgnoreCase);
                                 if (sizeMatch.Success)
                                 {
                                     int sizeValue = int.Parse(sizeMatch.Groups[1].Value);
                                     string sizeUnit = sizeMatch.Groups[2].Value.ToUpper();
                                     
                                     // Convert to MB for comparison
                                     int sizeMB = sizeValue;
                                     if (sizeUnit == "GB") sizeMB = sizeValue * 1024;
                                     else if (sizeUnit == "KB") sizeMB = sizeValue / 1024;
                                     
                                     // Only match SMALL partitions (< 1GB) IF it's a BOOT label
                                     // This filters out other EFI partitions while allowing large OS partitions
                                     bool isBootLabel = label.IndexOf("BOOT", StringComparison.OrdinalIgnoreCase) >= 0;
                                     if (!isBootLabel || sizeMB < 1024)
                                     {
                                         Log(string.Format(GetStr("LogFoundVolume"), label, volNum, sizeMB));
                                         return volNum;
                                     }
                                     else
                                     {
                                         Log(string.Format(GetStr("LogSkipLargeVolume"), label, sizeMB));
                                     }
                                 }
                                 else
                                 {
                                     // No size found, assume it's the right one (fallback)
                                     return volNum;
                                 }
                             }
                         }
                     }
                 }
            } catch {}
            return -1;
        }

        private void KillProcesses()
        {
            try
            {
                // Kill wimlib-imagex, DiskPart and other related tools if they are still running
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c taskkill /F /IM wimlib-imagex.exe /T & taskkill /F /IM diskpart.exe /T & taskkill /F /IM format.com /T";
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi).WaitForExit();
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isBackupRunning || isOperationRunning)
            {
                e.Cancel = true;
                BtnExit_Click(this, EventArgs.Empty);
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            // v91: Special handling for Backup
            if (isBackupRunning)
            {
                DialogResult result = MessageBox.Show(
                    GetStr("BackupAbortWarning"),
                    GetStr("BackupAbortTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result != DialogResult.Yes) return;

                try
                {
                    isBackupAborted = true; // Signal Abort

                    // 1. Kill Process
                    if (currentProcess != null && !currentProcess.HasExited)
                    {
                        try { currentProcess.Kill(); } catch { }
                    }
                    KillProcesses(); 

                    // 2. Delete Partial File
                    if (!string.IsNullOrEmpty(currentBackupTarget) && File.Exists(currentBackupTarget))
                    {
                        try { File.Delete(currentBackupTarget); } catch { }
                    }

                    // 3. Delete temp config file if exists
                    if (!string.IsNullOrEmpty(currentBackupTarget))
                    {
                         // Clean up any temp files (wimlib doesn't use scratch dirs)
                    }
                }
                catch { }
                
                // Log(GetStr("BackupAborted"));
                // Application.Exit();
                return;
            }

            if (isOperationRunning)
            {
                DialogResult result = MessageBox.Show(
                    GetStr("ExitConfirm"), 
                    GetStr("ConfirmTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    isRestoreAborted = true; // Signal Restore Abort
                    KillProcesses();
                    
                    if (currentProcess != null && !currentProcess.HasExited)
                    {
                        try { currentProcess.Kill(); } catch { }
                    }
                    
                    // Do NOT exit app, let finally block handle reset
                    // Application.Exit(); 
                }
                return;
            }
            Application.Exit();
        }



        private void CheckTargetDiskStyle()
        {
            if (rbPartOnly.Checked && cmbTarget.SelectedItem is PartitionItem)
            {
                PartitionItem pi = cmbTarget.SelectedItem as PartitionItem;
                string style = GetDiskStyle(pi.DiskIndex);
                
                gbBootMode.Enabled = true; // KEEP ENABLED per user request
                if (style == "GPT")
                {
                    rbGPT.Checked = true;
                    lblBootInfo.Text = GetStr("GPTDetectMsg");
                }
                else
                {
                    rbMBR.Checked = true;
                    lblBootInfo.Text = GetStr("MBRDetectMsg");
                }
            }
            else
            {
                // Whole Disk Mode: Boot Mode selection should remain enabled 
                // regardless of "Create Boot Record" checkbox, per user request.
                // "Create Boot Record selected or not -> Boot Mode NOT passive".
                gbBootMode.Enabled = true;
                lblBootInfo.Text = "";
            }
        }

        private string GetDiskStyle(string diskIndex)
        {
            try
            {
                // Run diskpart list disk to see if it has GPT *
                Process p = new Process();
                p.StartInfo.FileName = "diskpart.exe";
                // We create a script to list disks
                string scriptPath = Path.Combine(Path.GetTempPath(), "list_disk.txt");
                File.WriteAllText(scriptPath, "list disk");
                
                p.StartInfo.Arguments = "/s \"" + scriptPath + "\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                
                // Parse output
                // Disk ###  Status      Size     Free     Dyn  Gpt
                // Disk 0    Online       100 GB      0 B        *
                
                using (StringReader sr = new StringReader(output))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        // Match strict Disk N line start
                        // DiskPart output is aligned. "Disk 2 "
                        if (Regex.IsMatch(line, @"^\s*Disk\s+" + diskIndex + @"\s+"))
                        {
                            // Check if last character is * or Gpt column has *
                            if (line.Trim().EndsWith("*")) return "GPT";
                            else return "MBR";
                        }
                    }
                }
            } catch {}
            return "MBR"; // Default
        }
        private void CheckPostAction()
        {
            if (chkPostAction.Checked)
            {
                bool isRestart = (cmbPostAction.SelectedIndex == 1);
                string args = isRestart ? "/r /t 0" : "/s /t 0";
                
                if (isWinPE)
                {
                    string cmd = isRestart ? "reboot" : "shutdown";
                    try { Process.Start("wpeutil", cmd); } catch { }
                }
                else
                {
                    try { Process.Start("shutdown", args); } catch { }
                }
            }
        }

        private DialogResult ShowTopMostMessage(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return MessageBox.Show(text, caption, buttons, icon, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
        }

        private void GetWimInfo(string wimPath)
        {
             cmbWimIndex.Items.Clear();
             LogKey("WimAnalysis");
             
             Thread t = new Thread(() => {
                 try {
                     Process p = new Process();
                     // Use wimlib-imagex info instead of DISM
                     string wimlibPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin\\wimlib-imagex.exe");
                     p.StartInfo.FileName = wimlibPath;
                     p.StartInfo.Arguments = "info \"" + wimPath + "\"";
                     p.StartInfo.UseShellExecute = false;
                     p.StartInfo.RedirectStandardOutput = true;
                     p.StartInfo.CreateNoWindow = true;
                     p.Start();
                     string output = p.StandardOutput.ReadToEnd();
                     p.WaitForExit();
                     
                     // Parser Logic for wimlib output
                     // wimlib format: "Index:                  1" and "Name:                   Windows 10 Pro"
                     List<WimIndexItem> items = new List<WimIndexItem>();
                     WimIndexItem current = null;
                     
                     using (StringReader sr = new StringReader(output))
                     {
                         string line;
                         while ((line = sr.ReadLine()) != null)
                         {
                             // wimlib uses "Index:" format (colon attached)
                             if (line.TrimStart().StartsWith("Index:"))
                             {
                                 if (current != null) items.Add(current);
                                 current = new WimIndexItem();
                                 current.Index = line.Substring(line.IndexOf(':') + 1).Trim();
                             }
                             else if (current != null)
                             {
                                 if (line.TrimStart().StartsWith("Name:")) 
                                     current.Name = line.Substring(line.IndexOf(':') + 1).Trim();
                                 else if (line.TrimStart().StartsWith("Description:")) 
                                     current.Version = line.Substring(line.IndexOf(':') + 1).Trim();
                             }
                         }
                         if (current != null) items.Add(current);
                     }
                     
                     this.Invoke(new Action(() => {
                         cmbWimIndex.Items.Clear();
                         foreach(var item in items) cmbWimIndex.Items.Add(item);
                         
                         if (cmbWimIndex.Items.Count > 0) cmbWimIndex.SelectedIndex = 0;
                         else 
                         {
                             cmbWimIndex.Items.Add(GetStr("DefaultImage"));
                             cmbWimIndex.SelectedIndex = 0;
                         }
                         
                         LogKey("WimInfoLoaded", items.Count);
                     }));
                 }
                 catch (Exception ex) 
                 {
                     this.Invoke(new Action(() => {
                         LogKey("WimInfoError", ex.Message);
                         cmbWimIndex.Items.Clear();
                         cmbWimIndex.Items.Add(GetStr("DefaultImage"));
                         if (cmbWimIndex.Items.Count > 0) cmbWimIndex.SelectedIndex = 0;
                     }));
                 }
             });
             t.IsBackground = true;
             t.Start();
        }

        private string GetPartitionSizeFromTemplate(string templateName)
        {
            try
            {
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin\\" + templateName);
                if (File.Exists(templatePath))
                {
                    string content = File.ReadAllText(templatePath);
                    Match m = Regex.Match(content, @"SIZE=(\d+)", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        return m.Groups[1].Value;
                    }
                }
            }
            catch { }
            return "500"; // Default Fallback

        }

        private void WriteTextSafe(string path, string content)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
                {
                    sw.Write(content);
                    sw.Flush();
                }
                Thread.Sleep(100);
            }
            catch { }
        }

        private void CbLang_SelectedIndexChanged(object sender, EventArgs e)
        {
            LanguageItem sel = cbLang.SelectedItem as LanguageItem;
            if (sel != null)
            {
                // Save Preference
                try {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\BackupRestoreTool")) {
                        if (key != null) key.SetValue("Language", sel.Code);
                    }
                } catch { }

                LoadLanguage(sel.Code);
                UpdateUILanguage();
                RefreshSourcePartitions(); 
            }
        }

        private void BtnBrowseWim_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Windows Image (*.wim;*.esd)|*.wim;*.esd";
            if (ofd.ShowDialog() == DialogResult.OK) 
            {
                txtWimPath.Text = ofd.FileName;
                GetWimInfo(ofd.FileName);
            }
        }

        private void RbWholeDisk_CheckedChanged(object sender, EventArgs e)
        {
            RefreshSourcePartitions(); 
            lblRestoreTarget.Text = rbWholeDisk.Checked ? GetStr("TargetDisk") : GetStr("TargetPart");
            CheckTargetDiskStyle();
        }



        private void BtnRefreshRestore_Click(object sender, EventArgs e)
        {
            RefreshSourcePartitions();
        }

        private void CbCreateBoot_CheckedChanged(object sender, EventArgs e)
        {
            CheckTargetDiskStyle();
        }

        private void GbRestore_Resize(object sender, EventArgs e)
        {
            // UpdateButtonsLayout(); // Disabled to respect Designer
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
             RefreshSourcePartitions();
        }
        
        private void GbBackup_Resize(object sender, EventArgs e)
        {
             // UpdateButtonsLayout(); // Disabled to respect Designer
        }

        private void ChkPostAction_CheckedChanged(object sender, EventArgs e)
        {
             cmbPostAction.Enabled = chkPostAction.Checked;
        }

        private long currentDiskSizeGB = 0;

        private void CalculatePartitionSizes()
        {
            if (currentDiskSizeGB <= 0) return;

            long bootMB = (long)numBootSize.Value;
            long winGB = (long)numWinSize.Value;

            long totalUsedGB = winGB + (bootMB / 1024);
            long dataGB = currentDiskSizeGB - totalUsedGB;

            if (dataGB < 0)
            {
                lblDataSizeValue.Text = GetStr("NoSpace");
                lblDataSizeValue.ForeColor = Color.Red;
            }
            else
            {
                lblDataSizeValue.Text = dataGB + " GB";
                lblDataSizeValue.ForeColor = Color.Green;
            }
        }

        private void CmbTarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTarget.SelectedItem == null) return;
            
            CheckTargetDiskStyle();
            
            gbPartitionLayout.Visible = false;
            currentDiskSizeGB = 0;

            if (rbWholeDisk.Checked)
            {
                DiskItem disk = cmbTarget.SelectedItem as DiskItem;
                if (disk != null)
                {
                    // Parse size from display text safely
                    string pattern = @"\((\d+) GB\)";
                    Match m = Regex.Match(disk.DisplayText, pattern);
                    if (m.Success)
                    {
                        currentDiskSizeGB = long.Parse(m.Groups[1].Value);
                        lblDiskSizeInfo.Text = GetStr("TotalDiskSize") + " " + currentDiskSizeGB + " GB";
                        
                        // Default Values
                        numWinSize.Maximum = currentDiskSizeGB;
                        numWinSize.Value = currentDiskSizeGB; // Default to full
                        
                        // LOGIC: Show but maybe disable
                        gbPartitionLayout.Visible = true;
                        
                        bool isSystemDisk = false;
                        if (!isWinPE)
                        {
                            string sysDiskInd = GetSystemDiskIndex();
                            if (disk.DiskID == sysDiskInd) isSystemDisk = true;
                        }

                        if (isWinPE || !isSystemDisk)
                        {
                            // Active
                            gbPartitionLayout.Enabled = true;
                        }
                        else
                        {
                            // Passive (Windows running on target)
                            gbPartitionLayout.Enabled = false;
                        }

                    }
                }
            }
            CalculatePartitionSizes();
        }

        private void LnkWeb_Click(object sender, EventArgs e)
        {
             try { Process.Start("https://erturk.netlify.app"); } catch { } 
             try { Process.Start("https://www.tnctr.com/"); } catch { } 
        }

        private void LnkAbout_Click(object sender, EventArgs e)
        {
             MessageBox.Show(GetStr("AboutMessage"), GetStr("About"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LnkGit_Click(object sender, EventArgs e)
        {
             try { Process.Start("https://github.com/abdullah-erturk"); } catch { }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            AlignFooterLinks();
            // UpdateButtonsLayout(); // Disabled to respect Designer
        }

        private void RbRestoreMode_CheckedChanged(object sender, EventArgs e)
        {
            RefreshSourcePartitions(); 
            CmbTarget_SelectedIndexChanged(null, null);
        }

        private void NumPartitionSize_ValueChanged(object sender, EventArgs e)
        {
            CalculatePartitionSizes();
        }
        private void ResetLogState()
        {
            txtLog.Clear();
            logHistory.Clear(); // Fix: Clear history to prevent reappearance
            
            LogKey("SystemInit");
            if (!isWinPE) Log(GetStr("LogOnlineVSS"));

            bool isU = false;
            DetectBootModeWinPE(out isU);
            LogKey("FirmwareMode", (isU ? "UEFI" : "BIOS"));
            
            // Allow immediate visual feedback
            if (progressBar != null)
            {
                progressBar.Value = 0;
                progressBar.Style = ProgressBarStyle.Blocks;
            }
        }


        // v128: Helper to find free drive letters securely (Reverse Search Z -> A)
        private string GetFirstFreeDriveLetterChecked(List<string> exclude = null)
        {
            if (exclude == null) exclude = new List<string>();
            exclude.Add("C"); // Always skip C
            exclude.Add("X"); // Always skip X (WinPE Boot)

            // Search Z down to A to avoid common conflicts (G, H, etc.)
            for (char letter = 'Z'; letter >= 'A'; letter--)
            {
                string sLet = letter.ToString();
                if (exclude.Contains(sLet)) continue;

                // Check if truly free
                bool isUsed = false;
                try {
                    DriveInfo[] drives = DriveInfo.GetDrives();
                    foreach (DriveInfo d in drives)
                    {
                        if (d.Name.StartsWith(sLet, StringComparison.OrdinalIgnoreCase))
                        {
                            isUsed = true;
                            break;
                        }
                    }
                } catch {}

                if (!isUsed) return sLet;
            }
            return null;
        }
    }
}
