using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnitySkillsCSharp
{
    public static class UnitySkillInstaller
    {
        private const string k_MenuRoot      = "Unity Skills CSharp/Skills";
        private const string k_MenuInstall   = k_MenuRoot + "/Install";
        private const string k_MenuUpdate    = k_MenuRoot + "/Update";
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
            if (!ResolvePaths(out string srcPath, out string dstPath)) return;

            if (!Directory.Exists(dstPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
                CopyDirectory(srcPath, dstPath);
                Debug.Log($"{k_LogPrefix} Installed to: {dstPath}");
            }
        }

        [MenuItem(k_MenuUpdate)]
        public static void UpdateSkill()
        {
            if (!ResolvePaths(out string srcPath, out string dstPath)) return;

            if (Directory.Exists(dstPath))
            {
                Directory.Delete(dstPath, recursive: true);
                Debug.Log($"{k_LogPrefix} Removed existing: {dstPath}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
            CopyDirectory(srcPath, dstPath);
            Debug.Log($"{k_LogPrefix} Updated to: {dstPath}");
        }

        private static bool ResolvePaths(out string srcPath, out string dstPath)
        {
            srcPath = dstPath = null;

            string packageRoot = GetPackageRoot();
            if (packageRoot == null)
            {
                Debug.LogError($"{k_LogPrefix} Could not locate package root.");
                return false;
            }

            srcPath = Path.Combine(packageRoot, k_SourceFolder);
            if (!Directory.Exists(srcPath))
            {
                Debug.LogError($"{k_LogPrefix} Source not found: {srcPath}");
                return false;
            }

            string projectRoot = GetProjectRoot(packageRoot);
            dstPath = Path.GetFullPath(Path.Combine(projectRoot, k_SkillsDir, k_DestName));
            return true;
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
