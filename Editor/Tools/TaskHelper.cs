using UnityEditor;
using UnityEngine;

namespace UnitySkillsCSharp
{
    public static class TaskHelper
    {
        public static void ClearAllTasks()
        {
            if (!AssetDatabase.IsValidFolder(Const.TaskDir))
            {
                Debug.Log($"{Const.LogPrefixTaskHelper} Task folder not found: {Const.TaskDir}");
                return;
            }

            bool deleted = AssetDatabase.DeleteAsset(Const.TaskDir);
            if (deleted)
                Debug.Log($"{Const.LogPrefixTaskHelper} Cleared: {Const.TaskDir}");
            else
                Debug.LogError($"{Const.LogPrefixTaskHelper} Failed to delete: {Const.TaskDir}");
        }
    }
}
