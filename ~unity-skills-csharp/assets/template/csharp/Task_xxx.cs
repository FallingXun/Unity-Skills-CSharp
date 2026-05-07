using System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace UnitySkillsCSharp
{
    // Task：{任务内容}
    public static class Task_{任务编号}
    {
        private const string k_Date   = "{日期}";
        private const string k_TaskId   = "{任务编号}";
        private const string k_MenuItem_Execute = "Unity Skills CSharp/Task/" + k_Date + "/" + k_TaskId + "/Execute";
        private const string k_MenuItem_Ping = "Unity Skills CSharp/Task/" + + k_Date + "/" + k_TaskId + "/Ping";
        private const string k_TaskPath = "Assets/Unity Skills CSharp/Editor/Task/" + k_Date + "/Task_" + k_TaskId +".cs";

        public static void Step_1()
        {
            // Step 1：{步骤内容}
            try
            {
                // 实现步骤处理逻辑
            }
            catch (Exception e)
            {
                throw StepException(1, e);
            }
        }

        // 按需继续添加步骤方法 Step_2, Step_3 ...

        [MenuItem(k_MenuItem_Execute)]
        public static void Execute()
        {
            Step_1();
            // Step_2();
            // Step_3();
            Debug.Log($"[{k_TaskId}] Execute completed.");
        }

        private static Exception StepException(int step, Exception inner)
        {
            var msg = JsonConvert.SerializeObject(new
            {
                task  = k_TaskId,
                step  = step,
                error = inner.Message,
            });
            return new Exception(msg, inner);
        }

        
        [MenuItem(k_MenuItem_Ping)]
        public static void Ping()
        {
            var task = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(k_TaskPath);
            if(task != null)
            {
                Selection.activeObject = task;
                EditorGUIUtility.PingObject(task);
                Debug.Log($"[{k_TaskPath}] Ping success.", task);
            }
            else
            {
                Debug.Log($"[{k_TaskPath}] Ping fail.");
            }
        }
    }
}
