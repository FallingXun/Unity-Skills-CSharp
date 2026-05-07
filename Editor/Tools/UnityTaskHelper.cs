using UnityEditor;
using UnityEngine;

namespace UnitySkillsCSharp
{
    public static class UnityTaskHelper
    {
        private const string k_MenuRoot  = "Unity Skills CSharp/Task";
        private const string k_MenuClear = k_MenuRoot + "/Clear";
        private const string k_TaskDir   = "Assets/Unity Skills CSharp/Editor/Task";
        private const string k_LogPrefix = "[UnityTaskHelper]";

        [MenuItem(k_MenuClear, false, 1)]
        public static void ClearAllTasks()
        {
            if (!AssetDatabase.IsValidFolder(k_TaskDir))
            {
                Debug.Log($"{k_LogPrefix} Task folder not found: {k_TaskDir}");
                return;
            }

            bool deleted = AssetDatabase.DeleteAsset(k_TaskDir);
            if (deleted)
                Debug.Log($"{k_LogPrefix} Cleared: {k_TaskDir}");
            else
                Debug.LogError($"{k_LogPrefix} Failed to delete: {k_TaskDir}");
        }
    }
}
