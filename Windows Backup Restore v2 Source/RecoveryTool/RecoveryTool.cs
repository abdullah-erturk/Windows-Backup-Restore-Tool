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

namespace BackupRestoreTool
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            if (!CheckDotNet())
            {
                bool isTR = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName == "tr";
                string msg = isTR ? "Windows Kurtarma Aracı için .NET Framework 4.5 veya üzeri gereklidir.\nŞimdi indirme sayfasına gitmek ister misiniz?" 
                                 : ".NET Framework 4.5 or higher is required to run Windows Recovery Tool.\nWould you like to go to the download page now?";
                string title = isTR ? ".NET Framework Gerekli" : ".NET Framework Required";
                
                if (MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try { Process.Start("https://dotnet.microsoft.com/download/dotnet-framework/net45"); } catch { }
                }
                return;
            }

            if (!IsRunningAsAdmin())
            {
                RestartAsAdmin();
                return;
            }

            ImportDigitalSignature();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        static bool CheckDotNet()
        {
            try
            {
                using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                    .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    if (ndpKey == null) return false;
                    int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                    return releaseKey >= 378389; // 378389 is .NET 4.5
                }
            }
            catch { return false; }
        }

        static bool IsRunningAsAdmin()
        {
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        static void RestartAsAdmin()
        {
            ProcessStartInfo proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Application.ExecutablePath,
                Verb = "runas"
            }; 
            try { Process.Start(proc); } catch { }
            Environment.Exit(0);
        }

        static void ImportDigitalSignature() // Geliştiriciye ait dijital sertifika
        {
            // .reg dosyasının içeriği
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
74,3b,e5,f7,eb,be,5e,cb,0b,ec,3b,60,59,f1,96,bb,c1,c5,78,d2,32,79,dc,40,1d,\
7e,16,e2,31,4d,d2,0a,3d,46,8a,d0,87,5f,be,60,c0,d8,30,78,1e,c5,83,2a,97,44,\
43,ef,2b,f5,8f,d1,d2,16,14,0d,06,5b,fe,55,e7,53,62,b2,4c,e3,61,7b,03,53,8b,\
9f,f0,22,a4,0f,4b,5d,3e,d4,4b,1e,26,fe,36,3e,7e,16,39,a2,df,ee,8e,4f,3a,21,\
c2,36,c6,24,a9,d2,dd,eb,d9,69,e5,a4,78,36,bb,3b,60,df,6b,c4,8f,d9,a7,d2,be,\
f4,d7,61,40,dc,a8,78,50,90,35,b5,77,de,3a,bc,f9,4c,11,61,de,d6,16,4f,85,42,\
42,8a,36,27,ae,4a,3a,8b,40,f2,ba,db,6f,c9,64,dd,1c,9f";

            // .reg dosyasının yolu
            string tempPath = Path.GetTempPath();
            string regFilePath = Path.Combine(tempPath, "certificate.reg");

            try
            {
                // .reg dosyasını oluştur
                File.WriteAllText(regFilePath, regContent);

                // .reg dosyasını çalıştır
                Process process = new Process();
                process.StartInfo.FileName = "regedit.exe";
                process.StartInfo.Arguments = string.Format("/s \"{0}\"", regFilePath);
                process.StartInfo.Verb = "runas"; // Yönetici haklarıyla çalıştırmak için
                process.Start();

                // Process'in tamamlanmasını bekle
                process.WaitForExit();
            }
            finally
            {
                // .reg dosyasını sil
                if (File.Exists(regFilePath))
                {
                    File.Delete(regFilePath);
                }
            }
        }
    }

    public class PartitionItem
    {
        public string DisplayText { get; set; }
        public string DrivePath { get; set; }  // "C:\" or "\\?\Volume{GUID}\"
        public bool HasLetter { get; set; }
        public string VolumeID { get; set; } // DeviceID
        public string DiskIndex { get; set; }
        public string PartitionIndex { get; set; }
        public override string ToString() { return DisplayText; }
    }

    public class DiskItem
    {
        public string DisplayText { get; set; }
        public string DiskID { get; set; } // "0", "1", etc.
        public override string ToString() { return DisplayText; }
    }

    public class WimIndexItem
    {
        public string Index { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string DisplayText { get { return string.Format("{0}. {1} ({2})", Index, Name, Version); } }
        public override string ToString() { return DisplayText; }
    }

    public struct LogEntry
    {
        public string Key;
        public object[] Args;
        public string RawText;
        public DateTime Timestamp;
    }

    public class LanguageItem
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public override string ToString() { return Name; }
    }
}
