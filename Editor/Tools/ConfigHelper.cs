using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnitySkillsCSharp
{
    /// <summary>
    /// Static helper for reading/writing/syncing the JSON config file.
    /// Master copy lives at <c>Assets/Unity Skills CSharp/Editor/Config/config.json</c>.
    /// A synced copy is maintained at <c>{claudeDir}/skills/unity-skills-csharp/assets/config.json</c>.
    /// </summary>
    public static class ConfigHelper
    {

        /// <summary>
        /// Master config path inside the user's Assets folder.
        /// </summary>
        public static string ConfigPath => Path.Combine(
            Application.dataPath, "Unity Skills CSharp", "Editor", "Config", "config.json");

        /// <summary>
        /// Synced copy path inside the .claude skills directory.
        /// </summary>
        public static string SyncTargetPath(string claudeDir)
        {
            return Path.Combine(claudeDir, "skills", "unity-skills-csharp", "assets", "config.json");
        }

        /// <summary>
        /// True if the config file exists, port is valid (> 0), and claudeDir is not empty.
        /// </summary>
        public static bool Valid
        {
            get
            {
                if (!File.Exists(ConfigPath))
                    return false;
                var (port, claudeDir) = Load();
                return port > 0 && !string.IsNullOrEmpty(claudeDir);
            }
        }

        /// <summary>
        /// Load config from the master file. Returns defaults if the file is missing or malformed.
        /// </summary>
        public static (int port, string claudeDir) Load()
        {
            if (!File.Exists(ConfigPath))
                return (-1, "");

            try
            {
                JObject json = JObject.Parse(File.ReadAllText(ConfigPath, Encoding.UTF8));
                int port = json["port"]?.Value<int>() ?? -1;
                string claudeDir = json["claudeDir"]?.Value<string>() ?? "";
                return (port, claudeDir);
            }
            catch
            {
                Debug.LogError($"{Const.LogPrefixConfigHelper} Failed to parse config, using defaults.");
                return (-1, "");
            }
        }

        /// <summary>
        /// Write config to the master file.
        /// </summary>
        public static void Save(int port, string claudeDir)
        {
            var json = new JObject
            {
                ["port"] = port,
                ["claudeDir"] = claudeDir ?? ""
            };

            string dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(ConfigPath, json.ToString(Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
            Debug.Log($"{Const.LogPrefixConfigHelper} Config saved to: {ConfigPath}");
        }

        /// <summary>
        /// Copy the master config to the .claude skills directory.
        /// </summary>
        public static void SyncToClaude(string claudeDir)
        {
            if (string.IsNullOrEmpty(claudeDir))
            {
                Debug.LogError($"{Const.LogPrefixConfigHelper} Cannot sync: claudeDir is empty.");
                return;
            }

            if (!File.Exists(ConfigPath))
            {
                Debug.LogError($"{Const.LogPrefixConfigHelper} Cannot sync: master config not found.");
                return;
            }

            string targetPath = SyncTargetPath(claudeDir);
            string targetDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            File.Copy(ConfigPath, targetPath, overwrite: true);
            Debug.Log($"{Const.LogPrefixConfigHelper} Synced config to: {targetPath}");
        }

        /// <summary>
        /// True if the sync target exists and matches the master.
        /// </summary>
        public static bool IsSynced(string claudeDir)
        {
            if (string.IsNullOrEmpty(claudeDir) || !File.Exists(ConfigPath))
                return false;

            string targetPath = SyncTargetPath(claudeDir);
            if (!File.Exists(targetPath))
                return false;

            try
            {
                string master = File.ReadAllText(ConfigPath, Encoding.UTF8);
                string target = File.ReadAllText(targetPath, Encoding.UTF8);
                return master == target;
            }
            catch
            {
                return false;
            }
        }
    }
}
