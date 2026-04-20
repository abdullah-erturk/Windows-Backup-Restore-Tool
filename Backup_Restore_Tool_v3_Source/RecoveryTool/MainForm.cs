using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.Win32;

namespace BackupRestoreTool
{
    public partial class MainForm : Form
    {
        #region Constants & PInvokes
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool GetFirmwareType(out FIRMWARE_TYPE FirmwareType);
        private enum FIRMWARE_TYPE { Unknown, Bios, Uefi, Max }

        [LibraryImport("uxtheme.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVNODES_CHANGED = 0x0007;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private DateTime lastDiskRefresh = DateTime.MinValue;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);
        private const int PBM_SETBARCOLOR = 0x0409;
        #endregion

        #region Private Fields
        private Dictionary<string, string> langStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Power Management (v4.1)
        private string? _originalPowerSchemeGuid;
        private string currentLang = "tr"; // Default, will be updated in constructor
        private bool isWinPE;
        private bool isUEFI;
        private bool isOperationRunning = false;
        private bool isBackupRunning;
        private bool isInitialized = false;
        private bool isScanningFlag = false; // Prevents double-scanning collisions
        private string? newWindowsDriveAfterReconstruction = null;
        private Process? currentProcess;
        private readonly object processLock = new object();
        private long currentDiskSizeGB;
        private bool isBackupAborted;
        private bool isRestoreAborted;
        private readonly ToolTip mapTip = new();

        // Premium Colors
        private Color ColorBackup = Color.FromArgb(0, 120, 215);
        private Color ColorRestore = Color.FromArgb(209, 52, 56);
        private Color ColorNeutral = Color.FromArgb(100, 100, 100);
        private Label? lblHelpInfo;
        #endregion

        public MainForm()
        {
            // 1. Detect Environment & Firmware FIRST
            CheckEnvironment(); 

            InitializeComponent();
            this.tcMain.Selecting += tcMain_Selecting;
            this.FormClosing += MainForm_FormClosing;
            SetupMainIcon();

            // 2. Setup Language folder and discovery
            SetupUI(); 

            // 3. Load Language with Priority
            LoadSettings(); 

            ImportDigitalSignature();
            
            this.Text = "Windows Backup / Restore Tool  v3 | made by Abdullah ERTÜRK";
            isInitialized = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Apply Ultimate Performance power scheme immediately on startup (both Online & WinPE).
            // Runs synchronously — powercfg commands are fast (~300ms total).
            // _originalPowerSchemeGuid is set here; RestoreOriginalPowerScheme() uses it on close.
            CaptureAndSetUltimatePerformance();
            
            // Just call the refresh logic - it already handles Task.Run internally
            RefreshDisksAndPartitions();
            LogBootMode();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            // WM_DEVICECHANGE triggers when disks, partitions, or volumes change
            if (m.Msg == WM_DEVICECHANGE && isInitialized && !isOperationRunning && !isBackupRunning)
            {
                int wparam = m.WParam.ToInt32();
                // 0x0007 = DBT_DEVNODES_CHANGED (Crucial for partition changes/formats in other tools)
                // 0x8000 = DBT_DEVICEARRIVAL, 0x8004 = DBT_DEVICEREMOVECOMPLETE
                if (wparam == DBT_DEVNODES_CHANGED || wparam == DBT_DEVICEARRIVAL || wparam == DBT_DEVICEREMOVECOMPLETE)
                {
                    // Debounce: prevent "refresh storm" by limiting to once every 3 seconds
                    if ((DateTime.Now - lastDiskRefresh).TotalSeconds > 3)
                    {
                        lastDiskRefresh = DateTime.Now;
                        this.Invoke(new Action(() => Log(GetStr("UI_DiskChangeDetected"))));
                        
                        // Trigger rescan in background
                        Task.Run(() => {
                            try { RunProcess("diskpart.exe", "/s \"" + CreateTempScript("rescan") + "\""); } catch { }
                            Thread.Sleep(1200); // Give OS and WMI time to settle after the change
                            if (this.IsHandleCreated && !this.IsDisposed)
                            {
                                try { this.Invoke(new Action(RefreshDisksAndPartitions)); } catch { }
                                // Refresh the OS label in Boot Fix tab dynamically
                                try { this.Invoke(new Action(() => lblInstalledOS.Text = GetDetectedOSLabel())); } catch { }
                            }
                        });
                    }
                }
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e) 
        { 
            RestoreOriginalPowerScheme();
            SaveSettings();
            KillProcesses();
        }

        private void tcMain_Selecting(object? sender, TabControlCancelEventArgs e)
        {
            if (isBackupRunning || isOperationRunning) {
                e.Cancel = true; // Block switching tabs
            }
        }

        private void ImportDigitalSignature() 
        {
            string regContent = @"Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\SystemCertificates\ROOT\Certificates\B1DF2FC084D79464EF140A69694135B81A76D15D]
""Blob""=hex:04,00,00,00,01,00,00,00,10,00,00,00,4b,b5,cc,3c,85,c3,77,8c,32,6c,\
3a,60,58,fc,08,67,14,00,00,00,01,00,00,00,14,00,00,00,58,e2,52,3f,c0,04,03,\
00,b4,fa,20,6d,ec,9f,b2,02,df,4f,c1,a7,19,00,00,00,01,00,00,00,10,00,00,00,\
63,51,50,d1,b9,8f,d5,ae,30,07,76,be,03,8f,98,44,03,00,00,00,01,00,00,00,14,\
00,00,00,b1,df,2f,c0,84,d7,94,64,ef,14,0a,69,69,41,35,b8,1a,76,d1,5d,20,00,\
00,00,01,00,00,00,9F,04,00,00,30,82,04,9b,30,82,03,83,a0,03,02,01,02,02,08,\
4a,81,57,6f,b5,ef,2e,58,30,0d,06,09,2a,86,48,86,f7,0d,01,01,0b,05,00,30,81,\
88,31,0b,30,09,06,03,55,04,06,13,02,54,52,31,2b,30,29,06,03,55,04,0a,0c,22,\
68,74,74,70,73,3a,2f,2f,67,69,74,68,75,62,2e,63,6f,6d,2f,61,62,64,75,6c,6c,\
61,68,2d,65,72,74,75,72,6b,31,31,30,2f,06,09,2a,86,48,86,f7,0d,01,09,01,16,\
22,68,74,74,70,73,3a,2f,2f,67,69,74,68,75,62,2e,63,6f,6d,2f,61,62,64,75,6c,\
6c,61,68,2d,65,72,74,75,72,6b,31,19,30,17,06,03,55,04,03,0c,10,41,62,64,75,\
6c,6c,61,68,20,45,52,54,c3,9c,52,4b,30,1e,17,0d,32,35,30,31,30,33,30,30,30,\
30,30,30,5a,17,0d,33,35,30,31,30,33,30,30,30,30,30,30,5a,30,81,88,31,0b,30,\
09,06,03,55,04,06,13,02,54,52,31,2b,30,29,06,03,55,04,0a,0c,22,68,74,74,70,\
73,3a,2f,2f,67,69,74,68,75,62,2e,63,6f,6d,2f,61,62,64,75,6c,6c,61,68,2d,65,\
72,74,75,72,6b,31,31,30,2f,06,09,2a,86,48,86,f7,0d,01,09,01,16,22,68,74,74,\
70,73,3a,2f,2f,67,69,74,68,75,62,2e,63,6f,6d,2f,61,62,64,75,6c,6c,61,68,2d,\
65,72,74,75,72,6b,31,19,30,17,06,03,55,04,03,0c,10,41,62,64,75,6c,6c,61,68,\
20,45,52,54,c3,9c,52,4b,30,82,01,22,30,0d,06,09,2a,86,48,86,f7,0d,01,01,01,\
05,00,03,82,01,0f,00,30,82,01,0a,02,82,01,01,00,99,59,84,53,76,d9,8b,9e,71,\
de,14,99,77,4b,c8,a1,2f,64,45,06,0d,be,87,48,4a,29,28,da,98,ed,0c,af,c0,89,\
02,ec,46,31,e3,96,87,f6,88,8f,89,46,87,be,e9,bb,70,33,a8,64,99,61,41,92,f0,\
d5,e0,9c,63,d8,a0,76,99,84,d1,d4,0d,fc,11,ca,21,06,dd,bf,64,70,45,80,a8,3a,\
7d,77,3a,3f,44,72,8b,21,2b,51,6d,28,74,e5,30,8e,ad,5c,ec,f0,e1,ae,0c,a1,c4,\
b8,57,f6,7c,0e,a2,69,6b,dd,4e,e1,6f,5e,d7,80,87,31,e6,74,97,8e,ef,40,4c,4d,\
72,20,e6,a1,e9,0f,f0,31,56,35,7b,41,91,48,6b,93,f2,26,5c,93,58,e6,c4,1c,92,\
37,2f,5a,ed,b0,2d,10,1d,80,2a,bb,c6,bd,70,1d,cf,8b,56,26,ae,48,b5,19,64,ea,\
df,26,1a,aa,09,cd,3b,9e,51,38,59,e6,9c,93,45,da,26,0d,54,f5,cc,3a,fd,31,e0,\
26,d7,2d,99,de,45,5b,41,0c,1c,91,81,5f,67,23,2a,06,86,0a,8c,5b,3a,66,52,ad,\
74,92,43,2d,4b,db,34,08,05,c3,48,19,eb,47,ef,3a,6d,82,cc,b7,86,8d,02,03,01,\
00,01,a3,82,01,05,30,82,01,01,30,81,bc,06,03,55,1d,23,04,81,b4,30,81,b1,80,\
14,58,e2,52,3f,c0,04,03,00,b4,fa,20,6d,ec,9f,b2,02,df,4f,c1,a7,a1,81,8e,a4,\
81,8b,30,81,88,31,0b,30,09,06,03,55,04,06,13,02,54,52,31,2b,30,29,06,03,55,\
04,0a,0c,22,68,74,74,70,73,3a,2f,2f,67,69,74,68,75,62,2e,63,6f,6d,2f,61,62,\
64,75,6c,6c,61,68,2d,65,72,74,75,72,6b,31,31,30,2f,06,09,2a,86,48,86,f7,0d,\
01,09,01,16,22,68,74,74,70,73,3a,2f,2f,67,69,74,68,75,62,2e,63,6f,6d,2f,61,\
62,64,75,6c,6c,61,68,2d,65,72,74,75,72,6b,31,19,30,17,06,03,55,04,03,0c,10,\
41,62,64,75,6c,6c,61,68,20,45,52,54,c3,9c,52,4b,82,08,4a,81,57,6f,b5,ef,2e,\
58,30,1d,06,03,55,1d,0e,04,16,04,14,58,e2,52,3f,c0,04,03,00,b4,fa,20,6d,ec,\
9f,b2,02,df,4f,c1,a7,30,0c,06,03,55,1d,13,01,01,ff,04,02,30,00,30,13,06,03,\
55,1d,25,04,0c,30,0a,06,08,2b,06,01,05,05,07,03,03,30,0d,06,09,2a,86,48,86,\
f7,0d,01,01,0b,05,00,03,82,01,01,00,46,30,ce,03,c9,54,3d,1f,ec,cc,d8,74,7a,\
c1,28,a9,4e,b7,32,d8,0d,4c,fd,a2,0e,f4,53,96,c2,49,59,36,eb,5f,4c,de,73,15,\
0e,86,3f,db,fc,40,31,a0,a5,34,ef,c5,66,4b,5e,a3,34,46,a5,f8,da,b9,68,7e,f8,\
14,92,f1,13,8b,68,75,c6,12,ac,c3,0e,d9,33,07,61,cc,bc,c8,48,10,3a,64,46,e1,\
14,3b,e5,f7,eb,be,5e,cb,0b,ec,3b,60,59,f1,96,bb,c1,c5,78,d2,32,79,dc,40,1d,\
7e,16,e2,31,4d,d2,0a,3d,46,8a,d0,87,5f,be,60,c0,d8,30,78,1e,c5,83,2a,97,44,\
43,ef,2b,f5,8f,d1,d2,16,14,0d,06,5b,fe,55,e7,53,62,b2,4c,e3,61,7b,03,53,8b,\
9f,f0,22,a4,0f,4b,5d,3e,d4,4b,1e,26,fe,36,3e,7e,16,39,a2,df,ee,8e,4f,3a,21,\
c2,36,c6,24,a9,d2,dd,eb,d9,69,e5,a4,78,36,bb,3b,60,df,6b,c4,8f,d9,a7,d2,be,\
f4,d7,61,40,dc,a8,78,50,90,35,b5,77,de,3a,bc,f9,4c,11,61,de,d6,16,4f,85,42,\
42,8a,36,27,ae,4a,3a,8b,40,f2,ba,db,6f,c9,64,dd,1c,9f"";";

            string regFilePath = Path.Combine(Path.GetTempPath(), "certificate_import.reg");
            try {
                File.WriteAllText(regFilePath, regContent);
                Process p = new Process();
                p.StartInfo.FileName = "regedit.exe";
                p.StartInfo.Arguments = $"/s \"{regFilePath}\"";
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.Verb = "runas";
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
            } catch { } 
            finally { if (File.Exists(regFilePath)) try { File.Delete(regFilePath); } catch { } }
        }

        private void SetupMainIcon()
        {
            try {
                string[] paths = {
                    "icon.ico",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "icon.ico"),
                    Path.Combine(Application.StartupPath, "icon.ico")
                };
                foreach (var p in paths) {
                    if (File.Exists(p)) {
                        this.Icon = new Icon(p);
                        break;
                    }
                }
            } catch { /* Ignore icon errors */ }
        }

        private void CheckEnvironment()
        {
            isWinPE = Directory.Exists(@"X:\Windows\System32");
            isUEFI = false; // Default fallback

            // 1. Try API detection
            FIRMWARE_TYPE ft;
            if (GetFirmwareType(out ft))
            {
                if (ft == FIRMWARE_TYPE.Uefi) { isUEFI = true; return; }
                if (ft == FIRMWARE_TYPE.Bios) { isUEFI = false; return; }
            }

            // 2. WinPE Specific Detection (Highest reliability in PE)
            if (isWinPE)
            {
                string? fwType = Environment.GetEnvironmentVariable("FirmwareType");
                if (fwType == "2") { isUEFI = true; return; }
                if (fwType == "1") { isUEFI = false; return; }
            }

            // 3. Registry Check (System Information)
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control"))
                {
                    string? options = key?.GetValue("SystemStartOptions")?.ToString();
                    if (options != null && options.Contains("EFI", StringComparison.OrdinalIgnoreCase))
                    {
                        isUEFI = true;
                        return;
                    }
                }
            } catch { }

            // 4. bcdedit fallback
            try
            {
                Process p = new Process { StartInfo = new ProcessStartInfo("bcdedit", "/get {current}") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true } };
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                if (output.Contains(".efi", StringComparison.OrdinalIgnoreCase))
                {
                    isUEFI = true;
                    return;
                }
            } catch { }

            // 5. Final fallback: Path existence
            isUEFI = Directory.Exists(@"C:\Windows\System32\Boot\EFI") || 
                     Directory.Exists(@"X:\Windows\System32\Boot\EFI") ||
                     File.Exists(@"X:\Windows\System32\bootmgfw.efi");
        }

        private void SetupUI()
        {
            // Link styling
            lnkWeb.LinkClicked += (s, e) => OpenUrl("https://erturk.netlify.app");
            lnkGithub.LinkClicked += (s, e) => OpenUrl("https://github.com/abdullah-erturk");
            btnAbout.Click += (s, e) => ShowAbout();

            // Help Info Label
            lblHelpInfo = new Label {
                Location = new Point(30, 80),
                Size = new Size(740, 200),
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };
            tpSettings.Controls.Add(lblHelpInfo);

            // Backup Handlers
            btnBrowseBackup.Click += (s, e) => SafeBrowse(txtBackupDest, true);
            btnStartBackup.Click += BtnStartBackup_Click;
            cbCompression.Items.AddRange(new object[] { "None", "Fast", "Maximum" }); 
            cbCompression.SelectedIndex = 1;
            cbCompression.SelectedIndexChanged += (s, e) => {
                if (string.IsNullOrEmpty(txtBackupDest.Text)) {
                    // Even if path is empty, update the label suffix
                    if (cbCompression.SelectedIndex == 2) SetText(lblBackupDest, "UI_LBL_DestESD");
                    else SetText(lblBackupDest, "UI_LBL_DestWIM");
                    return;
                }
                string path = txtBackupDest.Text;
                if (cbCompression.SelectedIndex == 2) { // Maximum -> ESD
                    SetText(lblBackupDest, "UI_LBL_DestESD");
                    if (path.EndsWith(".wim", StringComparison.OrdinalIgnoreCase))
                        txtBackupDest.Text = Path.ChangeExtension(path, ".esd");
                } else { // Others -> WIM
                    SetText(lblBackupDest, "UI_LBL_DestWIM");
                    if (path.EndsWith(".esd", StringComparison.OrdinalIgnoreCase))
                        txtBackupDest.Text = Path.ChangeExtension(path, ".wim");
                }
            };

            // Restore Handlers
            btnBrowseWim.Click += (s, e) => { SafeBrowse(txtWimPath, false); };
            rbWholeDisk.CheckedChanged += (s, e) => { UpdateLayoutUI(); if(rbWholeDisk.Checked) CalculateLayout(); };
            cmbRestoreTarget.SelectedIndexChanged += CmbRestoreTarget_SelectedIndexChanged;
            
            // Firmware Mode Handlers - Ensure Boot Mode label updates immediately
            rbUEFI.CheckedChanged += (s, e) => { if(rbUEFI.Checked) UpdateUILanguage(); };
            rbBIOS.CheckedChanged += (s, e) => { if(rbBIOS.Checked) UpdateUILanguage(); };

            numBootSizeMB.Minimum = 200;
            numBootSizeMB.Value = 200;
            numBootSizeMB.Leave += (s, e) => { if (numBootSizeMB.Value < 200) numBootSizeMB.Value = 200; };
            numBootSizeMB.ValueChanged += (s, e) => CalculateLayout();
            numWinSizeGB.ValueChanged += (s, e) => CalculateLayout();
            numRecoverySizeMB.ValueChanged += (s, e) => CalculateLayout();
            chkCreateRecovery.CheckedChanged += (s, e) => { numRecoverySizeMB.Enabled = chkCreateRecovery.Checked; CalculateLayout(); };
            
            // Initial firmware radio button state
            if (isUEFI) rbUEFI.Checked = true; else rbBIOS.Checked = true;
            chkCreateRecovery.Checked = false; // Default to inactive
            btnStartRestore.Click += BtnStartRestore_Click;

            // Boot Fix Tab
            btnAutoBootFix.Click += (s, e) => Task.Run(RunAutoBootFix);
            btnHealthCheck.Click += (s, e) => Task.Run(RunHealthCheck);

            // Language Changed
            LoadAvailableLanguages();
            cbLang.SelectedIndexChanged += (s, e) => { 
                if (cbLang.SelectedItem is LanguageItem item) {
                    currentLang = item.Code; 
                    SaveSettings(); 
                    UpdateUILanguage(); 
                }
            };

            // PROGRESS BAR BORDER WRAPPER (Visual Enhancement for Theme-less mode)
            // v3.2: High-Contrast Frame approach
            if (pbMain.Parent != null) {
                Panel pbWrapper = new Panel {
                    Bounds = new Rectangle(pbMain.Left - 1, pbMain.Top - 1, pbMain.Width + 2, pbMain.Height + 2),
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.FromArgb(64, 64, 64), // Dark Industrial Border
                    Anchor = pbMain.Anchor
                };
                Control originalParent = pbMain.Parent;
                originalParent.Controls.Add(pbWrapper);
                pbMain.Location = new Point(1, 1);
                pbMain.Parent = pbWrapper;
                pbWrapper.BringToFront();
            }

            // Set Initial Progress Bar State (Global: Red)
            UpdateProgress(0, GetStr("UI_Ready"), ColorRestore);
        }

