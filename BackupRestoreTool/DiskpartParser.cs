using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BackupRestoreTool
{
    public class DiskpartParser
    {
        public class PartitionInfo
        {
            public int Index { get; set; }
            public string Type { get; set; } = string.Empty;
            public long SizeBytes { get; set; }
            public long StartingOffset { get; set; }
            public bool IsActive { get; set; }
            public bool IsSystem { get; set; }
            public string DriveLetter { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public string FileSystem { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public bool IsUnallocated { get; set; } = false;
        }

        public class DiskInfo
        {
            public int Index { get; set; }
            public bool IsGPT { get; set; }
            public long TotalSize { get; set; }
            public List<PartitionInfo> Partitions { get; set; } = new List<PartitionInfo>();
        }

                        public class DiskpartResult {
            public string Output { get; set; } = string.Empty;
            public int ExitCode { get; set; } = -1;
        }

        public static string RunDiskpart(string script)
        {
            return RunDiskpartEx(script, 15000).Output;
        }

        public static DiskpartResult RunDiskpartEx(string script, int timeoutMs = 15000)
        {
            string tempScript = Path.Combine(Path.GetTempPath(), $"dp_{Guid.NewGuid():N}.txt");
            try
            {
                File.WriteAllText(tempScript, script, Encoding.Default);
                ProcessStartInfo psi = new ProcessStartInfo("diskpart.exe", $"/s \"{tempScript}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.Default
                };

                using (Process? p = Process.Start(psi))
                {
                    if (p == null) return new DiskpartResult { Output = string.Empty, ExitCode = -1 };
                    var readTask = p.StandardOutput.ReadToEndAsync(); 
                    if (!readTask.Wait(timeoutMs)) { 
                        try { p.Kill(); p.WaitForExit(); } catch { } 
                        return new DiskpartResult { Output = string.Empty, ExitCode = -1 }; 
                    } 
                    string output = readTask.Result;
                    p.WaitForExit(5000);
                    return new DiskpartResult { Output = output, ExitCode = p.ExitCode };
                }
            }
            catch { return new DiskpartResult { Output = string.Empty, ExitCode = -1 }; }
            finally { if (File.Exists(tempScript)) try { File.Delete(tempScript); } catch { } }
        }

                public static List<DiskInfo> GetDisks()
        {
            List<DiskInfo> disks = new List<DiskInfo>();

            // Fetch exact sizes from WMI if available (fallback to list disk later)
            var exactSizes = new Dictionary<int, long>();
            try {
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT Index, Size FROM Win32_DiskDrive")) {
                    foreach (var obj in searcher.Get()) {
                        if (int.TryParse(obj["Index"]?.ToString(), out int idx) && long.TryParse(obj["Size"]?.ToString(), out long size)) {
                            exactSizes[idx] = size;
                        }
                    }
                }
            } catch { }

            string output = RunDiskpart("list disk");
            
            // Disk ###  Status         Size     Free     Dyn  Gpt (Universal, all languages)
            var matches = Regex.Matches(output, @"^\s*\S+\s+(\d+)\s+(.*?)\s+(\d+[\.,]?\d*\s+\S+)\s+(\d+[\.,]?\d*\s+\S+)\s+(\*?)\s+(\*?)\s*$", RegexOptions.Multiline);

            foreach (Match m in matches)
            {
                // Verify it's actually a disk row by checking if group 1 is a number
                if (!int.TryParse(m.Groups[1].Value, out int diskIdx)) continue;

                DiskInfo di = new DiskInfo
                {
                    Index = int.Parse(m.Groups[1].Value),
                    TotalSize = exactSizes.ContainsKey(int.Parse(m.Groups[1].Value)) ? exactSizes[int.Parse(m.Groups[1].Value)] : ParseSizeToBytes(m.Groups[3].Value),
                    IsGPT = m.Groups[6].Value == "*"
                };
                di.Partitions = GetPartitions(di.Index, di.TotalSize);
                disks.Add(di);
            }
            return disks;
        }

                public static List<PartitionInfo> GetPartitions(int diskIndex, long totalDiskSize = 0)
        {
            List<PartitionInfo> parts = new List<PartitionInfo>();
            
            // Dilden bağımsız IsActive tespiti (WMI BootPartition / PrimaryPartition)
            var activePartitions = new HashSet<int>();
            try {
                using (var searcher = new System.Management.ManagementObjectSearcher($"SELECT Index, BootPartition FROM Win32_DiskPartition WHERE DiskIndex = {diskIndex}")) {
                    foreach (var obj in searcher.Get()) {
                        if (obj["BootPartition"] != null && (bool)obj["BootPartition"] == true && obj["Index"] != null) {
                            if (int.TryParse(obj["Index"].ToString(), out int pIdx)) {
                                activePartitions.Add(pIdx + 1); // WMI Index 0 tabanlıdır, Diskpart 1 tabanlı
                            }
                        }
                    }
                }
            } catch { }

            string output = RunDiskpartEx($"select disk {diskIndex}\nlist partition").Output;

            // Capture Index, Type, Size, Offset (Universal for all global languages)
            var matches = Regex.Matches(output, @"^\s*\S+\s+(\d+)\s+(.*?)\s+(\d+[\.,]?\d*\s+\S+)\s+(\d+[\.,]?\d*\s+\S+)\s*?$", RegexOptions.Multiline);

            foreach (Match m in matches)
            {
                // Verify it is a valid partition row (skip headers)
                if (!int.TryParse(m.Groups[1].Value, out int partIdx)) continue;

                PartitionInfo pi = new PartitionInfo
                {
                    Index = int.Parse(m.Groups[1].Value),
                    Type = m.Groups[2].Value.Trim(),
                    SizeBytes = ParseSizeToBytes(m.Groups[3].Value),
                    StartingOffset = ParseSizeToBytes(m.Groups[4].Value)
                };

                                string detail = RunDiskpartEx($"select disk {diskIndex}\nselect partition {pi.Index}\ndetail partition").Output;
                
                // Fallback: WMI aktif bulduysa veya diskpart detayında aktif yazıyorsa
                pi.IsActive = activePartitions.Contains(pi.Index) || Regex.IsMatch(detail, @"(?i)^\s*(Active|Etkin|Активен|Aktiv|Activo|Attivo|Actif)\s*:\s*(Yes|Evet|Да|Ja|Si|Sì|Oui)", RegexOptions.Multiline);
                pi.IsSystem = pi.Type.Contains("System") || detail.Contains("Type    : c12a7328-f81f-11d2-ba4b-00a0c93ec93b");
                
                // Highly robust volume info parsing (Universal Language Averse)
                var lines = detail.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    // Universal Volume Match (e.g., "* Volume 1", "  Birim 2", "* 分区 3", "  Раздел 0")
                    if (Regex.IsMatch(line, @"^\s*\*?\s*\S+\s+\d+", RegexOptions.IgnoreCase) && 
                       (line.IndexOf("FAT", StringComparison.OrdinalIgnoreCase) >= 0 || 
                        line.IndexOf("NTFS", StringComparison.OrdinalIgnoreCase) >= 0 || 
                        line.IndexOf("RAW", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        line.IndexOf("EXFAT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        Regex.IsMatch(line, @"\s[A-Z]\s(?:\s|NTFS|FAT|RAW|EXFAT)")))
                    {
                        // 1. Capture Drive Letter (A-Z) bounded by spaces
                        var ltrMatch = Regex.Match(line, @"^\s*\*?\s*\S+\s+\d+\s+([A-Z])\s");
                        if (ltrMatch.Success) pi.DriveLetter = ltrMatch.Groups[1].Value.Trim();

                        // 2. Identify FileSystem by keywords
                        string[] fsKeywords = { "FAT32", "NTFS", "EXFAT", "FAT", "RAW" };
                        foreach (var fs in fsKeywords) {
                            if (line.IndexOf(fs, StringComparison.OrdinalIgnoreCase) >= 0) {
                                pi.FileSystem = fs;
                                break;
                            }
                        }

                        // 3. Identify Status
                        if (Regex.IsMatch(line, @"Healthy|Sa.lam", RegexOptions.IgnoreCase)) pi.Status = "Healthy";
                        else if (Regex.IsMatch(line, @"Failed|Ba.ar.s.z", RegexOptions.IgnoreCase)) pi.Status = "Failed";

                        // 4. Extract Label (Robust Array Split)
                        try {
                            var splitted = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            if (splitted.Count > 0 && splitted[0] == "*") splitted.RemoveAt(0);

                            int fsIndex = -1;
                            for (int i = 0; i < splitted.Count; i++) {
                                if (fsKeywords.Contains(splitted[i].ToUpper())) { fsIndex = i; break; }
                            }
                            
                            // If we found the Filesystem keyword index, the label is everything between the ID/Letter and the FS.
                            if (fsIndex > 2) {
                                // Default start is index 2 (Word/Birim, Number) -> Label starts at 2
                                // If Drive letter exists, usually it's at index 2, so Label starts at 3
                                int startIdx = string.IsNullOrEmpty(pi.DriveLetter) ? 2 : 3;
                                
                                // Extreme check: Sometimes label is empty and the space falls short
                                if (startIdx < fsIndex) {
                                    pi.Label = string.Join(" ", splitted.Skip(startIdx).Take(fsIndex - startIdx));
                                }
                            }
                        } catch { }
                        
                        break; // Stop parsing volume details once found
                    }
                    
                    // UNIVERSAL EXACT OFFSET CAPTURE: 
                    // Matches lines like "Offset in Bytes: 1048576" or "Uzaklık: 1048576 Bayt"
                    // Offset will always be the largest numeric value in the output (> 100,000)
                    var exactOffsetMatch = Regex.Match(line, @":\s*(\d{6,})");
                    if (exactOffsetMatch.Success) {
                        if (long.TryParse(exactOffsetMatch.Groups[1].Value, out long exactBytes)) {
                            pi.StartingOffset = exactBytes;
                        }
                    }
                }

                
                if (string.IsNullOrEmpty(pi.FileSystem) && detail.Contains("RAW")) pi.FileSystem = "RAW";

                parts.Add(pi);
            }

            // --- GAP ANALYSIS (Insert Unallocated Spaces) ---
            if (totalDiskSize > 0)
            {
                parts = parts.OrderBy(p => p.StartingOffset).ToList();
                List<PartitionInfo> enriched = new List<PartitionInfo>();
                long currentPos = 0;

                foreach (var p in parts)
                {
                    if (p.StartingOffset > currentPos + (1024 * 1024)) // 1MB minimum gap to count as unallocated
                    {
                        enriched.Add(new PartitionInfo {
                            IsUnallocated = true,
                            StartingOffset = currentPos,
                            SizeBytes = p.StartingOffset - currentPos,
                            Type = "Unallocated"
                        });
                    }
                    enriched.Add(p);
                    currentPos = p.StartingOffset + p.SizeBytes;
                }

                if (totalDiskSize > currentPos + (1024 * 1024))
                {
                    enriched.Add(new PartitionInfo {
                        IsUnallocated = true,
                        StartingOffset = currentPos,
                        SizeBytes = totalDiskSize - currentPos,
                        Type = "Unallocated"
                    });
                }
                return enriched;
            }

            return parts;
        }

        private static long ParseSizeToBytes(string sizeStr)
        {
            try
            {
                Match m = Regex.Match(sizeStr.Replace(",", ""), @"(\d+)\s*(\w+)");
                if (!m.Success) return 0;
                long val = long.Parse(m.Groups[1].Value);
                string unit = m.Groups[2].Value.ToUpper();
                if (unit.StartsWith("K")) return val * 1024;
                if (unit.StartsWith("M")) return val * 1024 * 1024;
                if (unit.StartsWith("G")) return val * 1024 * 1024 * 1024;
                if (unit.StartsWith("T")) return val * 1024 * 1024 * 1024 * 1024;
                return val;
            }
            catch { return 0; }
        }

        public static string? FindBootPartition(string diskIndex, bool isGPT)
        {
            try
            {
                int dIdx = int.Parse(diskIndex);
                var parts = GetPartitions(dIdx).Where(p => !p.IsUnallocated).OrderBy(p => p.Index).ToList();

                foreach (var p in parts)
                {
                    bool isBootCandidate = (isGPT ? p.IsSystem : p.IsActive) ||
                                           (p.SizeBytes < (1024L * 1024 * 1024) && p.Index <= 2);

                    if (isBootCandidate)
                    {
                        if (!string.IsNullOrEmpty(p.DriveLetter)) return p.DriveLetter + ":\\";
                        
                                                string tempLetter = MainForm.GetAvailableDriveLetter() ?? "S";
                        RunDiskpart($"select disk {diskIndex}\nselect partition {p.Index}\nassign letter={tempLetter}");
                        if (System.IO.Directory.Exists(tempLetter + ":\\")) return tempLetter + ":\\";
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
