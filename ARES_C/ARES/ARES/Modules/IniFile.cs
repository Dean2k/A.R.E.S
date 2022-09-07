using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ARES.Modules
{
    public class IniFile
    {
        private string Path;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public IniFile(string IniPath = null)
        {
            Path = new FileInfo(IniPath ?? "ARES" + ".ini").FullName;
        }

        public string Read(string Key, string Section = null)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section ?? "ARES", Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public string Read(string Key, string Section, string defaultString)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, defaultString, RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string Section = null)
        {
            WritePrivateProfileString(Section ?? "ARES", Key, Value, Path);
        }

        public void DeleteKey(string Key, string Section = null)
        {
            Write(Key, null, Section ?? "ARES");
        }

        public void DeleteSection(string Section = null)
        {
            Write(null, null, Section ?? "ARES");
        }

        public bool KeyExists(string Key, string Section = null)
        {
            return Read(Key, Section).Length > 0;
        }
    }
}