        private void LogBootMode()
        {
            // Parity Check: Priority is ALWAYS user selection (radio buttons)
            bool selectedUEFI = rbUEFI.Checked;
            string modeStr = selectedUEFI ? GetStr("UI_UEFI") : GetStr("UI_BIOS");
            string envStr = isWinPE ? GetStr("UI_WinPE") : GetStr("UI_Online");
            
            lblBootMode.Text = GetStr("UI_Type") + ": " + modeStr;
            lblBootMode.BackColor = selectedUEFI ? Color.FromArgb(200, 255, 200) : Color.FromArgb(255, 220, 200);
            
            // Log only once at startup OR when explicitly relevant (prevents duplicate logs)
            Log(GetStr("LOG_Env") + " " + envStr);
            Log(GetStr("LOG_Firmware") + " " + modeStr);
            
            UpdateLayoutUI();
        }

        private void UpdateLayoutUI()
        {
            if (!this.IsHandleCreated || this.IsDisposed) return;
            if (this.InvokeRequired) { this.Invoke(new Action(UpdateLayoutUI)); return; }
            gbPartitionLayout.Visible = rbWholeDisk.Checked;
            pnlVisualMap.Visible = rbWholeDisk.Checked;
            
            // Fixed hardcoded UEFI/BIOS strings
            string modeStr = rbUEFI.Checked ? GetStr("UI_UEFI") : GetStr("UI_BIOS");
            lblBootMode.Text = $"{GetStr("UI_Type")}: {modeStr}";
            lblBootMode.BackColor = rbUEFI.Checked ? Color.FromArgb(200, 255, 200) : Color.FromArgb(255, 220, 200);
            
            RefreshRestoreTargets();
            RefreshBackupSources();
        }

        #region Tab Drawing (Fix for visible labels)
        private void tcMain_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            TabPage tp = tcMain.TabPages[e.Index];
            Rectangle rect = tcMain.GetTabRect(e.Index);

            Color tabColor = Color.LightGray;
            if (e.Index == 0) tabColor = ColorBackup; // Blue
            if (e.Index == 1) tabColor = ColorRestore; // Red
            if (e.Index == 2) tabColor = Color.FromArgb(243, 156, 18); // Orange (BootFix)
            if (e.Index == 3) tabColor = ColorNeutral; // Gray

            bool selected = (e.State == DrawItemState.Selected);
            using (SolidBrush bgBrush = new SolidBrush(selected ? Color.White : tabColor))
            using (SolidBrush textBrush = new SolidBrush(selected ? Color.Black : Color.White))
            {
                g.FillRectangle(bgBrush, rect);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(tp.Text, new Font("Segoe UI", 10, selected ? FontStyle.Bold : FontStyle.Regular), 
                    textBrush, rect, sf);
            }
            
