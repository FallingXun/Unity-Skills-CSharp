using System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace UnitySkills
{
    // Task：{任务内容}
    public static class Task_{任务编号}
    {
        private const string k_TaskId   = "{任务编号}";
        private const string k_MenuItem = "Unity Skills CSharp/" + k_TaskId + "/Execute";

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

        [MenuItem(k_MenuItem)]
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
    }
}
