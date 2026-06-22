using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnitySkillsCSharp
{
    public static class SkillHelper
    {

        public static void Install()
        {
            if (!ResolvePaths(out string srcPath, out string dstPath)) return;

            string skillMdPath = Path.Combine(dstPath, "SKILL.md");

            if (!Directory.Exists(dstPath) || !File.Exists(skillMdPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
                CopyDirectory(srcPath, dstPath);
                Debug.Log($"{Const.LogPrefixSkillInstaller} Installed to: {dstPath}");
            }
        }

        public static void UpdateSkill()
        {
            if (!ResolvePaths(out string srcPath, out string dstPath)) return;

            if (Directory.Exists(dstPath))
            {
                Directory.Delete(dstPath, recursive: true);
                Debug.Log($"{Const.LogPrefixSkillInstaller} Removed existing: {dstPath}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
            CopyDirectory(srcPath, dstPath);
            Debug.Log($"{Const.LogPrefixSkillInstaller} Updated to: {dstPath}");
        }

        private static bool ResolvePaths(out string srcPath, out string dstPath)
        {
            srcPath = dstPath = null;

            string packageRoot = GetPackageRoot();
            if (packageRoot == null)
            {
                Debug.LogError($"{Const.LogPrefixSkillInstaller} Could not locate package root.");
                return false;
            }

            srcPath = Path.Combine(packageRoot, Const.SourceFolder);
            if (!Directory.Exists(srcPath))
            {
                Debug.LogError($"{Const.LogPrefixSkillInstaller} Source not found: {srcPath}");
                return false;
            }

            string projectRoot = GetProjectRoot();
            dstPath = Path.GetFullPath(Path.Combine(projectRoot, Const.SkillsDir, Const.DestName));
            return true;
        }

        private static string GetProjectRoot()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }

        private static string GetPackageRoot()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(SkillHelper).Assembly);
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
