using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace UnitySkillsCSharp
{
    public static class IniUtils
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(
            string lpAppName, string lpKeyName, string lpDefault,
            StringBuilder lpReturnedString, uint nSize, string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern bool WritePrivateProfileString(
            string lpAppName, string lpKeyName, string lpString, string lpFileName);

        public static string Read(string path, string section, string key, string defaultValue = "")
        {
            var sb = new StringBuilder(512);
            GetPrivateProfileString(section, key, defaultValue, sb, (uint)sb.Capacity,
                Path.GetFullPath(path));
            return sb.ToString();
        }

        public static void Write(string path, string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, Path.GetFullPath(path));
        }
    }
}
