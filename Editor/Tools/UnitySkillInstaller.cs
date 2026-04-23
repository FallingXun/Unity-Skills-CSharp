using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnitySkillsCSharp
{
    public static class UnitySkillInstaller
    {
        private const string k_MenuInstall    = "Unity Skills CSharp/Install Skill";
        private const string k_SourceFolder   = "~unity-skills-csharp";
        private const string k_DestName       = "unity-skills-csharp";
        private const string k_SkillsDir      = ".claude/skills";
        private const string k_LogPrefix      = "[UnitySkillInstaller]";

        private const string k_ConfigFile     = "config.ini";
        private const string k_ConfigAssetDir = "~unity-skills-csharp/assets";
        private const string k_ProjectSection = "project";
        private const string k_ProjectRootKey = "root_path";

        [MenuItem(k_MenuInstall)]
        public static void Install()
        {
            string packageRoot = GetPackageRoot();
            if (packageRoot == null)
            {
                Debug.LogError($"{k_LogPrefix} Could not locate package root.");
                return;
            }

            string srcPath     = Path.Combine(packageRoot, k_SourceFolder);
            string projectRoot = GetProjectRoot(packageRoot);
            string dstPath     = Path.GetFullPath(Path.Combine(projectRoot, k_SkillsDir, k_DestName));

            if (!Directory.Exists(srcPath))
            {
                Debug.LogError($"{k_LogPrefix} Source not found: {srcPath}");
                return;
            }

            if (!Directory.Exists(dstPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
                CopyDirectory(srcPath, dstPath);
                Debug.Log($"{k_LogPrefix} Installed to: {dstPath}");
            }
        }

        private static string GetProjectRoot(string packageRoot)
        {
            string configPath = Path.Combine(packageRoot, k_ConfigAssetDir, k_ConfigFile);
            if (File.Exists(configPath))
            {
                string value = IniUtils.Read(configPath, k_ProjectSection, k_ProjectRootKey);
                if (!string.IsNullOrWhiteSpace(value) && value != "\"\"" && Directory.Exists(value))
                    return value;
            }
            return Path.GetDirectoryName(Application.dataPath);
        }

        private static string GetPackageRoot()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(UnitySkillInstaller).Assembly);
            return info?.resolvedPath;
        }

        private static void CopyDirectory(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var file in Directory.GetFiles(src))
                File.Copy(file, Path.Combine(dst, Path.GetFileName(file)));
            foreach (var dir in Directory.GetDirectories(src))
                CopyDirectory(dir, Path.Combine(dst, Path.GetFileName(dir)));
        }
    }
}