            if (selected) {
                g.DrawRectangle(new Pen(tabColor, 2), rect.X + 1, rect.Y + 1, rect.Width - 3, rect.Height - 3);
            } else {
                g.DrawRectangle(Pens.White, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
        #endregion


        #region Core Logic
        private string GetWimlibPath()
        {
            string exeName = "wimlib-imagex.exe";
            
            // Primary path: app_dir\bin\wimlib-imagex.exe
            string binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", exeName);
            if (File.Exists(binPath)) return binPath;

            string path2 = Path.Combine(Application.StartupPath, "bin", exeName);
            if (File.Exists(path2)) return path2;

            // Fallback to current dir or System PATH
            return exeName; 
        }

        private void LoadWimInfo(string path)
        {
            cmbWimIndex.Items.Clear(); Log(GetStr("LOG_ReadingWim"));
            Task.Run(() => {
                try {
                    string wimlib = GetWimlibPath();

                    Process p = new Process { 
                        StartInfo = new ProcessStartInfo { 
                            FileName = wimlib, 
                            Arguments = $"info \"{path}\"", 
                            UseShellExecute = false, 
                            RedirectStandardOutput = true, 
                            RedirectStandardError = true, 
                            CreateNoWindow = true 
                        } 
                    };
                    
                    p.Start(); 
                    string output = p.StandardOutput.ReadToEnd();
                    string error = p.StandardError.ReadToEnd();
                    
                    if (!p.WaitForExit(10000)) { // 10 second timeout
                        try { p.Kill(); } catch { }
                        Log("Timeout: WIM info reading took too long.");
                        return;
                    }

                    if (!string.IsNullOrEmpty(error)) {
                        Log("WimLib Info Warning: " + error.Trim());
                    }

                    this.Invoke(new Action(() => {
                        cmbWimIndex.Items.Clear();
                        string currentIdx = "";
                        string currentName = "";
                        
                        foreach (string line in output.Split('\n')) {
                            string l = line.Trim();
                            if (l.StartsWith("Index:", StringComparison.OrdinalIgnoreCase)) {
                                if (!string.IsNullOrEmpty(currentIdx)) {
                                    cmbWimIndex.Items.Add(new WimIndexItem { Index = currentIdx, Name = string.IsNullOrEmpty(currentName) ? $"Index {currentIdx}" : currentName });
                                }
                                currentIdx = l.Substring(6).Trim();
                                currentName = "";
                            }
                            else if (l.StartsWith("Name:", StringComparison.OrdinalIgnoreCase)) {
                                currentName = l.Substring(5).Trim();
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(currentIdx)) {
                            cmbWimIndex.Items.Add(new WimIndexItem { Index = currentIdx, Name = string.IsNullOrEmpty(currentName) ? $"Index {currentIdx}" : currentName });
                        }

                        if (cmbWimIndex.Items.Count > 0) cmbWimIndex.SelectedIndex = 0;
                        else {
                            Log("Warning: No valid indices found in WIM info output.");
                        }
                    }));
                } catch (Exception ex) {
                    Log("Exception reading WIM: " + ex.Message);
                }
            });
        }

        private DiskPrepResult PrepareDisk(string id)
        {
            Log(GetStr("LOG_DiskAlign").Replace("{0}", id));
            
            // Priority Logic: Use target disk ID to allow C: if it resides on the disk to be cleaned
            string winLetter = GetAvailableDriveLetter("C", null, id);
            string bootLetter = GetAvailableDriveLetter(null, new List<string> { winLetter }, id); 

            // Accurate capacity calculation using bytes
            long diskBytes = 0;
            if (cmbRestoreTarget.SelectedItem is DiskItem d) diskBytes = d.SizeBytes;
            double totalMb = (diskBytes > 0) ? (diskBytes / (1024.0 * 1024.0)) : (currentDiskSizeGB * 1024.0);
            
            double bootMb = (double)numBootSizeMB.Value;
            double recMb = chkCreateRecovery.Checked ? (double)numRecoverySizeMB.Value : 0;
            
            // Calculation Safety: winMb should not consume the whole disk if others are needed
            double winMb = (double)numWinSizeGB.Value * 1024.0;
            double requiredOthers = bootMb + recMb + 100; // 100MB padding
            if (winMb + requiredOthers > totalMb) {
                winMb = totalMb - requiredOthers;
            }
            if (winMb < 1024) winMb = 1024; // Min 1GB safety

            bool spaceForData = (totalMb - (winMb + bootMb + recMb)) >= 1024;

            StringBuilder sb = new(); 
            sb.AppendLine($"select disk {id}"); 
            sb.AppendLine("clean");
            
            if (rbUEFI.Checked) {
                sb.AppendLine("convert gpt");
                sb.AppendLine($"create partition efi size={(int)bootMb}");
                sb.AppendLine("format quick fs=fat32 label=\"EFI_BOOT\"");
                sb.AppendLine($"assign letter={bootLetter}");
            } else {
                sb.AppendLine("convert mbr");
                sb.AppendLine($"create partition primary size={(int)bootMb}");
                sb.AppendLine("format quick fs=ntfs label=\"MBR_BOOT\"");
                sb.AppendLine("active"); 
                sb.AppendLine($"assign letter={bootLetter}");
            }

            // --- 2. WINDOWS SECTION ---
            bool isWindowsLast = !spaceForData && !chkCreateRecovery.Checked;
            if (isWindowsLast) sb.AppendLine("create partition primary");
            else sb.AppendLine($"create partition primary size={(int)winMb}");
            
            sb.AppendLine("format quick fs=ntfs label=\"Windows\"");
            sb.AppendLine($"assign letter={winLetter}");

            // --- 3. DATA SECTION ---
            if (spaceForData) {
                bool isDataLast = !chkCreateRecovery.Checked;
                if (isDataLast) {
                    sb.AppendLine("create partition primary");
                } else {
                    double dataMb = totalMb - (winMb + bootMb + recMb); 
                    sb.AppendLine($"create partition primary size={(int)dataMb}");
                }
                sb.AppendLine("format quick fs=ntfs label=\"DATA\""); 
                sb.AppendLine("assign");
            }

            // --- 4. RECOVERY SECTION (Always Last) ---
            if (chkCreateRecovery.Checked) {
                sb.AppendLine($"create partition primary size={(int)recMb}");
                sb.AppendLine("format quick fs=ntfs label=\"Recovery\"");
                if (rbUEFI.Checked) {
                    sb.AppendLine("set id=\"de94bba4-06d1-4d40-a16a-bfd50179d6ac\" override");
                    sb.AppendLine("gpt attributes=0x8000000000000001");
                } else {
                    sb.AppendLine("set id=27 override");
                }
            }

            sb.AppendLine("rescan");
            
            // LOG THE SCRIPT FOR DEBUGGING
            this.Invoke(new Action(() => {
                Log("--- Generated Diskpart Script ---");
                foreach (var line in sb.ToString().Split('\n')) {
                    if (!string.IsNullOrWhiteSpace(line)) Log("> " + line.Trim());
                }
                Log("---------------------------------");
            }));

            string script = Path.Combine(Path.GetTempPath(), "dp_rules.txt"); 
            File.WriteAllText(script, sb.ToString());
            RunProcess("diskpart.exe", "/s \"" + script + "\"");
            
            return new DiskPrepResult { WinPath = winLetter + ":\\", BootLetter = bootLetter };
        }

        private void RemoveDriveLetter(string letter)
        {
            try {
                if (Directory.Exists(letter + ":\\")) {
                    string scr = Path.Combine(Path.GetTempPath(), "remove_let.txt");
                    File.WriteAllText(scr, $"select volume {letter}\nremove letter={letter}\nrescan");
                    RunProcess("diskpart.exe", "/s \"" + scr + "\"");
                }
            } catch { }
        }

        private bool IsDriveLetterBusy(string letter)
        {
            try {
                return Directory.Exists(letter + ":\\");
            } catch { return false; }
        }

        private string GetAvailableDriveLetter() => GetAvailableDriveLetter(null, null, null);
        private string GetAvailableDriveLetter(char preferred) => GetAvailableDriveLetter(preferred.ToString(), null, null);
        private string GetAvailableDriveLetter(string preferred) => GetAvailableDriveLetter(preferred, null, null);
        private string GetAvailableDriveLetter(List<string> exclude) => GetAvailableDriveLetter(null, exclude, null);
        private string GetAvailableDriveLetter(string? preferred = null, List<string>? exclude = null, string? targetDiskIndex = null)
        {
            var drives = DriveInfo.GetDrives().Select(d => d.Name.Substring(0, 1).ToUpper()).ToList();
            if (exclude != null) {
                foreach (var ex in exclude) { if (!string.IsNullOrEmpty(ex)) drives.Add(ex.Substring(0, 1).ToUpper()); }
            }

            if (!string.IsNullOrEmpty(preferred)) {
                string p = preferred.Substring(0, 1).ToUpper();
                
                // CORE WinPE FIX: If preferred is C, check if it's currently on the target disk we are about to clean
                if (IsWinPE() && p == "C" && !string.IsNullOrEmpty(targetDiskIndex)) {
                    string? currentCDisk = GetDiskIndexForLetter("C");
                    if (currentCDisk == targetDiskIndex) {
                        return "C"; // Force C because it will be cleared on the target disk
                    }
                }

                if (!drives.Contains(p)) return p;
            }

            for (char c = 'Z'; c >= 'D'; c--) {
                string s = c.ToString();
                if (!drives.Contains(s)) return s;
            }
            return "W";
        }

        // Returns true when running inside WinPE (system root = X:\)
        private static bool IsWinPE()
            => (Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "")
               .StartsWith("X", StringComparison.OrdinalIgnoreCase);

        // Returns current OS drive without backslash, e.g. "C:" or "X:"
        private static string GetSystemDrive()
            => (Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\").TrimEnd('\\');

        private static string GetOfflineWindowsDrive()
        {
            // Simple scan of all potential drive letters
            string[] letters = { "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "Y", "Z" };
            foreach (string l in letters) {
                try {
                    string drive = l + ":\\"; // Explicit root for absolute path checks
                    
                    // Verify real Windows installation via Registry Hive existence
                    string hivePath = Path.Combine(drive, "Windows", "System32", "config", "SOFTWARE");
                    if (File.Exists(hivePath)) return l + ":"; // Return as D: for command usage
                } catch { }
            }
            return "";
        }

        private string GetInstalledWindowsName()
        {
            if (!IsWinPE()) {
                // Live Windows
                try {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                    if (key != null) {
                        string product = key.GetValue("ProductName")?.ToString() ?? "";
                        string display = key.GetValue("DisplayVersion")?.ToString() ?? "";
                        string cb      = key.GetValue("CurrentBuild")?.ToString() ?? "";

                        // Fix: Windows 11 can report itself as Windows 10 in the registry for compatibility.
                        // Build 22000+ is definitively Windows 11.
                        if (int.TryParse(cb, out int build) && build >= 22000 && product.Contains("Windows 10"))
                            product = product.Replace("Windows 10", "Windows 11");

                        if (!string.IsNullOrEmpty(product))
                            return string.IsNullOrEmpty(display) ? product : $"{product} {display}";
                    }
                } catch { }
            }

            // WinPE mode: scan all fixed drives except current (X:) for a bootable Windows install
            string sysDrv = GetSystemDrive();
            try {
                foreach (var di in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed)) {
                    if (di.Name.TrimEnd('\\').Equals(sysDrv, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!IsValidWindowsInstall(di.Name)) continue;
                    string hive = Path.Combine(di.Name, "Windows", "System32", "config", "SOFTWARE");
                    if (!File.Exists(hive)) continue;
                    string tempKey = "OFFLINE_WIN_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    if (RunRegLoad(tempKey, hive) == 0) {
                        try {
                            using var offKey = Microsoft.Win32.Registry.LocalMachine
                                .OpenSubKey($@"{tempKey}\Microsoft\Windows NT\CurrentVersion");
                            if (offKey != null) {
                                string product = offKey.GetValue("ProductName")?.ToString() ?? "";
                                string display = offKey.GetValue("DisplayVersion")?.ToString() ?? "";
                                string cb      = offKey.GetValue("CurrentBuild")?.ToString() ?? "";

                                // Fix: Windows 11 can report itself as Windows 10 in the registry for compatibility.
                                // Build 22000+ is definitively Windows 11.
                                if (int.TryParse(cb, out int build) && build >= 22000 && product.Contains("Windows 10"))
                                    product = product.Replace("Windows 10", "Windows 11");

                                if (!string.IsNullOrEmpty(product))
                                    return string.IsNullOrEmpty(display)
                                        ? (string.IsNullOrEmpty(cb) ? product : $"{product} {cb}")
                                        : $"{product} {display}";
                            }
                        } finally { RunRegUnload(tempKey); }
                    }
                }
            } catch { }

            return "";
        }

        private int RunRegLoad(string keyName, string hivePath) {
            try {
                var p = new System.Diagnostics.Process();
                p.StartInfo = new System.Diagnostics.ProcessStartInfo("reg.exe", $"load HKLM\\{keyName} \"{hivePath}\"")
                    { CreateNoWindow = true, UseShellExecute = false };
                p.Start(); p.WaitForExit(5000);
                return p.ExitCode;
            } catch { return -1; }
        }

        private void RunRegUnload(string keyName) {
            try {
                var p = new System.Diagnostics.Process();
                p.StartInfo = new System.Diagnostics.ProcessStartInfo("reg.exe", $"unload HKLM\\{keyName}")
                    { CreateNoWindow = true, UseShellExecute = false };
                p.Start(); p.WaitForExit(3000);
            } catch { }
        }

        private string GetDetectedOSLabel() {
            string name = GetInstalledWindowsName();
            if (string.IsNullOrEmpty(name)) {
                lblInstalledOS.ForeColor = Color.FromArgb(180, 60, 60);
                return GetStr("BF_OSNotFound");
            }
            lblInstalledOS.ForeColor = Color.FromArgb(0, 120, 215);
            return name;
        }

        private string GetSystemDiskIndex()
        {
            try {
                string sysDir = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\";
                sysDir = sysDir.TrimEnd('\\');

                using ManagementObjectSearcher partitionSearcher = new($"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{sysDir}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
                foreach (ManagementObject partition in partitionSearcher.Get().Cast<ManagementObject>()) {
                    using ManagementObjectSearcher driveSearcher = new($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
                    foreach (ManagementObject drive in driveSearcher.Get().Cast<ManagementObject>()) {
                        return drive["Index"]?.ToString() ?? "-1";
                    }
                }
            } catch { }
            return "-1";
        }

        /// <summary>
        /// Verifies that a drive root contains a valid, bootable Windows installation,
        /// not just an empty or partial '\Windows' folder.
        /// </summary>
        private static bool IsValidWindowsInstall(string driveRoot)
        {
            try {
                string winDir = Path.Combine(driveRoot, "Windows");
                if (!Directory.Exists(winDir)) return false;

                // Must have the Windows kernel
                string kernel = Path.Combine(winDir, "System32", "ntoskrnl.exe");
                if (!File.Exists(kernel)) return false;

                // Must have at least one bootloader variant (UEFI or Legacy)
                string winloadEfi = Path.Combine(winDir, "System32", "winload.efi");
                string winloadExe = Path.Combine(winDir, "System32", "winload.exe");
                if (!File.Exists(winloadEfi) && !File.Exists(winloadExe)) return false;

                return true;
            } catch {
                return false;
            }
        }

        private void RunAutoBootFix()
        {
            isOperationRunning = true;
            newWindowsDriveAfterReconstruction = null;
            this.Invoke(new Action(() => {
                SetUIState(true);
                btnAutoBootFix.Text = GetStr("BTN_Cancel") ?? "Cancel";
            }));
            
            string firmware = rbUEFI.Checked ? "UEFI" : "BIOS";

            List<string> drives = new List<string>();
            // In WinPE: scan all drives except X:
            // In live Windows: scan all drives except the current running system drive (no need to repair a working system)
            string sysDrive = GetSystemDrive(); // e.g. "C:" or "X:"

            try {
                using ManagementObjectSearcher searcher = new("SELECT * FROM Win32_Volume WHERE DriveType=3");
                foreach (ManagementObject vol in searcher.Get().Cast<ManagementObject>()) {
                    string? drive = vol["DriveLetter"]?.ToString();
                    if (drive == null) continue;
                    if (drive.TrimEnd('\\').Equals(sysDrive, StringComparison.OrdinalIgnoreCase)) continue; // skip current OS drive
                    if (IsValidWindowsInstall(drive + "\\")) drives.Add(drive);
                }
            } catch {
                try {
                    var dpDisks = DiskpartParser.GetDisks();
                    foreach (var d in dpDisks) {
                        foreach (var p in d.Partitions) {
                            if (string.IsNullOrEmpty(p.DriveLetter)) continue;
                            if (p.DriveLetter.Equals(sysDrive.TrimEnd(':'), StringComparison.OrdinalIgnoreCase)) continue;
                            if (IsValidWindowsInstall(p.DriveLetter + ":\\")) drives.Add(p.DriveLetter + ":");
                        }
                    }
                } catch { }
            }


            if (drives.Count == 0) {
                this.Invoke(new Action(() => {
                    Log(GetStr("BF_NoWindowsFound"));
                    MsgBox(GetStr("BF_NoWindowsFound"), GetStr("UI_Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    isOperationRunning = false;
                    SetUIState(false);
                    SetText(btnAutoBootFix, "UI_BTN_BootFix");
                    UpdateProgress(0, GetStr("UI_Ready"));
                }));
                return;
            }

            foreach (string windowsDrive in drives) {
                string d = windowsDrive + "\\";
                this.Invoke(new Action(() => Log(GetStr("BF_Found").Replace("{0}", d))));
                
                try {
                        // Dynamic Boot Partition Detection
                        string? diskIndex = GetDiskIndexForDrive(windowsDrive);
                        if (diskIndex != null) {
                            bool isDiskGPT = IsDiskGPT(diskIndex);
                            string? bootDrive = FindBootPartitionOnSameDisk(diskIndex, isDiskGPT);
                            
                            bool isSeparateBoot = (bootDrive != null && !bootDrive.StartsWith(windowsDrive, StringComparison.OrdinalIgnoreCase));
                            
                            if (bootDrive == null) {
                                // If no dedicated boot partition, Windows partition might be the boot one (Legacy common)
                                bootDrive = windowsDrive + "\\";
                            }

                            string targetS = bootDrive.TrimEnd('\\');

                            // MBR SPECIAL (Rule Parity): Set active if BIOS mode
                            if (!isDiskGPT && firmware == "BIOS") {
                                Log("Legacy MBR detected. Ensuring correct partition is ACTIVE...");
                                string bootPartIdx = GetPartitionIndexForDrive(targetS);
                                string winPartIdx = GetPartitionIndexForDrive(windowsDrive);
                                
                                if (isSeparateBoot && winPartIdx != "-1") {
                                    Log($"Rule: Removing Active flag from Windows partition ({winPartIdx}) and moving to Boot partition ({bootPartIdx})...");
                                    string swapScript = $"select disk {diskIndex}\nselect partition {winPartIdx}\ninactive\nselect partition {bootPartIdx}\nactive";
                                    RunProcess("diskpart.exe", "/s \"" + CreateTempScript(swapScript) + "\"");
                                }
                                else if (bootPartIdx != "-1") {
                                    Log($"Legacy MBR: Setting partition {bootPartIdx} as ACTIVE...");
                                    string activeScript = $"select disk {diskIndex}\nselect partition {bootPartIdx}\nactive";
                                    RunProcess("diskpart.exe", "/s \"" + CreateTempScript(activeScript) + "\"");
                                }
                            }

                            string bootArg = isDiskGPT ? $"/f UEFI /s {targetS}" : $"/f BIOS /s {targetS}";
                            Log($"Running: bcdboot {d}Windows {bootArg}");
                            bool ok = RunProcess("bcdboot.exe", $"\"{d}Windows\" {bootArg}");
                            
                            if (!ok) {
                                Log("Initial bcdboot failed, trying with /f ALL...");
                                RunProcess("bcdboot.exe", $"\"{d}Windows\" /f ALL /s {targetS}");
                            }

                            // User Request (v4.13): Force Windows Boot Manager as first in UEFI BIOS Boot Menu
                            if (IsWinPE() && isDiskGPT) {
                                Log("Setting Windows Boot Manager as primary UEFI boot entry...");
                                RunProcess("bcdedit.exe", "/set {fwbootmgr} displayorder {bootmgr} /addfirst");
                            }

                            // Cleanup temp letter if assigned during detection
                            if (isSeparateBoot && targetS.Length <= 3 && !targetS.StartsWith("C", StringComparison.OrdinalIgnoreCase)) {
                                Log($"Cleaning up temporary boot letter: {targetS}");
                                RemoveDriveLetter(targetS.Substring(0, 1));
                            }
                        }
                } catch (Exception ex) {
                    Log("Boot Fix Error: " + ex.Message);
                }
            }

            this.Invoke(new Action(() => {
                UpdateProgress(100, GetStr("MSG_Done"));
                Log("--------------------------------------------------");
                Log(GetStr("LOG_BootFixDone"));
                MsgBox(GetStr("MSG_Done"), GetStr("UI_Success"), MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                isOperationRunning = false;
                SetUIState(false);
                SetText(btnAutoBootFix, "UI_BTN_BootFix");
                UpdateProgress(0, GetStr("UI_Ready"));
                // Refresh OS label in case a Windows install was just repaired/found
                lblInstalledOS.Text = GetDetectedOSLabel();
            }));
        }
        private string ShowWimIndexSelector(string wimPath)
        {
            List<WimIndexItem> indices = new List<WimIndexItem>();
            string wimlib = GetWimlibPath();
            Process p = new Process { 
                StartInfo = new ProcessStartInfo { 
                    FileName = wimlib, Arguments = $"info \"{wimPath}\"", 
                    UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true 
                } 
            };
            p.Start(); 
            string outputStr = p.StandardOutput.ReadToEnd() ?? string.Empty;
            p.WaitForExit(5000);

            string currentIdx = string.Empty;
            string currentName = string.Empty;
            foreach (string line in outputStr.Split('\n')) {
                string l = line.Trim();
                if (l.StartsWith("Index:", StringComparison.OrdinalIgnoreCase)) {
                    if (!string.IsNullOrEmpty(currentIdx)) {
                        indices.Add(new WimIndexItem { Index = currentIdx ?? "1", Name = string.IsNullOrEmpty(currentName) ? $"Index {currentIdx}" : currentName ?? string.Empty });
                    }
                    if (l.Length > 6) currentIdx = l.Substring(6).Trim() ?? string.Empty;
                    currentName = string.Empty;
                } else if (l.StartsWith("Name:", StringComparison.OrdinalIgnoreCase)) {
                    if (l.Length > 5) currentName = l.Substring(5).Trim() ?? string.Empty;
                }
            }
            if (!string.IsNullOrEmpty(currentIdx)) {
                indices.Add(new WimIndexItem { Index = currentIdx ?? "1", Name = string.IsNullOrEmpty(currentName) ? $"Index {currentIdx}" : currentName ?? string.Empty });
            }

            if (indices.Count == 0) return "1"; // Default fallback
            if (indices.Count == 1) return indices[0].Index;

            string selectedIndex = "1";
            this.Invoke(new Action(() => {
                using Form f = new Form { Text = GetStr("UI_SelectWimIndexTitle"), Size = new Size(400, 150), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
                Label l = new Label { Text = GetStr("UI_SelectWimIndexDesc"), Location = new Point(20, 15), AutoSize = true };
                ComboBox cb = new ComboBox { Location = new Point(20, 40), Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
                foreach (var i in indices) cb.Items.Add(i);
                cb.SelectedIndex = 0;
                Button btn = new Button { Text = GetStr("UI_OK"), Location = new Point(140, 75), Width = 100, FlatStyle = FlatStyle.Flat };
                btn.Click += (s, e) => { 
                    if (cb.SelectedItem is WimIndexItem item) selectedIndex = item.Index;
                    f.Close(); 
                };
                f.Controls.AddRange(new Control[] { l, cb, btn });
                f.ShowDialog();
            }));
            return selectedIndex;
        }

        private void RunHealthCheck()
        {
            if (isOperationRunning || isBackupRunning) return;

            string dismPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "dism.exe");
            string sfcPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sfc.exe");

            if (!File.Exists(dismPath) || !File.Exists(sfcPath)) {
                MsgBox(GetStr("MSG_HealthMissing"), GetStr("UI_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool winPE = IsWinPE();
            string sysDrive = GetSystemDrive(); 
            string offlineWindowsPath = "";
            string wimSourcePath = "";
            string wimIndex = "1";

            if (winPE) {
                // Find offline windows
                foreach (var di in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed)) {
                    if (di.Name.TrimEnd('\\').Equals(sysDrive, StringComparison.OrdinalIgnoreCase)) continue;
                    if (IsValidWindowsInstall(di.Name)) { offlineWindowsPath = di.Name.TrimEnd('\\'); break; }
                }
                if (string.IsNullOrEmpty(offlineWindowsPath)) {
                    MsgBox(GetStr("BF_NoWindowsFound"), GetStr("UI_Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Prompt user for WIM file
                this.Invoke(new Action(() => {
                    MsgBox(GetStr("MSG_SelectWimSource"), GetStr("UI_Info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    using OpenFileDialog ofd = new OpenFileDialog { Filter = "Windows Image|*.wim;*.esd;*.swm", Title = "Select install.wim" };
                    if (ofd.ShowDialog() == DialogResult.OK) {
                        wimSourcePath = ofd.FileName;
                    }
                }));

                if (string.IsNullOrEmpty(wimSourcePath)) return; // User cancelled
                wimIndex = ShowWimIndexSelector(wimSourcePath);

                // Use the offline OS's native toolset to match the servicing stack and avoid 0x800f081f or mismatch errors
                if (offlineWindowsPath != null) {
                    string offDism = Path.Combine(offlineWindowsPath, "Windows", "System32", "dism.exe");
                    string offSfc = Path.Combine(offlineWindowsPath, "Windows", "System32", "sfc.exe");
                    if (File.Exists(offDism)) dismPath = offDism;
                    if (File.Exists(offSfc)) sfcPath = offSfc;
                }
            }

            if (MsgBox(GetStr("CONFIRM_HEALTH_PROMPT"), GetStr("UI_Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            isOperationRunning = true;
            this.Invoke(new Action(() => {
                SetUIState(true);
                UpdateProgress(0, GetStr("BF_Starting"));
                rtbLog.Clear();
                Log("=== Check & Repair Windows Health ===");
            }));

            Task.Run(() => {
                string scratchFolder = "";
                string scratchArg = "";
                bool operationFailed = false;
                bool sourceMismatchError = false;


                try {
                    this.Invoke(new Action(() => Log(GetStr("MSG_HealthWaiting") ?? "Scan and repair processes may take a while, please be patient...")));
                    
                    // Setup common execution parameters
                    if (winPE && offlineWindowsPath != null) {
                        scratchFolder = Path.Combine(offlineWindowsPath, "RecoveryTool_Scratch");
                        if (!Directory.Exists(scratchFolder)) {
                            try { Directory.CreateDirectory(scratchFolder); } catch { }
                        }
                        string logPath = Path.Combine(scratchFolder, "dism.log");
                        scratchArg = $" /ScratchDir:\"{scratchFolder}\" /LogPath:\"{logPath}\"";
                    }
                    
                    string checkArgs = winPE ? $"/Image:{offlineWindowsPath}\\ /Cleanup-Image /CheckHealth{scratchArg}" : "/Online /Cleanup-Image /CheckHealth";
                    string scanArgs = winPE ? $"/Image:{offlineWindowsPath}\\ /Cleanup-Image /ScanHealth{scratchArg}" : "/Online /Cleanup-Image /ScanHealth";
                    string restoreArgs = winPE ? $"/Image:{offlineWindowsPath}\\ /Cleanup-Image /RestoreHealth /Source:wim:\"{wimSourcePath}\":{wimIndex} /LimitAccess{scratchArg}" : "/Online /Cleanup-Image /RestoreHealth";
                    string sfcArgs = winPE ? $"/scannow /offbootdir={offlineWindowsPath}\\ /offwindir={offlineWindowsPath}\\Windows" : "/scannow";

                    // 1. Run CheckHealth
                    string checkOut = RunCommandWithProgress(dismPath, checkArgs, "DISM CheckHealth");

                    // 2. Run ScanHealth
                    string scanOut = RunCommandWithProgress(dismPath, scanArgs, "DISM ScanHealth");

                    string combined = (checkOut + scanOut).ToLowerInvariant();
                    if (combined.Contains("error:")) operationFailed = true;

                    // "Repairable" (EN) or "onarılabilir" (TR) indicate repair is needed
                    if (combined.Contains("repairable") || combined.Contains("onar")) {
                        this.Invoke(new Action(() => Log("\n" + GetStr("MSG_HealthRepairing"))));
                        
                        // DISM RestoreHealth
                        string restoreOut = RunCommandWithProgress(dismPath, restoreArgs, "DISM RestoreHealth");
                        if (restoreOut.ToLowerInvariant().Contains("error:")) {
                            operationFailed = true;
                            if (restoreOut.ToLowerInvariant().Contains("0x800f081f")) sourceMismatchError = true;
                        }

                        // SFC ScanNow
                        string sfcOut = RunCommandWithProgress(sfcPath, sfcArgs, "SFC ScanNow");
                        if (sfcOut.ToLowerInvariant().Contains("could not perform")) operationFailed = true;

                    } else if (combined.Contains("healthy") || combined.Contains("alg") || combined.Contains("sağ") || combined.Contains("no component store corruption")) {
                        this.Invoke(new Action(() => Log("\n" + GetStr("MSG_HealthOK"))));
                    } else {
                        this.Invoke(new Action(() => Log("\n" + GetStr("MSG_HealthUnknown"))));
                        operationFailed = true;
                    }

                } catch (Exception ex) {
                    this.Invoke(new Action(() => Log("Error running health check: " + ex.Message)));
                    operationFailed = true;
                } finally {
                    // Clean up WinPE scratch dir to ensure no leftovers
                    if (winPE && !string.IsNullOrEmpty(scratchFolder) && Directory.Exists(scratchFolder)) {
                        try { Directory.Delete(scratchFolder, true); } catch { }
                    }

                    this.Invoke(new Action(() => {
                        UpdateProgress(100, GetStr("MSG_Done"));
                        Log("\n" + GetStr("MSG_HealthDone"));
                        
                        if (operationFailed) {
                            string failMsg = sourceMismatchError ? 
                                (GetStr("ERR_DISM_SOURCE") ?? "Missing or incompatible actual media (Error: 0x800f081f).") 
                                : (GetStr("MSG_Fail") ?? "Operation completed with errors. Please check the logs.");
                            MsgBox(failMsg, GetStr("UI_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        } else {
                            MsgBox(GetStr("MSG_HealthDone"), GetStr("UI_Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        
                        isOperationRunning = false;
                        SetUIState(false);
                        UpdateProgress(0, GetStr("UI_Ready"));
                    }));
                }
            });
        }

        private string RunCommandWithProgress(string fileName, string arguments, string title)
        {
            this.Invoke(new Action(() => { 
                Log($"\n>>> {title}: {fileName} {arguments}"); 
                UpdateProgress(1, title + "..."); 
                Application.DoEvents(); 
            }));
            
            StringBuilder fullOutput = new StringBuilder();
            StringBuilder currentLine = new StringBuilder();

            Process p = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            p.Start();

            if (p.StandardOutput == null) return "";

            while (!p.StandardOutput.EndOfStream) {
                int charCode = p.StandardOutput.Read();
                if (charCode == -1) break;
                char c = (char)charCode;

                if (c == '\r' || c == '\n') {
                    string line = currentLine.ToString();
                    currentLine.Clear();
                    
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string cleanLine = new string(line.Where(ch => !char.IsControl(ch)).ToArray()).Trim();
                    if (string.IsNullOrWhiteSpace(cleanLine)) continue;

                    bool isProgress = ParseAndReportProgress(cleanLine, title);
                    
                    if (!isProgress) {
                        if (cleanLine.Any(char.IsLetter)) {
                            fullOutput.AppendLine(cleanLine);
                            this.Invoke(new Action(() => Log(cleanLine)));
                        }
                    }
                } else {
                    currentLine.Append(c);
                    // Critical DISM Fix: Some DISM versions in WinPE update progress bar without newlines (\b)
                    // If we see a bracket and a percentage in the buffer, try to parse it
                    string buffer = currentLine.ToString();
                    if (buffer.Contains("[") && buffer.Contains("]") && buffer.Contains("%")) {
                        if (ParseAndReportProgress(buffer, title)) {
                            currentLine.Clear(); // Clear buffer after successful progress update
                        }
                    }
                }
            }
            p.WaitForExit();
            return fullOutput.ToString().Trim();
        }

        private bool ParseAndReportProgress(string line, string title)
        {
            try {
                if (line.Contains("[") && line.Contains("]") && line.Contains("%")) {
                    int pctIdx = line.IndexOf('%');
                    int start = pctIdx - 1;
                    while (start >= 0 && (char.IsDigit(line[start]) || line[start] == '.')) start--;
                    if (start < pctIdx - 1) {
                        if (double.TryParse(line.Substring(start + 1, pctIdx - start - 1), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double pct)) {
                            int intPct = (int)pct;
                            if (intPct >= 2) {
                                this.Invoke(new Action(() => UpdateProgress(intPct, $"{title}... {intPct}%")));
                            }
                            return true;
                        }
                    }
                }
                if (line.Contains("%") && (line.ToLower().Contains("verification") || line.ToLower().Contains("doğrulama"))) {
                    string[] parts = line.Split(' ');
                    foreach (var part in parts) {
                        if (part.EndsWith("%") && double.TryParse(part.TrimEnd('%'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sfcPct)) {
                            int intSfcPct = (int)sfcPct;
                            if (intSfcPct >= 2) {
                                this.Invoke(new Action(() => UpdateProgress(intSfcPct, $"{title}... {intSfcPct}%")));
                            }
                            return true;
                        }
                    }
                }
            } catch { }
            return false;
        }

        private string? GetDiskIndexForLetter(string letter)
        {
            try {
                string l = letter.TrimEnd('\\', ':').ToUpper() + ":";
                using ManagementObjectSearcher partitionSearcher = new($"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{l}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
                foreach (ManagementObject partition in partitionSearcher.Get().Cast<ManagementObject>()) {
                    using ManagementObjectSearcher driveSearcher = new($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
                    foreach (ManagementObject disk in driveSearcher.Get().Cast<ManagementObject>()) {
                        return disk["Index"]?.ToString();
                    }
                }
            } catch { }
            return null;
        }

        private string? GetDiskIndexForDrive(string driveLetter)
        {
            return GetDiskIndexForLetter(driveLetter);
        }

        private string GetPartitionIndexForDrive(string driveLetter)
        {
            try {
                string drive = driveLetter.TrimEnd('\\');
                using ManagementObjectSearcher partitionSearcher = new($"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{drive}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
                foreach (ManagementObject partition in partitionSearcher.Get()) {
                    // WMI index is 0-based, Diskpart is 1-based. Usually they match in Diskpart index property though.
                    return (Convert.ToInt32(partition["Index"]) + 1).ToString();
                }
            } catch { }

            // Fallback: Use DiskpartParser
            try {
                string letter = driveLetter.Substring(0, 1).ToUpper();
                var disks = DiskpartParser.GetDisks();
                foreach (var d in disks) {
                    var p = d.Partitions.FirstOrDefault(x => x.DriveLetter == letter);
                    if (p != null) return p.Index.ToString();
                }
            } catch { }

            return "-1";
        }

        private void ShowAbout()
        {
            using (Form f = new Form { 
                Text = GetStr("UI_AboutTitle"), 
                ClientSize = new Size(420, 210), 
                StartPosition = FormStartPosition.CenterParent, 
                FormBorderStyle = FormBorderStyle.FixedDialog, 
                MaximizeBox = false, MinimizeBox = false,
                AutoScaleMode = AutoScaleMode.Dpi,
                AutoScaleDimensions = new SizeF(96F, 96F)
            }) {
                f.Padding = new Padding(20);

                Label lblTitle = new Label {
                    Text = "Windows Backup / Restore Tool",
                    Dock = DockStyle.Top, 
                    AutoSize = false,
                    Height = 40,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 11.5f, FontStyle.Bold)
                };

                Label lblAuthor = new Label {
                    Text = GetStr("UI_MadeBy"),
                    Dock = DockStyle.Top, 
                    AutoSize = false,
                    Height = 40,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
                };
                
                LinkLabel lnk = new LinkLabel { 
                    Text = GetStr("UI_BUY_COFFEE"), 
                    Dock = DockStyle.Top, 
                    Height = 35,
                    AutoSize = false, 
                    TextAlign = ContentAlignment.MiddleCenter
                };
                lnk.LinkClicked += (s, e) => OpenUrl("https://buymeacoffee.com/abdullaherturk");
                
                Button ok = new Button { 
                    Text = GetStr("UI_OK"), 
                    Size = new Size(120, 35), 
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 120, 215),
                    ForeColor = Color.White
                };
                ok.Location = new Point((f.ClientSize.Width - ok.Width) / 2, f.ClientSize.Height - ok.Height - 20);
                ok.Click += (s, e) => f.Close();

                f.Controls.Add(ok);
                f.Controls.Add(lnk);
                f.Controls.Add(lblAuthor); 
                f.Controls.Add(lblTitle); 
                f.ShowDialog();
            }
        }

        private void RefreshRestoreTargets()
        {
            // NEW: No longer blocks the UI thread.
            Task.Run(() => {
                var items = GetRestoreTargetsData();
                this.BeginInvoke(new Action(() => {
                    cmbRestoreTarget.Items.Clear();
                    foreach (var item in items) cmbRestoreTarget.Items.Add(item);
                }));
            });
        }

        private List<object> GetRestoreTargetsData()
        {
            List<object> items = new List<object>();
            try {
                if (rbWholeDisk.Checked)
                {
                    using ManagementObjectSearcher searcher = new("SELECT * FROM Win32_DiskDrive WHERE Size > 0");
                    foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                    {
                        long sizeBytes = Convert.ToInt64(obj["Size"] ?? 0);
                        double sizeGB = Math.Round(sizeBytes / (1024.0 * 1024.0 * 1024.0), 2);
                        string status = obj["Status"]?.ToString() ?? "Healthy";
                        items.Add(new DiskItem { 
                            DiskID = obj["Index"]?.ToString() ?? "0", 
                            DisplayText = $"Disk {obj["Index"] ?? "?"}: [{status}] {obj["Model"] ?? "Unknown"} ({sizeGB} GB)",
                            SizeBytes = sizeBytes,
                            SizeGB = sizeGB
                        });
                    }
                }
                else
                {
                    List<PartitionItem> allParts = new List<PartitionItem>();
                    using ManagementObjectSearcher searcher = new("SELECT * FROM Win32_DiskPartition");
                    foreach (ManagementObject part in searcher.Get().Cast<ManagementObject>())
                    {
                        string devID = part["DeviceID"]?.ToString() ?? "";
                        string dIdx = "0", pIdx = "1";
                        Match m = Regex.Match(devID, @"Disk #(\d+), Partition #(\d+)");
                        if (m.Success) { dIdx = m.Groups[1].Value; pIdx = (int.Parse(m.Groups[2].Value) + 1).ToString(); }
                        ulong partSize = Convert.ToUInt64(part["Size"]);
                        string sizeStr = partSize >= (1024UL * 1024 * 1024) ? Math.Round(partSize / (1024.0 * 1024.0 * 1024.0), 2) + " GB" : Math.Round(partSize / (1024.0 * 1024.0), 0) + " MB";
                        using var logSearch = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{devID}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                        var logicalDisks = logSearch.Get().Cast<ManagementObject>().ToList();
                        if (logicalDisks.Count > 0) {
                            foreach (ManagementObject obj in logicalDisks) {
                                allParts.Add(new PartitionItem { DisplayText = $"{(obj["DeviceID"]?.ToString() ?? "")} {(obj["VolumeName"]?.ToString() ?? "")} ({sizeStr})", DrivePath = (obj["DeviceID"]?.ToString() ?? "") + "\\", DiskIndex = dIdx, PartitionIndex = pIdx, SizeBytes = (long)partSize, HasLetter = true });
                            }
                        } else {
                            string wmiPartType = part["Type"]?.ToString() ?? "", wmiTypeLC = wmiPartType.ToLowerInvariant(), label = "Unallocated", fs = "RAW";
                            if (wmiTypeLC.Contains("recovery")) { label = "Recovery"; fs = "NTFS"; }
                            else if (wmiTypeLC.Contains("system")) { label = "System (EFI)"; fs = "FAT32"; }
                            else if (wmiTypeLC.Contains("reserved")) { label = "MSR"; fs = "RAW"; }
                            allParts.Add(new PartitionItem { DisplayText = $"{label} ({sizeStr}) [{fs}]", DrivePath = "", DiskIndex = dIdx, PartitionIndex = pIdx, SizeBytes = (long)partSize, IsUnformatted = (fs == "RAW"), HasLetter = false, FileSystem = fs });
                        }
                    }
                    foreach (var item in allParts.OrderByDescending(p => p.HasLetter).ThenBy(p => p.DrivePath).ToList()) items.Add(item);
                }
            } catch { }
            return items;
        }

        private void RefreshDisksAndPartitions()
        {
            // COLLISION GUARD: Don't refresh if we are in the middle of a critical operation
            if (isOperationRunning || isBackupRunning || isScanningFlag) return;

            isScanningFlag = true;
            Log(GetStr("LOG_Scanning") ?? "Scanning disks...");
            UpdateProgress(10, "Scanning...");
            Task.Run(() => {
                try {
                    var backupItems = GetBackupSourcesData();
                    var restoreItems = GetRestoreTargetsData();
                    
                    this.BeginInvoke(new Action(() => {
                        // Re-check guard inside BeginInvoke to be extra safe
                        if (isOperationRunning || isBackupRunning) { isScanningFlag = false; return; }

                        cmbBackupSource.Items.Clear();
                        foreach (var item in backupItems) cmbBackupSource.Items.Add(item);
                        
                        cmbRestoreTarget.Items.Clear();
                        foreach (var item in restoreItems) cmbRestoreTarget.Items.Add(item);
                        
                        if (cmbBackupSource.Items.Count > 0) cmbBackupSource.SelectedIndex = 0;
                        if (cmbRestoreTarget.Items.Count > 0) cmbRestoreTarget.SelectedIndex = 0;
                        
                        Log(GetStr("LOG_Scanning_Done") ?? "Scanning complete.");
                        UpdateProgress(0, GetStr("UI_Ready"));
                        isScanningFlag = false;
                    }));
                } catch { isScanningFlag = false; }
            });
        }

        private void RefreshBackupSources() { RefreshDisksAndPartitions(); }

        private List<PartitionItem> GetBackupSourcesData()
        {
            List<PartitionItem> items = new List<PartitionItem>();
            try {
                using ManagementObjectSearcher searcher = new("SELECT * FROM Win32_DiskPartition");
                foreach (ManagementObject part in searcher.Get().Cast<ManagementObject>()) {
                    string devID = part["DeviceID"]?.ToString() ?? "", dIdx = "0", pIdx = "1";
                    Match m = Regex.Match(devID, @"Disk #(\d+), Partition #(\d+)");
                    if (m.Success) { dIdx = m.Groups[1].Value; pIdx = (int.Parse(m.Groups[2].Value) + 1).ToString(); }
                    ulong pSize = Convert.ToUInt64(part["Size"]);
                    string sizeStr = pSize >= (1024UL * 1024 * 1024) ? Math.Round(pSize / (1024.0 * 1024.0 * 1024.0), 2) + " GB" : Math.Round(pSize / (1024.0 * 1024.0), 0) + " MB";
                    using var logSearch = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{devID}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                    var logicalDisks = logSearch.Get().Cast<ManagementObject>().ToList();
                    if (logicalDisks.Count > 0) {
                        foreach (ManagementObject obj in logicalDisks) {
                            items.Add(new PartitionItem { DisplayText = $"{(obj["DeviceID"]?.ToString() ?? "")} {(obj["VolumeName"]?.ToString() ?? "")} ({sizeStr})", DrivePath = (obj["DeviceID"]?.ToString() ?? "") + "\\", DiskIndex = dIdx, PartitionIndex = pIdx, HasLetter = true });
                        }
                    } else {
                        string wmiPartType = part["Type"]?.ToString() ?? "", wmiTypeLC = wmiPartType.ToLowerInvariant(), label = "Unallocated", fs = "RAW";
                        if (wmiTypeLC.Contains("recovery")) { label = "Recovery"; fs = "NTFS"; }
                        else if (wmiTypeLC.Contains("system")) { label = "System (EFI)"; fs = "FAT32"; }

                        items.Add(new PartitionItem { DisplayText = $"{label} ({sizeStr}) [{fs}]", DrivePath = "", DiskIndex = dIdx, PartitionIndex = pIdx, SizeBytes = (long)pSize, IsUnformatted = (fs == "RAW"), HasLetter = false, FileSystem = fs });
                    }
                }
            } catch { }
            return items.OrderByDescending(p => p.HasLetter).ThenBy(p => p.DrivePath).ToList();
        }

        private void btnRefresh_Click(object sender, EventArgs e) { RefreshDisksAndPartitions(); }

        private static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Unified handling for all active operations including Driver Backup/Restore
            if (isBackupRunning || isOperationRunning) {
                if (MsgBox(GetStr("EXIT_PROMPT"), GetStr("UI_Warning"), MessageBoxButtons.YesNo) == DialogResult.No) {
                    e.Cancel = true;
                    return;
                } else {
                    if (isBackupRunning) isBackupAborted = true; 
                    if (isOperationRunning) isRestoreAborted = true;
                    KillProcesses();
                }
            } else {
                // Safety cleanup even if no active operation is tracked
                UpdateProgress(0, GetStr("UI_Ready"), ColorRestore);
            }
            base.OnFormClosing(e);
        }
        #endregion

        #region Boiling Plate & Language
        private void CalculateLayout() 
        { 
            if (currentDiskSizeGB <= 0) return; 
            double b = (double)numBootSizeMB.Value / 1024.0; 
            double w = (double)numWinSizeGB.Value; 
            double r = chkCreateRecovery.Checked ? (double)numRecoverySizeMB.Value / 1024.0 : 0; 
            double d = currentDiskSizeGB - b - w - r; 
            
            // 1:1 Parity with Diskpart GUI dynamic unit labels
            double bootMbVal = (double)numBootSizeMB.Value;
            double winGbVal = (double)numWinSizeGB.Value;
            double recMbVal = (double)numRecoverySizeMB.Value;

            lblBootSize.AutoSize = true;
            lblBootSize.Text = (GetStr("UI_BootSize") ?? "").Replace(bootMbVal >= 1024 ? "(MB)" : "(GB)", bootMbVal >= 1024 ? "(GB)" : "(MB)");

            lblWinSize.AutoSize = true;
            lblWinSize.Text = GetStr("UI_WindowsSize"); // "Windows (GB):" from ini

            string recLabel = GetStr("UI_CreateRecovery");
            chkCreateRecovery.AutoSize = true;
            chkCreateRecovery.Text = recMbVal >= 1024 
                ? recLabel.Replace("(MB)", "(GB)") 
                : recLabel.Replace("(GB)", "(MB)");

            if (d <= 0) { 
                lblDataSize.Text = GetStr("ERR_InsufficientSpace"); // Now "DATA 0 GB"
                lblDataSize.ForeColor = Color.Red; 
                mapTip.SetToolTip(lblDataSize, GetStr("UI_SinglePartTooltip"));
            } else { 
                lblDataSize.Text = string.Format(GetStr("UI_DataSize"), Math.Round(d, 1)); 
                lblDataSize.ForeColor = Color.LimeGreen; 
                mapTip.SetToolTip(lblDataSize, GetStr("UI_DataTooltip"));
            }

            pnlVisualMap.Invalidate(); 
        }

        private void PnlVisualMap_Paint(object sender, PaintEventArgs e) 
        { 
            if (currentDiskSizeGB <= 0) return; 
            Graphics g = e.Graphics; 
            float totalW = pnlVisualMap.Width; 
            float h = pnlVisualMap.Height;

            double bootGb = (double)numBootSizeMB.Value / 1024.0;
            double winGb = (double)numWinSizeGB.Value;
            double recGb = chkCreateRecovery.Checked ? (double)numRecoverySizeMB.Value / 1024.0 : 0;
            double dataGb = currentDiskSizeGB - bootGb - winGb - recGb;
            if (dataGb < 0) dataGb = 0;

            // Smart Scaling Logic (min 25px for active partitions)
            bool hasBoot = bootGb > 0;
            bool hasWin = winGb > 0;
            bool hasRec = chkCreateRecovery.Checked && recGb > 0;
            bool hasData = dataGb > 0.1;

            int minB = hasBoot ? 25 : 0;
            int minW = hasWin ? 25 : 0;
            int minR = hasRec ? 25 : 0;
            int minD = hasData ? 15 : 0;

            int reserved = minB + minW + minR + minD;
            float expandable = totalW - reserved;
            if (expandable < 0) expandable = 0;

            double totalGb = bootGb + winGb + (hasRec ? recGb : 0) + (hasData ? dataGb : 0);
            if (totalGb <= 0) totalGb = 1;

            int calcB = minB + (hasBoot ? (int)((bootGb / totalGb) * expandable) : 0);
            int calcW = minW + (hasWin ? (int)((winGb / totalGb) * expandable) : 0);
            int calcR = minR + (hasRec ? (int)((recGb / totalGb) * expandable) : 0);
            int calcD = (int)totalW - calcB - calcW - calcR;
            if (calcD < 0) calcD = 0;

            // Drawing Order: Boot -> Windows -> DATA -> Recovery (Last)
            int currX = 0;
            if (hasBoot) {
                g.FillRectangle(new SolidBrush(Color.FromArgb(230, 126, 34)), currX, 0, calcB, h);
                currX += calcB;
            }
            if (hasWin) {
                g.FillRectangle(new SolidBrush(Color.FromArgb(52, 152, 219)), currX, 0, calcW, h);
                currX += calcW;
            }
            if (hasData) {
                g.FillRectangle(new SolidBrush(Color.FromArgb(46, 204, 113)), currX, 0, calcD, h);
                currX += calcD;
            }
            if (hasRec) {
                g.FillRectangle(new SolidBrush(Color.FromArgb(155, 89, 182)), totalW - calcR, 0, calcR, h);
            }

            g.DrawRectangle(Pens.Gray, 0, 0, totalW - 1, h - 1);
        }

        private void PnlVisualMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentDiskSizeGB <= 0) return;
            float w = pnlVisualMap.Width;
            float bW = (float)((double)numBootSizeMB.Value / (currentDiskSizeGB * 1024.0) * w);
            float wW = (float)((double)numWinSizeGB.Value / currentDiskSizeGB * w);
            float rW = chkCreateRecovery.Checked ? (float)((double)numRecoverySizeMB.Value / (currentDiskSizeGB * 1024.0) * w) : 0;
            float dW = w - bW - wW - rW;

            double bootGb = (double)numBootSizeMB.Value / 1024.0;
            double winGb = (double)numWinSizeGB.Value;
            double recGb = chkCreateRecovery.Checked ? (double)numRecoverySizeMB.Value / 1024.0 : 0;

            string tip = "";
            if (e.X < bW) tip = $"Boot: {numBootSizeMB.Value} MB";
            else if (e.X < bW + wW) tip = $"Windows: {numWinSizeGB.Value} GB";
            else if (e.X < bW + wW + dW) {
                tip = (dW > 15) ? string.Format(GetStr("UI_DataSize"), Math.Round(currentDiskSizeGB - (bW+wW+rW)/w*currentDiskSizeGB, 1)) : "DATA";
                string extra = (currentDiskSizeGB - (bootGb + winGb + recGb) <= 0) ? GetStr("UI_SinglePartTooltip") : GetStr("UI_DataTooltip");
                tip += "\n" + extra;
            }
            else if (chkCreateRecovery.Checked && e.X >= w - rW) tip = $"Recovery: {numRecoverySizeMB.Value} MB";

            if (!string.IsNullOrEmpty(tip)) mapTip.SetToolTip(pnlVisualMap, tip);
        }
        private bool SafeBrowse(TextBox target, bool isSave) 
        {
            using FileDialog fd = isSave ? new SaveFileDialog() : new OpenFileDialog();
            // Filter based on compression for Save, or all for Open
            if (isSave) {
                if (cbCompression.SelectedIndex == 2)
                    fd.Filter = "Electronic Software Distribution (*.esd)|*.esd";
                else
                    fd.Filter = "Windows Image (*.wim)|*.wim";
            } else {
                fd.Filter = "Windows Image (*.wim;*.esd)|*.wim;*.esd";
            }
            
            fd.FileName = (cbCompression.SelectedIndex == 2 && isSave) ? "install.esd" : "install.wim";
            
            if (fd.ShowDialog() == DialogResult.OK) {
                target.Text = fd.FileName;
                if (!isSave) LoadWimInfo(fd.FileName);
                return true;
            }
            return false;
        }
        private void BtnBrowseBackup_Click(object sender, EventArgs e) => SafeBrowse(txtBackupDest, true);
        private void BtnStartBackup_Click(object? sender, EventArgs e) 
        { 
            if (isBackupRunning) {
                if (MsgBox(GetStr("CONFIRM_ABORT") ?? "Abort backup?", GetStr("UI_Abort") ?? "Abort", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                    isBackupAborted = true;
                    Log(GetStr("MSG_Aborting") ?? "Aborting operation...");
                    KillProcesses();
                }
                return;
            }

            if (cmbBackupSource.SelectedItem == null || string.IsNullOrEmpty(txtBackupDest.Text)) return; 
            
            var selectedItem = cmbBackupSource.SelectedItem as PartitionItem;
            if (selectedItem == null) return;

            string sourcePath = selectedItem.DrivePath;
            string destPath = txtBackupDest.Text;
            int compIndex = cbCompression.SelectedIndex;
            string compType = "fast"; // Default
            if (compIndex == 0) compType = "none";
            else if (compIndex == 1) compType = "fast";
            else if (compIndex == 2) compType = "maximum";
            
            isBackupRunning = true; 
            isBackupAborted = false; 
            btnStartBackup.Text = GetStr("BTN_Abort") ?? "Abort";
            SetUIState(true);

            Task.Run(() => {
                string? tempLetter = null;
                string? configPath = null;
                string finalSource = sourcePath;

                try {
                    // 1. Handle Partition without Letter (Assign via Diskpart)
                    if (!selectedItem.HasLetter) {
                        this.Invoke(new Action(() => Log(GetStr("LOG_WaitDrive").Replace("{0}", "assigned letter (Diskpart)"))));
                        tempLetter = AssignLetterViaDiskpart(selectedItem.DiskIndex, selectedItem.PartitionIndex);
                        if (tempLetter == null) {
                            this.Invoke(new Action(() => {
                                MsgBox(GetStr("MSG_Fail"));
                                isBackupRunning = false;
                                btnStartBackup.Text = GetStr("BTN_StartBackup") ?? "Start Backup";
                                SetUIState(false);
                            }));
                            return;
                        }
                        finalSource = tempLetter;
                    } 
                    // 2. Handle Volume GUID (Legacy Fallback)
                    else if (sourcePath.StartsWith("\\\\?\\Volume")) {
                        this.Invoke(new Action(() => Log(GetStr("LOG_WaitDrive").Replace("{0}", "assigned letter (WMI)"))));
                        tempLetter = AssignTempLetterToGUID(sourcePath);
                        if (tempLetter == null) {
                            this.Invoke(new Action(() => {
                                MsgBox(GetStr("MSG_Fail"));
                                isBackupRunning = false;
                                btnStartBackup.Text = GetStr("BTN_StartBackup") ?? "Start Backup";
                                SetUIState(false);
                            }));
                            return;
                        }
                        finalSource = tempLetter;
                    }

                    if (!finalSource.EndsWith("\\") && !finalSource.StartsWith("\\\\?\\Volume") && finalSource.Length > 0) finalSource += "\\";

                    // 2. Build Exclusion Config (Wimlib Config)
                    configPath = Path.Combine(Path.GetTempPath(), $"wimlib_config_{DateTime.Now.Ticks}.ini");
                    StringBuilder sb = new();
                    sb.AppendLine("[ExclusionList]");
                    sb.AppendLine("\\System Volume Information");
                    sb.AppendLine("\\$RECYCLE.BIN");
                    sb.AppendLine("\\pagefile.sys");
                    sb.AppendLine("\\hiberfil.sys");
                    sb.AppendLine("\\swapfile.sys");
                    
                    // Dynamic Cloud Exclusions (Mirroring Legacy)
                    if (Directory.Exists(finalSource + "Users")) {
                        foreach (string userDir in Directory.GetDirectories(finalSource + "Users")) {
                            string user = Path.GetFileName(userDir);
                            string[] cloudDirs = { "OneDrive", "Dropbox", "Google Drive", "SkyDrive" };
                            foreach (string cloud in cloudDirs) {
                                if (Directory.Exists(Path.Combine(userDir, cloud)))
                                    sb.AppendLine($"\\Users\\{user}\\{cloud}");
                            }
                            if (Directory.Exists(Path.Combine(userDir, "AppData\\Local\\Temp")))
                                sb.AppendLine($"\\Users\\{user}\\AppData\\Local\\Temp");
                        }
                    }
                    File.WriteAllText(configPath, sb.ToString(), Encoding.UTF8);

                    // 3. Flags and Command
                    bool isESD = destPath.EndsWith(".esd", StringComparison.OrdinalIgnoreCase);
                    string compressArg = (isESD || compType == "maximum") ? "--compress=LZMS" : $"--compress={compType}";
                    string extraArgs = "--check --no-acls";
                    if (isESD || compType == "maximum") extraArgs += " --solid";
                    if (!isWinPE) extraArgs += " --snapshot";
                    if (File.Exists(configPath)) extraArgs += $" --config=\"{configPath}\"";

                    string w = GetWimlibPath();
                    this.Invoke(new Action(() => Log($"Starting backup of {finalSource} to {destPath}...")));
                    
                    // Match Legacy Pathing: Ensure drive roots are captured correctly using user's favored \ format.
                    // To prevent the trailing backslash from escaping the closing quote in wimCmd, we use double-backslash if it ends in \.
                    string safeSource = finalSource;
                    if (safeSource.EndsWith("\\") && !safeSource.StartsWith("\\\\")) {
                        safeSource = safeSource.TrimEnd('\\') + "\\\\"; // Results in "C:\\" inside the command
                    }
                    
                    // Matching Legacy Command Structure: [NAME] [DESCRIPTION] [OPTIONS]
                    string wimCmd = $"\"{w}\" capture \"{safeSource}\" \"{destPath}\" \"WindowsBackup\" \"Created_by_BackupRestoreTool\" {compressArg} {extraArgs}";
                    
                    // Shell-Wrapper approach for WinPE compatibility
                    string tempBat = Path.Combine(Path.GetTempPath(), $"backup_{DateTime.Now.Ticks}.bat");
                    File.WriteAllText(tempBat, $"@echo off\r\necho Running: {wimCmd}\r\n{wimCmd}", Encoding.Default);
                    
                    bool success = RunProcess("cmd.exe", $"/c \"{tempBat}\"");
                    if (File.Exists(tempBat)) try { File.Delete(tempBat); } catch { }

                    // 4. Finalize and Cleanup
                    isBackupRunning = false; 
                    this.Invoke(new Action(() => {
                        // CLEANUP: Always remove temporary letters at the end of the operation
                        if (!string.IsNullOrEmpty(tempLetter)) {
                            if (!selectedItem.HasLetter) {
                                RemoveLetterViaDiskpart(selectedItem.DiskIndex, selectedItem.PartitionIndex, tempLetter);
                            } else if (sourcePath.StartsWith("\\\\?\\Volume")) {
                                RemoveTempLetter(tempLetter);
                            }
                        }
                        
                        if (configPath != null && File.Exists(configPath)) try { File.Delete(configPath); } catch { }
                        
                        Log("--------------------------------------------------");
                        if (isBackupAborted) {
                            Log(GetStr("MSG_BackupAborted"));
                            // AGGRESSIVE CLEANUP: Wait for process to fully release handle and retry deletion
                            Task.Run(() => {
                                Thread.Sleep(1000); 
                                for (int i = 0; i < 5; i++) {
                                    if (File.Exists(destPath)) {
                                        try { File.Delete(destPath); Log($"Successfully deleted aborted file on attempt {i+1}"); break; }
                                        catch { Thread.Sleep(1000); }
                                    } else break;
                                }
                            });
                            MsgBox(GetStr("MSG_BackupAborted"), GetStr("UI_Aborted"), MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        }
                        else {
                            if (success) {
                                Log(GetStr("MSG_Done"));
                                if (!chkPostAction.Checked) {
                                    MsgBox(GetStr("MSG_Done"), GetStr("UI_Success"), MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                                }
                                HandlePostProcessAction();
                            }
                            else {
                                Log(GetStr("MSG_Fail"));
                                MsgBox(GetStr("MSG_Fail"), GetStr("UI_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                                if (File.Exists(destPath)) try { File.Delete(destPath); } catch { }
                            }
                        }
                        btnStartBackup.Text = GetStr("BTN_StartBackup") ?? "Start Backup";
                        SetUIState(false);
                        UpdateProgress(0, GetStr("UI_Ready"));
                    }));
                } catch (Exception ex) {
                    isBackupRunning = false;
                    this.Invoke(new Action(() => {
                        if (tempLetter != null) RemoveTempLetter(tempLetter);
                        Log("Backup Fatal Error: " + ex.Message);
                        Log(GetStr("MSG_Fail"));
                        MsgBox(GetStr("MSG_Fail"), GetStr("UI_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        btnStartBackup.Text = GetStr("BTN_StartBackup") ?? "Start Backup";
                        SetUIState(false);
                        UpdateProgress(0, GetStr("UI_Ready"));
                    }));
                }
            }); 
        }
        private void BtnStartRestore_Click(object? sender, EventArgs e) 
        { 
            if (isOperationRunning) {
                if (MsgBox(GetStr("CONFIRM_ABORT") ?? "Abort restore?", GetStr("UI_Abort") ?? "Abort", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                    isRestoreAborted = true;
                    Log(GetStr("MSG_Aborting") ?? "Aborting operation...");
                    KillProcesses();
                }
                return;
            }
            if (string.IsNullOrEmpty(txtWimPath.Text) || cmbRestoreTarget.SelectedItem == null) return; 

            // Capture UI values BEFORE background task (Thread Safety)
            bool wholeDisk = rbWholeDisk.Checked;
            var selectedTarget = cmbRestoreTarget.SelectedItem;
            var selectedWimIndex = cmbWimIndex.SelectedItem as WimIndexItem;
            if (selectedTarget == null || selectedWimIndex == null) return;
            bool createBoot = chkCreateBoot.Checked;
            string wimPath = txtWimPath.Text;
            string firmware = rbUEFI.Checked ? "UEFI" : "BIOS";

            if (selectedWimIndex == null) return;

            isOperationRunning = true; 
            isRestoreAborted = false; 
            newWindowsDriveAfterReconstruction = null;
            btnStartRestore.Text = GetStr("BTN_Abort") ?? "Abort";
            SetUIState(true);

            // --- LIVE PROTECTION CHECK (Enhanced v4.15) ---
            if (!isWinPE) {
                string sysDiskIdx = GetSystemDiskIndex();
                bool isBlocked = false;

                if (wholeDisk) {
                    if (((DiskItem)selectedTarget).DiskID == sysDiskIdx) {
                        isBlocked = true;
                    }
                } else {
                    var pi = (PartitionItem)selectedTarget;
                    string sysPartIdx = GetPartitionIndexForDrive("C:");
                    
                    if (pi.DiskIndex == sysDiskIdx) {
                        int targetIdx = 0; int.TryParse(pi.PartitionIndex, out targetIdx);
                        int currentSysIdx = 0; int.TryParse(sysPartIdx, out currentSysIdx);
                        
                        // Rule: Block C: and everything BEFORE it on the system disk
                        if (targetIdx > 0 && currentSysIdx > 0 && targetIdx <= currentSysIdx) {
                            isBlocked = true;
                        }
                    }
                    
                    // Safety Fallback: Explicit C: drive path check
                    if (!isBlocked && pi.DrivePath.ToUpper().StartsWith("C:")) {
                        isBlocked = true;
                    }
                }

                if (isBlocked) {
                    MsgBox(GetStr("ERR_LiveProtection"), GetStr("UI_Error"), MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    isOperationRunning = false;
                    btnStartRestore.Text = GetStr("BTN_StartRestore") ?? "Start Restore";
                    SetUIState(false);
                    return;
                }
            }

            if (MsgBox(GetStr("CONFIRM_PROMPT"), GetStr("UI_Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) {
                isOperationRunning = false;
                btnStartRestore.Text = GetStr("BTN_StartRestore") ?? "Start Restore";
                SetUIState(false);
                return;
            }

            UpdateProgress(0, GetStr("BF_Starting"));
            
            Task.Run(() => {
                bool success = false;
                try {
                    string? targetPath = "";
                    string dynamicBootLetter = ""; 
                    string? tempTargetLetter = null;
                    string firmware = rbUEFI.Checked ? "UEFI" : "BIOS";

                    if (wholeDisk) {
                        var prep = PrepareDisk(((DiskItem)selectedTarget).DiskID);
                        targetPath = prep.WinPath;
                        dynamicBootLetter = prep.BootLetter;
                        
                        // Dynamic polling for the assigned drive letter
                        bool ready = false;
                        for (int i = 0; i < 20; i++) {
                            if (Directory.Exists(targetPath)) { ready = true; break; }
                            this.Invoke(new Action(() => Log(GetStr("LOG_WaitDrive").Replace("{0}", (i + 1).ToString()))));
                            Thread.Sleep(500);
                        }
                        if (!ready) {
                            this.Invoke(new Action(() => MsgBox(string.Format(GetStr("MSG_W_NotFound"), targetPath), GetStr("UI_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error)));
                            isOperationRunning = false; return;
                        }

                        // STABILITY DELAY: Let storage controller settle after Diskpart formatting/partitioning
                        this.Invoke(new Action(() => Log("Waiting for storage stack to settle (3s)...")));
                        Thread.Sleep(3000);
                    } else {
                        // --- PARTITION-ONLY MODE (Logic Consolidated) ---
                        var pi = (PartitionItem)selectedTarget;
                        targetPath = pi.DrivePath;
                        bool preparationDone = false;

                        // Step 1: Handle RAW or Unallocated targets first
                        if (pi.IsUnallocated || pi.IsUnformatted) {
                            if (createBoot) {
                                // Full layout rebuild required for boot fix on unallocated space
                                this.Invoke(new Action(() => Log(GetStr("LOG_BootPartNotFound") + " (Unallocated target)")));
                                int pIdx = 0; int.TryParse(pi.PartitionIndex, out pIdx);
                                bool targetDiskGPT = IsDiskGPT(pi.DiskIndex);
                                
                                string? reconstructedBoot = FindAndFormatPrecedingBootPartition(pi.DiskIndex, pIdx, targetDiskGPT);
                                if (!string.IsNullOrEmpty(reconstructedBoot)) {
                                    dynamicBootLetter = reconstructedBoot.TrimEnd('\\', ':');
                                    if (!string.IsNullOrEmpty(newWindowsDriveAfterReconstruction)) {
                                        targetPath = newWindowsDriveAfterReconstruction;
                                        this.Invoke(new Action(() => Log("Target path updated after reconstruction: " + targetPath)));
                                    }
                                }
                                preparationDone = true;
                            } else {
                                // Just simple auto-prepare without boot fix
                                this.Invoke(new Action(() => Log(pi.IsUnallocated ? "Auto-preparing unallocated space..." : "Formatting unformatted partition...")));
                                string script = pi.IsUnallocated 
                                    ? $"select disk {pi.DiskIndex}\ncreate partition primary\nformat quick fs=ntfs label=\"Restored\"\nassign"
                                    : $"select disk {pi.DiskIndex}\nselect partition {pi.PartitionIndex}\nformat quick fs=ntfs label=\"Restored\"\nassign";
                                
                                RunProcess("diskpart.exe", "/s \"" + CreateTempScript(script) + "\"");
                                Thread.Sleep(3000);
                                
                                // Dynamically find the newly assigned letter
                                var disks = DiskpartParser.GetDisks();
                                var newP = disks.FirstOrDefault(d => d.Index.ToString() == pi.DiskIndex)?.Partitions
                                          .FirstOrDefault(pt => pt.Type.Contains("Primary") && !string.IsNullOrEmpty(pt.DriveLetter));
                                if (newP != null) targetPath = newP.DriveLetter + ":\\";
                                preparationDone = true;
                            }
                        }

                        // Step 2: If it was already a valid partition but we need Boot Fix
                        if (!preparationDone && createBoot) {
                            bool targetDiskGPT = IsDiskGPT(pi.DiskIndex);
                            dynamicBootLetter = FindBootPartitionOnSameDisk(pi.DiskIndex, targetDiskGPT)?.TrimEnd('\\', ':') ?? "";
                            
                            if (string.IsNullOrEmpty(dynamicBootLetter)) {
                                this.Invoke(new Action(() => Log(GetStr("LOG_BootPartNotFound"))));
                                int pIdx = 0; int.TryParse(pi.PartitionIndex, out pIdx);
                                string? reconstructedBoot = FindAndFormatPrecedingBootPartition(pi.DiskIndex, pIdx, targetDiskGPT);
                                if (!string.IsNullOrEmpty(reconstructedBoot)) {
                                    dynamicBootLetter = reconstructedBoot.TrimEnd('\\', ':');
                                    if (!string.IsNullOrEmpty(newWindowsDriveAfterReconstruction)) {
                                        targetPath = newWindowsDriveAfterReconstruction;
                                        this.Invoke(new Action(() => Log("Target path updated after reconstruction: " + targetPath)));
                                    }
                                }
                            }
                        } 
                        
                        if (!preparationDone) {
                            // Standard existing partition format
                            string fs = "ntfs";
                            string label = "Win_OS";
                            if (pi.FileSystem == "FAT32" || pi.DisplayText.Contains("EFI") || pi.DisplayText.Contains("System")) {
                                fs = "fat32";
                                label = "EFI_BOOT";
                            }

                            // Dynamic letter assignment for partitions that don't have one (Part Mode)
                            if (string.IsNullOrEmpty(targetPath) || targetPath == "\\") {
                                this.Invoke(new Action(() => Log("Assigning temporary letter to target partition...")));
                                tempTargetLetter = AssignLetterViaDiskpart(pi.DiskIndex, pi.PartitionIndex);
                                targetPath = tempTargetLetter ?? "";
                            }

                            if (string.IsNullOrEmpty(targetPath) || targetPath == "\\") {
                                this.Invoke(new Action(() => Log("[!] Error: Could not assign letter to target partition.")));
                            } else {
                                this.Invoke(new Action(() => Log(string.Format(GetStr("LOG_Formatting") ?? "Formatting {0}...", targetPath))));
                                RunProcess("cmd.exe", $"/c format {targetPath.TrimEnd('\\')} /q /y /fs:{fs} /v:{label}");
                                // STABILITY DELAY: Part-Mode settle period
                                this.Invoke(new Action(() => Log("Waiting for storage stack to settle (3s)...")));
                                Thread.Sleep(3000);
                            }
                        }
                    }

                        if (string.IsNullOrEmpty(targetPath) || targetPath == "\\") {
                            this.Invoke(new Action(() => {
                                MsgBox("Could not prepare target partition automatically.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                SetUIState(false);
                            }));
                            isOperationRunning = false;
                            return;
                        }
                        
                        this.Invoke(new Action(() => Log($"Starting wimlib restoration to {targetPath}...")));
                        success = RunProcess(GetWimlibPath(), $"apply \"{wimPath}\" {selectedWimIndex.Index} \"{targetPath.TrimEnd('\\')}\" --check"); 
                        
                        if (success && createBoot) {
                            this.Invoke(new Action(() => Log(GetStr("LOG_CreatingBoot"))));
                            string bootArg = firmware == "UEFI" ? $"/f UEFI /s {dynamicBootLetter}:" : $"/f BIOS /s {dynamicBootLetter}:";
                            RunProcess("bcdboot.exe", $"\"{targetPath.TrimEnd('\\')}\\Windows\" {bootArg}");

                            // User Request (v4.13): Force Windows Boot Manager as first in UEFI BIOS Boot Menu
                            if (IsWinPE() && firmware == "UEFI") {
                                this.Invoke(new Action(() => Log("Refining UEFI boot order...")));
                                RunProcess("bcdedit.exe", "/set {fwbootmgr} displayorder {bootmgr} /addfirst");
                            }
                        }

                    // ABSOLUTE CLEANUP (Hardened): Ensure all temporary letters are removed
                    this.Invoke(new Action(() => {
                        // 1. Cleanup Boot Letter if it was temporary
                        if (createBoot && !string.IsNullOrEmpty(dynamicBootLetter)) {
                            string uc = dynamicBootLetter.TrimEnd('\\', ':').ToUpper();
                            if (uc != "C" && uc != "X") {
                                Log($"Cleaning up temporary boot letter: {uc}");
                                // Enhanced v4.16: Safe DiskIndex retrieval based on restoration mode
                                string dIdx = (selectedTarget is DiskItem d) ? d.DiskID : ((PartitionItem)selectedTarget).DiskIndex;
                                RemoveLetterViaDiskpart(dIdx, "any", uc); 
                                RemoveTempLetter(uc); // Fallback aggressive
                            }
                        }
                        // 2. Cleanup Target Letter if it was temporary
                        if (!string.IsNullOrEmpty(tempTargetLetter)) {
                            Log($"Cleaning up temporary target letter: {tempTargetLetter}");
                            // In this case, it's guaranteed to be PartitionItem because tempTargetLetter is only set in Partition Mode
                            var pi = (PartitionItem)selectedTarget;
                            RemoveLetterViaDiskpart(pi.DiskIndex, pi.PartitionIndex, tempTargetLetter);
                        }
                    }));

                    isOperationRunning = false; 
                    this.Invoke(new Action(() => {
                        Log("--------------------------------------------------");
                        if (isRestoreAborted) {
                            Log(GetStr("MSG_RestoreAborted"));
                            MsgBox(GetStr("MSG_RestoreAborted"), GetStr("UI_Aborted"), MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        } else if (success) {
                            Log(GetStr("MSG_Done"));
                            if (!chkPostAction.Checked) {
                                MsgBox(GetStr("MSG_Done"), GetStr("UI_Success"), MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                            }
                            HandlePostProcessAction();
                        } else {
                            Log(GetStr("MSG_Fail"));
                            MsgBox(GetStr("MSG_Fail"), GetStr("UI_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                        }
                    }));
                } catch (Exception ex) {
                    isOperationRunning = false;
                    this.Invoke(new Action(() => {
                        Log("Restore Fatal Error: " + ex.Message);
                        Log(GetStr("MSG_Fail"));
                        MsgBox(GetStr("MSG_Fail"), GetStr("UI_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
                    }));
                }
                this.Invoke(new Action(() => {
                    btnStartRestore.Text = GetStr("BTN_StartRestore") ?? "Start Restore";
                    SetUIState(false);
                    UpdateProgress(0, GetStr("UI_Ready"));
                }));
            }); 
        }

        private bool RunProcess(string exe, string args) 
        { 
            lock(processLock) {
                if (isBackupAborted || isRestoreAborted) return false;
            }

            try {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args) {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using (Process p = new Process { StartInfo = psi }) {
                    lock(processLock) { currentProcess = p; }
                    p.Start();

                    // CUSTOM CHARACTER-BASED STREAM READER LOOP (v3.2/v3.3)
                    // This allows detecting '\r' which tools like wimlib-imagex use for progress.
                    // V3.3 FIX: Read both Output and Error to prevent deadlock.
                    Task.Run(() => {
                        try {
                            using (StreamReader reader = p.StandardOutput) {
                                StringBuilder lineBuffer = new StringBuilder();
                                while (!reader.EndOfStream) {
                                    int nextChar = reader.Read();
                                    if (nextChar == -1) break;

                                    char c = (char)nextChar;
                                    if (c == '\r' || c == '\n') {
                                        ProcessBuffer(lineBuffer);
                                    } else {
                                        lineBuffer.Append(c);
                                    }
                                }
                                // V3.3 FIX: Final buffer flush
                                if (lineBuffer.Length > 0) ProcessBuffer(lineBuffer);
                            }
                        } catch { }
                    });

                    // V3.3 FIX: Handle StandardError to prevent deadlock
                    Task.Run(() => {
                        try {
                            using (StreamReader errReader = p.StandardError) {
                                while (!errReader.EndOfStream) {
                                    string? errLine = errReader.ReadLine();
                                    if (errLine != null) this.Invoke(new Action(() => Log("[!] " + errLine)));
                                }
                            }
                        } catch { }
                    });

                    p.WaitForExit();
                    return p.ExitCode == 0 || p.ExitCode == 3010;
                }
            } catch (Exception ex) {
                string localizedError = GetStr("ERR_FileNotFound").Replace("{0}", exe);
                this.Invoke(new Action(() => Log($"[ERROR] {localizedError}\n({ex.Message})")));
                return false;
            } finally {
                lock(processLock) { currentProcess = null; }
            }
        }

        private void ProcessBuffer(StringBuilder lineBuffer)
        {
            string data = lineBuffer.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(data))
            {
                bool isHelp = data.Contains("Bcdboot -") || data.Contains("The bcdboot.exe");
                if (!isHelp)
                {
                    this.Invoke(new Action(() => Log(TranslateDiskpartOutput(data))));
                }
            }
            lineBuffer.Clear();
        }

        private string TranslateDiskpartOutput(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return data;
            
            // Standard Diskpart Output Translation Table
            if (data.Contains("Please wait while DiskPart scans", StringComparison.OrdinalIgnoreCase)) return GetStr("LOG_DP_Scanning");
            if (data.Contains("DiskPart has finished scanning", StringComparison.OrdinalIgnoreCase)) return GetStr("LOG_DP_ScanDone");
            if (data.Contains("succeeded in creating the specified partition", StringComparison.OrdinalIgnoreCase)) return GetStr("LOG_DP_PartCreated");
            if (data.Contains("successfully formatted the volume", StringComparison.OrdinalIgnoreCase)) return GetStr("LOG_DP_Formatted");
            if (data.Contains("marked the current partition as active", StringComparison.OrdinalIgnoreCase)) return GetStr("LOG_DP_Active");
            if (data.Contains("succeeded in cleaning the disk", StringComparison.OrdinalIgnoreCase)) return GetStr("LOG_DP_Cleaned");
            
            // Regex for "Disk 0 is now the selected disk."
            var match = Regex.Match(data, @"Disk\s+(\d+)\s+is\s+now\s+the\s+selected\s+disk", RegexOptions.IgnoreCase);
            if (match.Success) return GetStr("LOG_DP_Selected").Replace("{0}", match.Groups[1].Value);

            return data;
        }

        private async void HandlePostProcessAction()
        {
            if (!chkPostAction.Checked) return;

            bool isRestart = cmbPostAction.SelectedIndex == 0;
            string actionStr = isRestart ? GetStr("UI_ACT_Restart") : GetStr("UI_ACT_Shutdown");
            
            Log($"[AUTO] {actionStr} in 2 seconds...");
            await Task.Delay(2000);

            try {
                if (isWinPE) {
                    // WinPE Specific commands
                    RunProcess("wpeutil", isRestart ? "reboot" : "shutdown");
                } else {
                    // Standard Windows commands
                    string args = (isRestart ? "/r" : "/s") + " /t 0 /f";
                    Process.Start(new ProcessStartInfo("shutdown", args) { CreateNoWindow = true, UseShellExecute = false });
                }
            } catch (Exception ex) {
                Log($"Auto action failed: {ex.Message}");
            }
        }

        private void KillProcesses()
        {
            lock (processLock) {
                if (currentProcess != null && !currentProcess.HasExited) {
                    try { currentProcess.Kill(true); } catch { }
                }
            }
            // Aggressive cleanup using taskkill (Mirroring legacy)
            try {
                ProcessStartInfo psi = new("taskkill.exe", "/F /IM wimlib-imagex.exe /T") { CreateNoWindow = true, UseShellExecute = false };
                Process.Start(psi); // No wait to speed up exit
                
                psi.Arguments = "/F /IM DriverIndexer.exe /T";
                Process.Start(psi);
                
                psi.Arguments = "/F /IM diskpart.exe /T";
                Process.Start(psi);
            } catch { }
        }

        private string? AssignLetterViaDiskpart(string diskIdx, string partIdx)
        {
            string? letter = GetAvailableDriveLetter();
            if (string.IsNullOrEmpty(letter)) return null;

            string script = $"select disk {diskIdx}\r\nselect partition {partIdx}\r\nassign letter={letter}\r\nexit";
            string scriptPath = Path.Combine(Path.GetTempPath(), $"assign_{DateTime.Now.Ticks}.txt");
            File.WriteAllText(scriptPath, script);
            
            RunProcess("diskpart.exe", $"/s \"{scriptPath}\"");
            if (File.Exists(scriptPath)) try { File.Delete(scriptPath); } catch { }
            
            Thread.Sleep(1000); // Wait for mounting
            if (Directory.Exists(letter + ":\\")) {
                Log($"Diskpart: Letter {letter}: assigned to Disk {diskIdx} Part {partIdx}");
                return letter + ":\\";
            }
            return null;
        }

        private void RemoveLetterViaDiskpart(string diskIdx, string partIdx, string letter)
        {
            try {
                if (string.IsNullOrEmpty(letter)) return;
                string cleanLetter = letter.Substring(0, 1); // "S"
                string script = $"select disk {diskIdx}\r\nselect partition {partIdx}\r\nremove letter={cleanLetter}\r\nexit";
                string scriptPath = Path.Combine(Path.GetTempPath(), $"remove_{DateTime.Now.Ticks}.txt");
                File.WriteAllText(scriptPath, script);
                RunProcess("diskpart.exe", $"/s \"{scriptPath}\"");
                if (File.Exists(scriptPath)) try { File.Delete(scriptPath); } catch { }
            } catch { }
        }

        private string? AssignTempLetterToGUID(string guidPath)
        {
            try {
                string? letter = GetAvailableDriveLetter();
                if (string.IsNullOrEmpty(letter)) return null;

                using ManagementObjectSearcher s = new("SELECT * FROM Win32_Volume WHERE DeviceID='" + guidPath.Replace("\\", "\\\\") + "'");
                foreach (ManagementObject vol in s.Get().Cast<ManagementObject>()) {
                    try {
                        vol["DriveLetter"] = letter + ":";
                        vol.Put();
                        Thread.Sleep(1000); // Wait for OS to mount
                        if (Directory.Exists(letter + ":\\")) {
                            Log($"Temporary letter {letter}: assigned to {guidPath}");
                            return letter + ":\\";
                        }
                    } catch { } 
                    finally { vol.Dispose(); }
                }
            } catch (Exception ex) { Log("Temp Letter Error: " + ex.Message); }
            return null;
        }

        private void RemoveTempLetter(string letter)
        {
            try {
                if (string.IsNullOrEmpty(letter) || letter.Length < 1) return;
                string drive = letter.Substring(0, 1).ToUpper() + ":"; // e.g., "S:"
                
                Log($"Aggressive cleanup for temporary letter {drive}...");

                // 1. Un-assign using Diskpart (more aggressive than mountvol)
                string diskIdx = GetDiskIndexForDrive(drive) ?? "-1";
                if (diskIdx != "-1") {
                    string partIdx = GetPartitionIndexForDrive(drive);
                    if (partIdx != "-1") {
                        bool isGPT = IsDiskGPT(diskIdx);
                        string script = $"select disk {diskIdx}\nselect partition {partIdx}\nremove letter={drive.Substring(0, 1)}\n";
                        if (isGPT) script += "gpt attributes=0x8000000000000001";
                        else script += "set id=1C override";
                        
                        RunProcess("diskpart.exe", "/s \"" + CreateTempScript(script) + "\"");
                    }
                }

                // 2. Secondary cleanup with mountvol
                try {
                    Process p = new Process();
                    p.StartInfo.FileName = "mountvol.exe";
                    p.StartInfo.Arguments = $"{drive} /D";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    p.WaitForExit();
                } catch { }
                
                Log($"Temporary letter {drive} removal finalized.");
            } catch (Exception ex) { Log("Error removing temp letter: " + ex.Message); }
        }

        private void SetUIState(bool isRunning)
        {
            // Center UI Locking Logic (Industrial Standard)
            bool enable = !isRunning;

            // Loop through each TabPage to disable contents without disabling the buttons or the TabControl itself
            foreach (TabPage tp in tcMain.TabPages) {
                foreach (Control ctrl in tp.Controls) {
                    // Prevent disabling the main buttons that act as "Abort" buttons
                    if (ctrl == btnStartBackup || ctrl == btnStartRestore) continue;
                    
                    // Disable all other configuration inputs/panels
                    ctrl.Enabled = enable;
                }
            }

            cbLang.Enabled = enable;
            btnClearLog.Enabled = enable;

            // EXCEPTIONS: Per user request, keep these active
            btnAbout.Enabled = true; 
            lnkGithub.Enabled = true;
            lnkWeb.Enabled = true;

            // Footer buttons specific logic: Ensure the running operation's button is ALWAYS enabled (as Abort)
            // but the other operation is disabled to prevent concurrent issues.
            if (isRunning) {
                if (isBackupRunning) {
                    btnStartBackup.Enabled = true; // For Abort
                    btnStartRestore.Enabled = false;
                }
                if (isOperationRunning) {
                    btnStartRestore.Enabled = true; // For Abort
                    btnStartBackup.Enabled = false;
                }
            } else {
                btnStartBackup.Enabled = true;
                btnStartRestore.Enabled = true;
            }
        }
        private DialogResult MsgBox(string text, string title = "", MessageBoxButtons btns = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information, MessageBoxDefaultButton defBtn = MessageBoxDefaultButton.Button1, MessageBoxOptions options = 0)
        {
            // Fix: ServiceNotification and DefaultDesktopOnly cannot be combined with an owner window. 
            // We must strip these flags when an owner is provided to prevent Win32 exceptions.
            options &= ~(MessageBoxOptions.ServiceNotification | MessageBoxOptions.DefaultDesktopOnly);

            DialogResult result = DialogResult.None;
            Action act = () => {
                string t = string.IsNullOrEmpty(title) ? (GetStr("UI_Info") ?? "Info") : title;
                bool origTop = this.TopMost;
                this.TopMost = true;
                result = MessageBox.Show(this, text, t, btns, icon, defBtn, options);
                this.TopMost = origTop;
            };
            if (this.InvokeRequired) this.Invoke(new Action(act)); else act();
            return result;
        }

        private void Log(string m)
        {
            if (rtbLog == null || rtbLog.IsDisposed) return;
            if (rtbLog.InvokeRequired) {
                rtbLog.BeginInvoke(new Action(() => Log(m)));
                return;
            }
            if (!rtbLog.IsHandleCreated) return;

            // Progress Parsing (Legacy Sync)
            Match match = Regex.Match(m, @"(\d+\.?\d*)\%");
            if (match.Success) {
                if (double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double val)) {
                    int p = (int)Math.Min(val, 100);
                    UpdateProgress(p, p + "%", isBackupRunning ? ColorBackup : ColorRestore);
                }
            }

            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {m}\n");
            rtbLog.ScrollToCaret();
        }
        private void UpdateProgress(int value, string status, Color? barColor = null)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired) { 
                this.BeginInvoke(new Action(() => UpdateProgress(value, status, barColor))); 
                return; 
            }
            if (!this.IsHandleCreated) return;
            
            // FORCE Global Color Preference (REMOVED OPTIMIZATION TO ENSURE STABILITY)
            Color effectColor = barColor ?? ColorRestore;

            // Progress Bar Style and Color (Stability: Style must be set BEFORE color)
            if (value == -1) {
                if (pbMain.Style != ProgressBarStyle.Marquee) pbMain.Style = ProgressBarStyle.Marquee;
            } else {
                if (pbMain.Style != ProgressBarStyle.Blocks) pbMain.Style = ProgressBarStyle.Blocks;
                pbMain.Value = Math.Max(0, Math.Min(100, value));
            }

            // To apply ForeColor/PBM_SETBARCOLOR on modern Windows, we must disable the 'Aero' look
            SetWindowTheme(pbMain.Handle, "", "");
            // PBM_SETBARCOLOR (0x0409) supports ColorRef
            int colorCode = (effectColor.B << 16) | (effectColor.G << 8) | effectColor.R;
            SendMessage(pbMain.Handle, PBM_SETBARCOLOR, 0, (IntPtr)colorCode);
            pbMain.ForeColor = effectColor; // Fallback sync

            lblProgressStatus.Text = status;
            pbMain.Update(); // Force immediate draw
            lblProgressStatus.Refresh();
        }

        #region [V4.1 POWER MANAGEMENT]
        // Ultimate Performance GUID (hidden built-in scheme, available on Windows 10 1803+)
        private const string GuidUltimatePerf = "e9a42b02-d5df-448d-aa00-03f14749eb61";
        // High Performance GUID (always present — reliable WinPE fallback)
        private const string GuidHighPerf     = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

        /// <summary>
        /// Saves the currently active power scheme, then activates Ultimate Performance.
        /// Falls back to High Performance when Ultimate is unavailable (some WinPE builds).
        /// Called synchronously from OnShown — fast enough (~300ms) to not block the UI noticeably.
        /// </summary>
        private void CaptureAndSetUltimatePerformance()
        {
            try {
                // Step 1 — Save the current scheme GUID so we can restore it on exit
                var res = RunCommand("powercfg", "/getactivescheme");
                var m = Regex.Match(res.Output, @"Power Scheme GUID:\s*([a-f0-9\-]+)", RegexOptions.IgnoreCase);
                if (m.Success) _originalPowerSchemeGuid = m.Groups[1].Value.Trim();

                if (IsWinPE()) {
                    // Step 2 (WinPE) — Directly use built-in High Performance GUID
                    // Duplicating schemes in WinPE can trigger unstable hardware enumeration/resets
                    RunCommand("powercfg", $"/setactive {GuidHighPerf}");
                } else {
                    // Step 2 (Online) — Unlock Ultimate Performance
                    RunCommand("powercfg", $"-duplicatescheme {GuidUltimatePerf}");
                    RunCommand("powercfg", $"/setactive {GuidUltimatePerf}");
                }

                // Step 3 — Verify activation
                var verify = RunCommand("powercfg", "/getactivescheme");
                bool ultimateActive = verify.Output.IndexOf(GuidUltimatePerf, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!ultimateActive) {
                    // Fallback: High Performance is always present in every WinPE and Online build
                    RunCommand("powercfg", $"/setactive {GuidHighPerf}");
                }
            } catch { /* Power management errors must never crash the app */ }
        }

        /// <summary>
        /// Restores the power scheme that was active before the application launched.
        /// Called automatically from MainForm_FormClosing — safe to call even if startup failed.
        /// </summary>
        private void RestoreOriginalPowerScheme()
        {
            try {
                if (!string.IsNullOrEmpty(_originalPowerSchemeGuid)) {
                    RunCommand("powercfg", $"/setactive {_originalPowerSchemeGuid}");
                }
            } catch { }
        }
        #endregion

        private string GetSystemToolPath(string toolName)
        {
            string system32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32");
            string tool = Path.Combine(system32, toolName);

            // WOW64 Redirection Bypass: If 32-bit app on 64-bit OS, System32 points to SysWOW64
            // But SysWOW64 doesn't have tools like PnPUtil or DISM in some cases.
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                string sysnative = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative");
                string nativeTool = Path.Combine(sysnative, toolName);
                if (File.Exists(nativeTool)) return nativeTool;
            }

            return File.Exists(tool) ? tool : toolName; // Fallback to just toolName (rely on PATH)
        }

        private CommandResult RunCommand(string exe, string args)
        {
            CommandResult result = new CommandResult();
            try {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args) {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };
                using (Process? p = Process.Start(psi)) {
                    if (p == null) return result;
                    
                    // REVERT TO SYNCHRONOUS ReadToEnd (v3.3)
                    // This is essential for parsing tool output (e.g. DriverIndexer list) reliably.
                    string output = p.StandardOutput.ReadToEnd();
                    string error = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    
                    result.Output = string.IsNullOrEmpty(output) ? error : output;
                    result.ExitCode = p.ExitCode;
                    return result;
                }
            } catch (Exception ex) {
                result.Output = "Error: " + ex.Message;
                result.ExitCode = -99;
                return result;
            }
        }
        private class CommandResult { 
            public string Output { get; set; } = ""; 
            public int ExitCode { get; set; } = -1; 
        }

        private string GetStr(string k) {
            // Hardcoded Fallbacks for essential keys (prevents raw key display if ini is missing them)
            if (!langStrings.ContainsKey(k)) {
                if (k == "LOG_Scanning") return currentLang == "tr" ? "Sistem yapılandırması taranıyor, lütfen bekleyin..." : "Scanning system configuration, please wait...";
                if (k == "LOG_Scanning_Done") return currentLang == "tr" ? "Sistem taraması tamamlandı." : "System scanning complete.";
            }
            string val = langStrings.GetValueOrDefault(k, k);
            return val.Replace("\\n", Environment.NewLine);
        }

        private void SetText(Control? c, string key) {
            if (c == null) return;
            c.Text = GetStr(key);
        }

        private string GetLanguageDir()
        {
            // Discover Language Folder
            string? currentDir = Application.StartupPath;
            string[] localPaths = {
                Path.Combine(Application.StartupPath, "bin", "Lang"),
                Path.Combine(Application.StartupPath, "AppFiles", "Lang"),
                Path.Combine(Application.StartupPath, "Lang")
            };

            foreach (var path in localPaths) {
                if (Directory.Exists(path) && Directory.GetFiles(path, "*.ini").Length > 0) return path;
            }

            // Reliability Fallback: Climb up
            while (currentDir != null) {
                string[] climbPaths = {
                    Path.Combine(currentDir, "AppFiles", "Lang"),
                    Path.Combine(currentDir, "RecoveryTool", "AppFiles", "Lang")
                };
                foreach (var path in climbPaths) {
                    if (Directory.Exists(path) && Directory.GetFiles(path, "*.ini").Length > 0) return path;
                }
                var parent = Directory.GetParent(currentDir);
                if (parent == null) break;
                currentDir = parent.FullName;
            }
            return "";
        }

        private void LoadAvailableLanguages()
        {
            cbLang.Items.Clear();
            string dir = GetLanguageDir();
            if (string.IsNullOrEmpty(dir)) return;

            // Target Priority: 1. Registry (already in currentLang via LoadSettings call site)
            // But we need to detect OS language first as a fallback
            string osLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
            
            foreach (var file in Directory.GetFiles(dir, "*.ini")) {
                string code = Path.GetFileNameWithoutExtension(file).ToLower();
                string displayName = code.ToUpper(); 

                try {
                    foreach (var line in File.ReadAllLines(file, Encoding.UTF8)) {
                        if (line.Trim().StartsWith("UI_LANG_NAME=")) {
                            displayName = line.Split(new[] { '=' }, 2)[1].Trim();
                            break;
                        }
                    }
                } catch { }

                var item = new LanguageItem { DisplayName = displayName, Code = code };
                cbLang.Items.Add(item);
            }
        }

        private void UpdateUILanguage() 
        { 
            try {
                // 1. Mandatory UI updates
                string dir = GetLanguageDir();
                string f = Path.Combine(dir, $"{currentLang}.ini");

                if (string.IsNullOrEmpty(dir) || !File.Exists(f)) { 
                    CalculateLayout();
                    return; 
                }

                langStrings.Clear(); 
                foreach (var l in File.ReadAllLines(f, Encoding.UTF8)) {
                    string line = l.Trim();
                    if (line.Contains("=") && !line.StartsWith("#") && !line.StartsWith(";")) { 
                        var s = line.Split(new[] { '=' }, 2); 
                        langStrings[s[0].Trim()] = s[1].Trim();
                    } 
                }

                // 3. UI Label Updates
                SetText(lblHeader, "UI_Header");
                SetText(tpBackup, "UI_Backup");
                SetText(tpRestore, "UI_Restore");
                SetText(tpBootFix, "UI_BootFix");
                SetText(tpSettings, "UI_LangHelp");
                
                if (lblHelpInfo != null) lblHelpInfo.Text = GetStr("UI_HelpText").Replace("\\n", Environment.NewLine);

                SetText(lblSourcePart, "UI_LBL_Source");
                SetText(lblCompression, "UI_LBL_Compression");
                if (cbCompression.SelectedIndex == 2) SetText(lblBackupDest, "UI_LBL_DestESD");
                else SetText(lblBackupDest, "UI_LBL_DestWIM");
                
                SetText(btnBrowseBackup, "UI_BTN_Browse");
                SetText(btnBrowseWim, "UI_BTN_Browse");
                SetText(btnStartBackup, "UI_START_BACKUP");
                SetText(btnDriverBackup, "UI_BTN_DriverBackup");
                SetText(btnDriverRestore, "UI_BTN_DriverRestore");
                SetText(lblWimPath, "UI_LBL_WimPath");
                SetText(lblWimIndex, "UI_LBL_WimIndex");
                SetText(gbStrategy, "UI_GB_Strategy");
                SetText(rbPartRestore, "UI_RB_PartOnly");
                SetText(rbWholeDisk, "UI_RB_DiskRestore");
                SetText(lblTarget, "UI_LBL_Target");
                SetText(chkCreateBoot, "UI_CHK_Boot");
                SetText(gbBoot, "UI_GB_Boot");
                SetText(gbPartitionLayout, "UI_GB_Layout");
                SetText(btnStartRestore, "UI_START_RESTORE");
                
                SetText(lblBootFixTitle, "UI_LBL_BootFixTitle");
                lblBootFixInfo.Text = (GetStr("UI_LBL_BootFixInfo") ?? "").Replace("\\n", Environment.NewLine);
                SetText(btnAutoBootFix, "UI_BTN_BootFix");
                SetText(lblBootFixDesc, "UI_LBL_BootFixDesc");
                SetText(btnHealthCheck, "UI_BTN_HealthCheck");
                SetText(lblHealthCheckDesc, "UI_LBL_HealthCheckInfo");


                // Detected OS label — updated on language change too (for the "not found" string)
                lblInstalledOS.Text = GetDetectedOSLabel();
                
                // --- POST ACTION (AUTO SHUTDOWN/RESTART) ---
                chkPostAction.Text = GetStr("UI_OnCompletion");
                int sIdx = cmbPostAction.SelectedIndex;
                cmbPostAction.Items.Clear();
                cmbPostAction.Items.Add(GetStr("UI_ACT_Restart"));
                cmbPostAction.Items.Add(GetStr("UI_ACT_Shutdown"));
                cmbPostAction.SelectedIndex = sIdx >= 0 ? sIdx : 0;
                
                // LogBootMode() removed from here to prevent redundant logs on language change
                SetText(lblLang, "UI_LBL_Lang");
                SetText(btnAbout, "UI_About");
                SetText(btnClearLog, "UI_BTN_ClearLog");

                lblBootSize.Text = GetStr("UI_BootSize");
                lblWinSize.Text = GetStr("UI_WindowsSize");
                chkCreateRecovery.Text = GetStr("UI_CreateRecovery");

                UpdateLayoutUI();
                CalculateLayout();
                tcMain.Invalidate(); 
                
                // Do NOT clear log or re-log everything on language change - prevents duplicates
                if (isInitialized) {
                    UpdateLayoutUI(); // Just refresh layout
                }

                if (pbMain.Value == 0) lblProgressStatus.Text = GetStr("UI_Ready");
            } catch (Exception ex) {
                Log("UpdateUILanguage Error: " + ex.Message);
            }
        }

        private void btnClearLog_Click(object sender, EventArgs e) { rtbLog.Clear(); }
        private void CmbRestoreTarget_SelectedIndexChanged(object? sender, EventArgs e) { if (cmbRestoreTarget.SelectedItem is DiskItem d) { currentDiskSizeGB = (long)d.SizeGB; numWinSizeGB.Maximum = currentDiskSizeGB; CalculateLayout(); } }
        public class DiskPrepResult { public string WinPath { get; set; } = ""; public string BootLetter { get; set; } = ""; }
        public class PartitionItem {
            public string DisplayText { get; set; } = "";
            public string DrivePath { get; set; } = "";
            public string DiskIndex { get; set; } = "0";
            public string PartitionIndex { get; set; } = "1";
            public string FileSystem { get; set; } = "NTFS"; // Keep for restoration fix compatibility
            public bool HasLetter { get; set; }
            public long SizeBytes { get; set; }
            public bool IsUnallocated { get; set; }
            public bool IsUnformatted { get; set; }
            public override string ToString() => DisplayText;
        }

        public class DiskItem {
            public string DisplayText { get; set; } = "";
            public string DiskID { get; set; } = "0";
            public long SizeBytes { get; set; }
            public double SizeGB { get; set; }
            public override string ToString() => DisplayText;
        }
        public class WimIndexItem { public string Index { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public override string ToString() => $"{Index}: {Name}"; }
        public class LanguageItem { public string DisplayName { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; public override string ToString() => DisplayName; }
        private void SaveSettings() 
        { 
            try { 
                using (var k = Registry.CurrentUser.CreateSubKey(@"Software\WindowsBackupRestoreTool")) {
                    k.SetValue("Language", currentLang); 
                }
            } catch { } 
        }

        private void LoadSettings() 
        { 
            string? savedLang = null;
            try { 
                using (var k = Registry.CurrentUser.OpenSubKey(@"Software\WindowsBackupRestoreTool")) {
                    if (k != null) {
                        savedLang = k.GetValue("Language")?.ToString(); 
                    }
                }
            } catch { } 

            string osLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
            string dir = GetLanguageDir();

            // 1. Registry Check
            if (!string.IsNullOrEmpty(savedLang) && File.Exists(Path.Combine(dir, $"{savedLang}.ini"))) {
                currentLang = savedLang;
            }
            // 2. OS Language Check
            else if (File.Exists(Path.Combine(dir, $"{osLang}.ini"))) {
                currentLang = osLang;
            }
            // 3. Fallback to English
            else {
                currentLang = "eng";
            }
            
            // Apply language to UI
            UpdateUILanguage();

            // Sync ComboBox
            for (int i = 0; i < cbLang.Items.Count; i++) {
                if (cbLang.Items[i] is LanguageItem item && item.Code.Equals(currentLang, StringComparison.OrdinalIgnoreCase)) {
                    cbLang.SelectedIndex = i;
                    break;
                }
            }
        }
        #endregion
        private class DiskPartInfo
        {
            public int Index { get; set; }
            public string Type { get; set; } = string.Empty;
            public long SizeMB { get; set; }
        }

        private List<DiskPartInfo> GetDiskPartitions(string diskIndex)
        {
            List<DiskPartInfo> list = new List<DiskPartInfo>();
            try
            {
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

                using (StringReader sr = new StringReader(output))
                {
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Trim().StartsWith("-") || string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length < 3) continue;

                            int idx;
                            if (!int.TryParse(parts[1], out idx)) continue;

                            string type = parts[2];
                            long sizeVal = 0;
                            for (int i = 2; i < parts.Length - 1; i++)
                            {
                                string unit = parts[i + 1].ToUpper();
                                if (unit == "MB" || unit == "GB" || unit == "KB")
                                {
                                    long val = 0;
                                    if (long.TryParse(parts[i], out val))
                                    {
                                        if (unit == "GB") val *= 1024;
                                        else if (unit == "KB") val /= 1024;
                                        if (val > sizeVal) sizeVal = val;
                                    }
                                }
                            }
                            list.Add(new DiskPartInfo { Index = idx, Type = type, SizeMB = sizeVal });
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return list;
        }

        private bool IsDiskGPT(string diskIndex)
        {
            try
            {
                string scriptPath = Path.Combine(Path.GetTempPath(), "check_gpt.txt");
                File.WriteAllText(scriptPath, "list disk");
                
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
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Trim().StartsWith("Disk " + diskIndex))
                        {
                            return line.TrimEnd().EndsWith("*");
                        }
                    }
                }
            }
            catch { }
            return true; // Modern systems fallback
        }

        private string? FindBootPartitionOnSameDisk(string diskIndex, bool isGPT)
        {
            try
            {
                using ManagementObjectSearcher searcher = new($"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='\\\\.\\PHYSICALDRIVE{diskIndex}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
                var partitions = searcher.Get().Cast<ManagementObject>()
                                 .OrderBy(p => Convert.ToUInt32(p["Index"] ?? 0))
                                 .ToList();

                foreach (ManagementObject part in partitions)
                {
                    string partType = part["Type"]?.ToString() ?? "";
                    ulong size = Convert.ToUInt64(part["Size"] ?? 0);
                    uint index = Convert.ToUInt32(part["Index"] ?? 0);
                    
                    bool isBootPartition = false;
                    try { isBootPartition = Convert.ToBoolean(part["BootPartition"]); } catch { }

                    // Priority Candidate Analysis (Rule: Hidden <1GB partition at the start is usually the boot one)
                    bool isBootCandidate = (isGPT ? partType.Contains("System") : isBootPartition) ||
                                          (size < 1073741824 && index < 2);

                    if (isBootCandidate)
                    {
                        string devID = part["DeviceID"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(devID)) continue;

                        using ManagementObjectSearcher logSearch = new($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{devID}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                        
                        ManagementObjectCollection logDisks = logSearch.Get();
                        if (logDisks.Count > 0)
                        {
                            foreach (ManagementObject vol in logDisks)
                            {
                                string? dID = vol["DeviceID"]?.ToString();
                                if (dID != null) return dID + "\\";
                            }
                        }
                        else
                        {
                            string? tempLetter = GetAvailableDriveLetter();
                            if (!string.IsNullOrEmpty(tempLetter)) {
                                Log($"Hidden boot partition detected on Disk {diskIndex}. Assigning temporary letter {tempLetter}:...");
                                string partNum = (Convert.ToInt32(part["Index"] ?? 0) + 1).ToString();
                                string script = $"select disk {diskIndex}\nselect partition {partNum}\nassign letter={tempLetter}";
                                
                                if (!RunProcess("diskpart.exe", "/s \"" + CreateTempScript(script) + "\"")) {
                                    // FORCE MOUNT (Rule Parity): If standard assign fails on MBR, try setting ID to NTFS (07)
                                    if (!isGPT) {
                                        Log("Standard assignment failed. Forcing MBR ID to 07 (NTFS) for mounting...");
                                        string forceScript = $"select disk {diskIndex}\nselect partition {partNum}\nset id=07 override\nassign letter={tempLetter}";
                                        RunProcess("diskpart.exe", "/s \"" + CreateTempScript(forceScript) + "\"");
                                    }
                                }

                                if (Directory.Exists(tempLetter + ":\\")) return tempLetter + ":\\";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            { 
                Log("WMI error, using Diskpart fallback for boot detection: " + ex.Message);
                try {
                    return DiskpartParser.FindBootPartition(diskIndex, isGPT);
                } catch { }
            }
            return null;
        }

        private string? FindAndFormatPrecedingBootPartition(string diskIndex, int targetPartIndex, bool useGPT)
        {
            try
            {
                Log("Starting FULL Reconstruction Logic for Disk " + diskIndex + ", Target Index " + targetPartIndex);
                var allPartitions = GetDiskPartitions(diskIndex);
                var targetPartition = allPartitions.FirstOrDefault(p => p.Index == targetPartIndex);
                
                // 1. Delete Target
                Log("Deleting Target Partition: " + targetPartIndex);
                string delTargetScript = "select disk " + diskIndex + "\nselect partition " + targetPartIndex + "\ndelete partition override";
                RunProcess("diskpart.exe", "/s \"" + CreateTempScript(delTargetScript) + "\"");

                // 2. Delete Preceding
                for (int idx = targetPartIndex - 1; idx >= 1; idx--)
                {
                    Log("Deleting Preceding Partition: " + idx);
                    string delPrecScript = "select disk " + diskIndex + "\nselect partition " + idx + "\ndelete partition override";
                    RunProcess("diskpart.exe", "/s \"" + CreateTempScript(delPrecScript) + "\"");
                }

                // 3. Rescan
                RunProcess("diskpart.exe", "/s \"" + CreateTempScript("rescan") + "\"");
                Thread.Sleep(3000);

                // 4. Create fresh layout
                string bootLabel = useGPT ? "EFI_BOOT" : "MBR_BOOT";
                string? dynamicBootLet = GetAvailableDriveLetter();
                // In WinPE, strongly prefer C for Windows if available
                string? dynamicWinLet = GetAvailableDriveLetter("C", new List<string> { dynamicBootLet ?? "" });

                if (string.IsNullOrEmpty(dynamicBootLet) || string.IsNullOrEmpty(dynamicWinLet)) return null;

                string script;
                if (useGPT)
                {
                    script = string.Format(
                        "select disk {0}\ncreate partition efi size=200\nformat quick fs=fat32 label=\"{1}\" override\nassign letter={2}\n" +
                        "create partition primary\nformat quick fs=ntfs label=\"Win_OS\" override\nassign letter={3}",
                        diskIndex, bootLabel, dynamicBootLet, dynamicWinLet
                    );
                }
                else
                {
                    script = string.Format(
                        "select disk {0}\ncreate partition primary size=200\nactive\nformat quick fs=fat32 label=\"{1}\" override\nassign letter={2}\n" +
                        "create partition primary\nformat quick fs=ntfs label=\"Win_OS\" override\nassign letter={3}",
                        diskIndex, bootLabel, dynamicBootLet, dynamicWinLet
                    );
                }

                if (RunProcess("diskpart.exe", "/s \"" + CreateTempScript(script) + "\""))
                {
                    Thread.Sleep(2000);
                    // Force Windows -> C: if free
                    if (!Directory.Exists("C:\\"))
                    {
                        Log("Reassigning Windows partition to C:...");
                        string reassignScript = string.Format("select volume {0}\nremove\nassign letter=C", dynamicWinLet);
                        RunProcess("diskpart.exe", "/s \"" + CreateTempScript(reassignScript) + "\"");
                        newWindowsDriveAfterReconstruction = "C:\\";
                    }
                    else
                    {
                        newWindowsDriveAfterReconstruction = dynamicWinLet + ":\\";
                    }
                    return dynamicBootLet + ":\\";
                }
            }
            catch (Exception ex) { Log("Reconstruction Error: " + ex.Message); }
            return null;
        }

        private string CreateTempScript(string content)
        {
            string path = Path.Combine(Path.GetTempPath(), "dp_script_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".txt");
            File.WriteAllText(path, content);
            return path;
        }

        private void RunProcess(string filename, string args, bool wait = true)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = filename;
                p.StartInfo.Arguments = args;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                if (wait) p.WaitForExit();
            }
            catch { }
        }

        #region Driver Management

        #region Helper: External Tool Path Resolver
        private string GetToolPath(string exeName)
        {
            // Primary path: app_dir\bin\tool.exe
            string binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", exeName);
            if (File.Exists(binPath)) return binPath;

            string path2 = Path.Combine(Application.StartupPath, "bin", exeName);
            if (File.Exists(path2)) return path2;

            // Fallback to current dir
            return exeName; 
        }
        #endregion

        #region Helper: External Tool Runner with Progress Sync
        private int RunToolWithProgress(string fileName, string args, out int detectedSuccesses, bool quiet = false, int forcedTotal = 0, Color? barColor = null, bool skipReset = false)
        {
            int exitCode = -1;
            int successes = 0;
            int importTotal = forcedTotal; 
            int importCurrent = 0;
            string actionLabel = (importTotal > 0 || forcedTotal > 0) ? GetStr("UI_Restore") : ""; 

            // v3.3: Skip progress bar reset if called from an outer operation loop
            if (!skipReset) {
                UpdateProgress(0, GetStr("UI_Ready"), barColor ?? ColorRestore);
            }
            
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.Arguments = args;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.StandardOutputEncoding = Encoding.UTF8;

                    process.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            string line = e.Data.Trim();
                            
                            // Log only if not quiet or if it looks like progress/important info
                            // NEW: Anti-spam filter for Diagnostic List
                            if (!quiet || line.Contains("[") && line.Contains("]"))
                            {
                                // Filtered logging for diagnostics: only show drivers containing 'oem'
                                if (line.Contains("DEBUG: DIAGNOSTIC")) Log(line);
                                else if (fileName.Contains("DriverIndexer") && args.Contains("list") && !line.Contains(".inf", StringComparison.OrdinalIgnoreCase)) {
                                     // Skip non-driver lines in list
                                }
                                else {
                                    Log(line);
                                }
                            }

                            // 1. Standard Progress Regex: Match [ 1/59 ] or [4/10] etc.
                            var match = System.Text.RegularExpressions.Regex.Match(line, @"\[\s*(\d+)\s*/\s*(\d+)\s*\]");
                            if (match.Success)
                            {
                                int current = int.Parse(match.Groups[1].Value);
                                int total = int.Parse(match.Groups[2].Value);
                                string label = $"{current}/{total}";
                                int percent = (int)((double)current / total * 100);
                                UpdateProgress(percent, label, barColor);
                            }
                            
                            // 2. Import-Style Progress: Detect "Found X (drivers|devices)"
                            var matchFound = System.Text.RegularExpressions.Regex.Match(line, @"Found\s+(\d+)\s+(drivers|devices)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (matchFound.Success)
                            {
                                int.TryParse(matchFound.Groups[1].Value, out importTotal);
                                importCurrent = 0;
                            }

                            // 3. Import-Style Progress: Detect per-driver results (Highly Robust Regex)
                            int totalToUse = Math.Max(importTotal, forcedTotal);
                            var matchDriver = System.Text.RegularExpressions.Regex.Match(line, @"(Success|Error|Installing|Geri|Yedekleme|Yedekle|Backup|Restore|Geri\s*Yükleme|Importing|Processing|Applying|Creating).*\.(inf|sys|exe)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            
                            if (totalToUse > 0 && matchDriver.Success)
                            {
                                importCurrent++;
                                string drvName = line.Split(':').Last().Trim();
                                
                                // FORCE 1/9 Design Log for Console and Log
                                string progressLabel = string.Format("[{0}/{1}]", importCurrent, totalToUse);
                                Log(string.Format("{0} {1}: {2}", progressLabel, actionLabel, drvName));
                                successes++;
                                
                                int percent = (int)((double)importCurrent / totalToUse * 100);
                                UpdateProgress(percent, string.Format("{0}/{1}", importCurrent, totalToUse), barColor ?? ColorRestore);
                            }
                        }
                    };

                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Log("[!] " + e.Data.Trim());
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                Log("[!] Tool Runner Error: " + ex.Message);
            }
            detectedSuccesses = successes;
            return exitCode;
        }
        #endregion

        private class DriverInfoItem
        {
            public string PublishedName { get; set; } = string.Empty;
            public string OriginalName { get; set; } = string.Empty;
            public string Provider { get; set; } = string.Empty;
            public string Class { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string Date { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;

            public override string ToString()
            {
                string name = string.IsNullOrEmpty(Description) ? OriginalName : Description;
                return $"{name} ({Provider}) [{PublishedName}]";
            }
        }

        private void btnDriverBackup_Click(object sender, EventArgs e)
        {
            if (isOperationRunning) return;
            SetUIState(true);
            UpdateProgress(-1, GetStr("LOG_Driver_Scanning"));

            Task.Run(() => {
                try {
                    List<DriverInfoItem> drivers = ExtractDrivers();
                    
                    this.Invoke(new Action(() => {
                        SetUIState(false);
                        if (drivers.Count == 0) {
                            MsgBox(GetStr("MSG_NoDriversFound"), GetStr("UI_Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            UpdateProgress(0, GetStr("UI_Ready"));
                            return;
                        }

                        List<DriverInfoItem> selected = ShowDriverSelector(drivers);
                        if (selected == null || selected.Count == 0) {
                            UpdateProgress(0, GetStr("UI_Ready"));
                            return;
                        }

                        using (var fbd = new FolderBrowserDialog { Description = GetStr("MSG_SelectFolder_Backup") }) {
                            if (fbd.ShowDialog() == DialogResult.OK) {
                                isOperationRunning = true; // LOCK IMMEDIATELY IN UI THREAD
                                string destDir = fbd.SelectedPath;
                                Task.Run(() => RunDriverBackup(selected, destDir));
                            } else {
                                UpdateProgress(0, GetStr("UI_Ready"));
                            }
                        }
                    }));
                } catch (Exception ex) {
                    this.Invoke(new Action(() => {
                        Log("Error during driver backup prep: " + ex.Message);
                        SetUIState(false);
                    }));
                }
            });
        }

        private List<DriverInfoItem> ExtractDrivers()
        {
            List<DriverInfoItem> list = new List<DriverInfoItem>();
            string drvIndexer = GetToolPath("DriverIndexer.exe");

            if (!File.Exists(drvIndexer)) {
                Log("[!] Error: DriverIndexer.exe not found. Cannot scan drivers.");
                return list;
            }

            string targetDrive = IsWinPE() ? GetOfflineWindowsDrive() : GetSystemDrive();

            if (IsWinPE() && string.IsNullOrEmpty(targetDrive)) {
                this.Invoke(new Action(() => {
                   MsgBox(GetStr("MSG_WindowsNotFound_DriverBackup"), GetStr("UI_Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }));
                return list;
            }

            if (string.IsNullOrEmpty(targetDrive)) targetDrive = GetSystemDrive();

            // Strip trailing backslash BEFORE quoting.
            // "C:\" inside a quoted arg causes Windows arg-parser to treat \" as an escaped quote.
            // DriverIndexer would then receive the broken path  C:"  instead of  C:\.
            // Correct form: "C:" — DriverIndexer resolves the root internally.
            string quotedTarget = targetDrive.TrimEnd('\\', '/');

            Log(string.Format("Scanning drivers on {0}\\ using DriverIndexer Engine...", quotedTarget));

            // Run list command WITHOUT -s by default to match user's manual command and filtered output
            var cmd = RunCommand(drvIndexer, $"list \"{quotedTarget}\"");
            string output = cmd.Output;

            if (string.IsNullOrEmpty(output)) {
                Log("[!] No output from DriverIndexer engine.");
                return list;
            }

            // Split by "Driver Info:" to handle the provided multiline format
            string[] blocks = output.Split(new[] { "Driver Info:", "驱动信息:" }, StringSplitOptions.RemoveEmptyEntries);
            
            HashSet<string> seenInfs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var block in blocks)
            {
                var item = new DriverInfoItem();
                string[] lines = block.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    string l = line.Trim();
                    if (string.IsNullOrEmpty(l) || l.StartsWith("-")) continue;

                    // Robust INF extraction:
                    // DriverIndexer output format/labels may differ by WinPE language, version, or separator.
                    // Prefer extracting from the explicit "Inf Path" / "程序路径" field first (supports ':' or '=').
                    if (l.IndexOf(".inf", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try
                        {
                            string? infVal = null;

                            // 1) Labeled extraction (handles both "Inf Path: X:\...\oem0.inf" and "Inf Path=X:\...\oem0.inf")
                            var labeled = System.Text.RegularExpressions.Regex.Match(
                                l,
                                @"(?i)\b(?:Inf\s*Path|程序路径)\b\s*[:=]\s*""?(?<val>[^""\r\n]*?\.inf)""?"
                            );

                            if (labeled.Success)
                            {
                                infVal = labeled.Groups["val"].Value.Trim();
                            }

                            // 2) Fallback extraction: Prefer full path with drive letter: C:\...\oem0.inf
                            if (string.IsNullOrEmpty(infVal))
                            {
                                var fullPathMatch = System.Text.RegularExpressions.Regex.Match(
                                    l,
                                    @"(?i)([A-Z]:\\[^""\r\n]*?\.inf)\b"
                                );

                                if (fullPathMatch.Success)
                                {
                                    infVal = fullPathMatch.Groups[1].Value.Trim().Trim('"');
                                }
                                else if (string.IsNullOrEmpty(item.PublishedName))
                                {
                                    // 3) Last-resort: token-like "oem0.inf" (only if we don't already have a PublishedName)
                                    var tokenMatch = System.Text.RegularExpressions.Regex.Match(
                                        l,
                                        @"(?i)\b([^\s""']+?\.inf)\b"
                                    );
                                    if (tokenMatch.Success)
                                        infVal = tokenMatch.Groups[1].Value.Trim().Trim('"');
                                }
                            }

                            if (!string.IsNullOrEmpty(infVal))
                            {
                                string fn = Path.GetFileName(infVal);
                                if (!string.IsNullOrEmpty(fn))
                                {
                                    item.PublishedName = fn;
                                    if (string.IsNullOrEmpty(item.Description))
                                        item.Description = item.PublishedName;
                                }
                            }
                        }
                        catch
                        {
                            // Ignore parsing failures; we may still parse via key/value labels below.
                        }
                    }

                    // Key-Value splitting (e.g., "Inf Path: C:\path\to\driver.inf")
                    var parts = l.Split(new[] { ':' }, 2);
                    if (parts.Length < 2) continue;

                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    if (key.Equals("Inf Path", StringComparison.OrdinalIgnoreCase) || key.Contains("程序路径")) {
                        try {
                            item.PublishedName = Path.GetFileName(val); 
                            if (string.IsNullOrEmpty(item.Description)) item.Description = item.PublishedName;
                        } catch { item.PublishedName = val; }
                    }
                    else if (key.Equals("OEM Name", StringComparison.OrdinalIgnoreCase) || key.Contains("OEM名称"))
                        item.OriginalName = val;
                    else if (key.Equals("Class Name", StringComparison.OrdinalIgnoreCase) || key.Contains("类别名称"))
                        item.Class = val;
                    else if (key.Equals("Class Desc", StringComparison.OrdinalIgnoreCase) || key.Contains("类别描述"))
                        item.Description = val;
                    else if (key.Equals("Provider", StringComparison.OrdinalIgnoreCase) || key.Contains("供应商"))
                        item.Provider = val;
                    else if (key.Equals("Version", StringComparison.OrdinalIgnoreCase) || key.Contains("版本"))
                        item.Version = val;
                    else if (key.Equals("Date", StringComparison.OrdinalIgnoreCase) || key.Contains("日期"))
                        item.Date = val;
                }

                // Deduplication: Only add if we haven't seen this INF file in this scan
                // DriverIndexer.exe export expects the INF filename (e.g. adobepdf.inf) for --inf.
                if (!string.IsNullOrEmpty(item.PublishedName) && !seenInfs.Contains(item.PublishedName)) {
                    list.Add(item);
                    seenInfs.Add(item.PublishedName);
                }
            }

            Log(string.Format("Scan complete. Found {0} unique drivers.", list.Count));
            return list;
        }

        private List<DriverInfoItem> ShowDriverSelector(List<DriverInfoItem> drivers)
        {
            List<DriverInfoItem> selected = new List<DriverInfoItem>();
            bool confirmed = false;
            this.Invoke(new Action(() => {
                using Form f = new Form { 
                    Text = GetStr("UI_SelectDriverTitle"), 
                    ClientSize = new Size(600, 500), 
                    StartPosition = FormStartPosition.CenterParent, 
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false, MinimizeBox = false,
                    AutoScaleMode = AutoScaleMode.Dpi,
                    AutoScaleDimensions = new SizeF(96F, 96F),
                    Padding = new Padding(12)
                };

                // Bottom Button Panel (Responsive)
                TableLayoutPanel tlp = new TableLayoutPanel {
                    Dock = DockStyle.Bottom,
                    Height = 45,
                    ColumnCount = 4,
                    RowCount = 1
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

                CheckedListBox clb = new CheckedListBox { 
                    Dock = DockStyle.Fill,
                    CheckOnClick = true,
                    Font = new Font("Segoe UI", 9.5f)
                };
                foreach (var d in drivers) clb.Items.Add(d, true);

                Button btnAll = new Button { Text = GetStr("UI_SelectAll"), Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
                btnAll.Click += (s, e) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, true); };

                Button btnNone = new Button { Text = GetStr("UI_SelectNone"), Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
                btnNone.Click += (s, e) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, false); };

                Button btnCancel = new Button { Text = GetStr("UI_Cancel"), Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };
                btnCancel.Click += (s, e) => { confirmed = false; f.Close(); };

                Button btnOk = new Button { Text = GetStr("UI_OK"), Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.Teal, ForeColor = Color.White, Margin = new Padding(3) };
                btnOk.Click += (s, e) => { 
                    foreach (var item in clb.CheckedItems) selected.Add((DriverInfoItem)item); 
                    confirmed = true; 
                    f.Close(); 
                };

                tlp.Controls.Add(btnAll, 0, 0);
                tlp.Controls.Add(btnNone, 1, 0);
                tlp.Controls.Add(btnCancel, 2, 0);
                tlp.Controls.Add(btnOk, 3, 0);

                f.Controls.Add(clb);
                f.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 10 }); // Spacer
                f.Controls.Add(tlp);

                f.ShowDialog();
            }));
            return confirmed ? selected : new List<DriverInfoItem>();
        }

        private void RunDriverBackup(List<DriverInfoItem> drivers, string destDir)
        {
            this.Invoke(new Action(() => SetUIState(true)));
            
            string drvIndexer = GetToolPath("DriverIndexer.exe");
            int successCount = 0;

            Log(GetStr("LOG_DriverBackup_Starting"));
            Log(GetStr("LOG_DriverBackup_Folder") + ": " + destDir);

            string targetDrive = IsWinPE() ? GetOfflineWindowsDrive() : GetSystemDrive();
            if (string.IsNullOrEmpty(targetDrive)) targetDrive = GetSystemDrive();

            // Normalize: strip trailing backslash — DriverIndexer export accepts "C:" form.
            // Using "C:\" without quoting is safe here (no spaces), but keeping consistent.
            string exportTarget = targetDrive.TrimEnd('\\', '/');

            // Sanitize Destination Directory for command line
            string safeDest = destDir.TrimEnd('\\');

            for (int i = 0; i < drivers.Count; i++) {
                var d = drivers[i];
                string label = $"{i + 1}/{drivers.Count}";
                // Update progress without spamming log (v3.2)
                UpdateProgress((int)((double)(i + 1) / drivers.Count * 100), label, ColorBackup);
                
                // Show clean progress log: [1/9] Backup: Description
                Log(string.Format("[{0}/{1}] {2}: {3}", i + 1, drivers.Count, GetStr("UI_Backup"), d.Description));

                // Command: export <system drive> <export directory> --inf <inf name> -s
                string args = $"export {exportTarget} \"{safeDest}\" --inf \"{d.PublishedName}\" -s";

                int detected;
                // v3.3: Call with skipReset=true to maintain outer loop progress bar value
                int code = RunToolWithProgress(drvIndexer, args, out detected, true, 0, ColorBackup, true);
                if (code == 0) successCount++;
            }

            Log(string.Format("-> {0} / {1} drivers successfully backed up.", successCount, drivers.Count));
            
            UpdateProgress(100, successCount == drivers.Count ? "100%" : $"{successCount}/{drivers.Count}", ColorBackup);
            UpdateProgress(0, GetStr("UI_Ready"));
            this.Invoke(new Action(() => SetUIState(false)));
            isOperationRunning = false;
        }

        private void btnDriverRestore_Click(object sender, EventArgs e)
        {
            if (isOperationRunning) return;

            using (var fbd = new FolderBrowserDialog { Description = GetStr("MSG_SelectFolder_Restore") }) {
                if (fbd.ShowDialog() == DialogResult.OK) {
                    isOperationRunning = true; // LOCK IMMEDIATELY in UI Thread
                    string sourceDir = fbd.SelectedPath;
                    
                    // Check for INF files
                    var infFiles = Directory.GetFiles(sourceDir, "*.inf", SearchOption.AllDirectories);
                    if (infFiles.Length == 0) {
                        isOperationRunning = false; // Release lock
                        // This runs on the UI thread already (button click handler) — no Invoke needed
                        MessageBox.Show(this, GetStr("MSG_NoDriversFound"), GetStr("UI_Warning"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0);
                        return;
                    }

                    Task.Run(() => RunDriverRestore(sourceDir));
                }
            }
        }

        private void RunDriverRestore(string sourceDir)
        {
            this.Invoke(new Action(() => SetUIState(true)));
            UpdateProgress(-1, GetStr("LOG_DriverRestore_Starting"));
            Log($"{GetStr("UI_SourceFolder")}: {sourceDir}");

            try {
                string drvIndexer = GetToolPath("DriverIndexer.exe");
                
                // 1. Scan for INF files first to get a count
                var infFiles = Directory.GetFiles(sourceDir, "*.inf", SearchOption.AllDirectories);
                int total = infFiles.Length;
                int successCount = 0;

                // Unified log for both WinPE and Live. No duplicates inside blocks.
                string args;
                if (IsWinPE()) {
                    string imagePath = GetOfflineWindowsDrive();
                    if (string.IsNullOrEmpty(imagePath)) {
                        Log("[!] Error: Offline Windows partition not found. Make sure the Windows drive is mounted.");
                        UpdateProgress(0, GetStr("UI_Ready"));
                        this.Invoke(new Action(() => SetUIState(false)));
                        isOperationRunning = false;
                        return;
                    }
                    // Use unquoted drive root — no spaces, no escaping issues (e.g.  D:\ )
                    string importTarget = imagePath.TrimEnd('\\', '/') + "\\";
                    args = $"import {importTarget} \"{sourceDir.TrimEnd('\\')}\"";
                } else {
                    // Live mode: resolve the actual system drive instead of hardcoding C:
                    string sysDrive = GetSystemDrive().TrimEnd('\\', '/') + "\\";
                    args = $"import {sysDrive} \"{sourceDir.TrimEnd('\\')}\"";
                }

                Log("Executing restoration...");
                int detected;
                int code = RunToolWithProgress(drvIndexer, args, out detected, false, total, ColorRestore);
                successCount = detected;

                if (!IsWinPE()) {
                    // Simulating "Scan for hardware changes" to refresh Device Manager instantly (Online mode)
                    this.Invoke(new Action(() => Log("Refreshing hardware (Scan for changes)...")));
                    RunCommand(GetSystemToolPath("pnputil.exe"), "/scan-devices");
                }
                
                Log(string.Format("-> {0} / {1} drivers successfully restored.", successCount, total));
                UpdateProgress(100, "100%");
            } catch (Exception ex) {
                Log("Error during driver restore: " + ex.Message);
            }

            Log(GetStr("LOG_Driver_Finished"));
            UpdateProgress(0, GetStr("UI_Ready"));
            this.Invoke(new Action(() => SetUIState(false)));
            isOperationRunning = false;
        }

        private int CopyDirectory(string sourceDir, string destDir)
        {
            int count = 0;
            try {
                Directory.CreateDirectory(destDir);
                foreach (string file in Directory.GetFiles(sourceDir)) {
                    string destFile = Path.Combine(destDir, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                    count++;
                }
                foreach (string sub in Directory.GetDirectories(sourceDir)) {
                    string destSub = Path.Combine(destDir, Path.GetFileName(sub));
                    count += CopyDirectory(sub, destSub);
                }
            } catch (Exception ex) {
                Log($"[!] Copy Error ({Path.GetFileName(sourceDir)}): {ex.Message}");
            }
            return count;
        }

        private string GetDriverDescriptionFromInf(string infPath)
        {
            try {
                string[] lines = File.ReadAllLines(infPath);
                string desc = "";
                bool inStrings = false;

                foreach (var line in lines) {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("[", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith("]", StringComparison.OrdinalIgnoreCase)) {
                        inStrings = trimmed.Equals("[Strings]", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (inStrings && (trimmed.Contains("Desc", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Name", StringComparison.OrdinalIgnoreCase)) && trimmed.Contains("=")) {
                        var parts = trimmed.Split(new[] { '=' }, 2);
                        if (parts.Length == 2) {
                            desc = parts[1].Trim().Trim('"');
                            if (!string.IsNullOrEmpty(desc)) break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(desc)) {
                    string parent = Path.GetFileName(Path.GetDirectoryName(infPath) ?? "");
                    if (!string.IsNullOrEmpty(parent) && parent.Contains("_")) return parent;
                    return Path.GetFileName(infPath);
                }
                return desc;
            } catch {
                return Path.GetFileName(infPath);
            }
        }

        #endregion
    }
}
