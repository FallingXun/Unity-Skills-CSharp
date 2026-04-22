using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnitySkills
{
    public static class UnitySkillInstaller
    {
        private const string k_MenuInstall  = "Unity Skills CSharp/Install Skill";
        private const string k_SourceFolder = "~unity-skills-csharp";
        private const string k_DestName     = "unity-skills-csharp";
        private const string k_SkillsDir    = ".claude/skills";
        private const string k_ScriptName   = "UnitySkillInstaller.cs";
        private const string k_LogPrefix    = "[UnitySkillInstaller]";

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
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string dstPath     = Path.GetFullPath(Path.Combine(projectRoot, k_SkillsDir, k_DestName));

            if (!Directory.Exists(srcPath))
            {
                Debug.LogError($"{k_LogPrefix} Source not found: {srcPath}");
                return;
            }

            if (Directory.Exists(dstPath))
            {
                Directory.Delete(dstPath, recursive: true);
                Debug.Log($"{k_LogPrefix} Removed existing: {dstPath}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
            CopyDirectory(srcPath, dstPath);
            Debug.Log($"{k_LogPrefix} Installed to: {dstPath}");
        }

        private static string GetPackageRoot()
        {
            var rootPath = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UnitySkillInstaller).Assembly).resolvedPath;
            return rootPath;
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
