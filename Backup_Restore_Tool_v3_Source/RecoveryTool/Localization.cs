using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace BackupRestoreTool
{
    public static class Localization
    {
        private static Dictionary<string, string> _translations = new Dictionary<string, string>();
        private static string _langDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lang");

        public static void LoadLanguage(string langCode)
        {
            _translations.Clear();
            string path = Path.Combine(_langDir, $"{langCode}.ini");
            if (!File.Exists(path)) return;

            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith(";") || line.Trim().StartsWith("[")) continue;
                int idx = line.IndexOf('=');
                if (idx > 0)
                {
                    string key = line.Substring(0, idx).Trim();
                    string val = line.Substring(idx + 1).Trim().Replace("\\n", Environment.NewLine);
                    _translations[key] = val;
                }
            }
        }

        public static string Get(string key, params object[] args)
        {
            if (_translations.ContainsKey(key))
            {
                string val = _translations[key];
                return args.Length > 0 ? string.Format(val, args) : val;
            }
            return key;
        }

        public static List<LanguageInfo> GetAvailableLanguages()
        {
            var list = new List<LanguageInfo>();
            if (!Directory.Exists(_langDir)) Directory.CreateDirectory(_langDir);

            foreach (var file in Directory.GetFiles(_langDir, "*.ini"))
            {
                string code = Path.GetFileNameWithoutExtension(file).ToLower();
                string name = ReadLanguageName(file) ?? code.ToUpper();
                list.Add(new LanguageInfo { Code = code, Name = name });
            }
            return list;
        }

        private static string? ReadLanguageName(string path)
        {
            try
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    if (line.StartsWith("UI_LANG_NAME=")) return line.Split('=')[1].Trim();
                }
            }
            catch { }
            return null;
        }
    }

    public class LanguageInfo
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public override string ToString() => Name;
    }
}
