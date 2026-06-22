using UnityEditor;
using UnityEngine;

namespace UnitySkillsCSharp
{
    /// <summary>
    /// Entry point for the Unity Skills CSharp package.
    /// Runs on every domain reload via [InitializeOnLoad].
    /// Handles skill installation, config validation, and HTTP server startup.
    /// Also owns all MenuItem definitions for the package.
    /// </summary>
    [InitializeOnLoad]
    public static class Initialization
    {
        // -------------------------------------------------------------- lifecycle

        static Initialization()
        {
            // Step 1: Install skill files to .claude/skills/unity-skills-csharp
            SkillHelper.Install();

            // Step 2: Register HTTP server compilation hooks and lifecycle callbacks
            UnityHttpServer.Initialize();

            // Step 3: Validate config (file exists, port > 0, claudeDir not empty)
            if (!ConfigHelper.Valid)
            {
                Debug.Log($"{Const.LogPrefixInitialization} Config invalid, opening ConfigWindow.");
                ConfigWindow.Open();
                return;
            }

            var (port, claudeDir) = ConfigHelper.Load();

            // Step 4: Sync config to .claude if missing or outdated
            if (!ConfigHelper.IsSynced(claudeDir))
            {
                Debug.Log($"{Const.LogPrefixInitialization} Config not synced, copying to .claude.");
                ConfigHelper.SyncToClaude(claudeDir);
            }

            // Step 5: Stop any stale listener and start with the configured port
            UnityHttpServer.StopIfRunning();
            UnityHttpServer.StartWithPort(port);
        }

        // -------------------------------------------------------------- Menu items — Config (priority 100–199)

        [MenuItem(Const.MenuGroupConfig, false, 101)]
        private static void OpenConfig()
        {
            ConfigWindow.OpenFromMenu();
        }

        // -------------------------------------------------------------- Menu items — Skill (priority 200–299)

        [MenuItem(Const.MenuGroupSkillInstall, false, 201)]
        private static void SkillsInstall()
        {
            SkillHelper.Install();
        }

        [MenuItem(Const.MenuGroupSkillUpdate, false, 202)]
        private static void SkillsUpdate()
        {
            SkillHelper.UpdateSkill();

            if (!ConfigHelper.Valid)
            {
                Debug.LogWarning($"{Const.LogPrefixInitialization} Config invalid, skip sync.");
                return;
            }

            var (_, claudeDir) = ConfigHelper.Load();
            if (!ConfigHelper.IsSynced(claudeDir))
            {
                Debug.Log($"{Const.LogPrefixInitialization} Config not synced, copying to .claude.");
                ConfigHelper.SyncToClaude(claudeDir);
            }
        }

        // -------------------------------------------------------------- Menu items — Server (priority 300–399)

        [MenuItem(Const.MenuGroupServerStart, false, 301)]
        private static void ServerStart()
        {
            UnityHttpServer.StartServerMenu();
        }

        [MenuItem(Const.MenuGroupServerStop, false, 302)]
        private static void ServerStop()
        {
            UnityHttpServer.StopServerMenu();
        }

        [MenuItem(Const.MenuGroupServerRestart, false, 303)]
        private static void ServerRestart()
        {
            UnityHttpServer.RestartServerMenu();
        }

        [MenuItem(Const.MenuGroupServerAutoStart, false, 304)]
        private static void ServerToggleAutoStart()
        {
            UnityHttpServer.ToggleAutoStart();
        }

        [MenuItem(Const.MenuGroupServerAutoStart, true, 304)]
        private static bool ServerToggleAutoStartValidate()
        {
            return UnityHttpServer.AutoStartChecked();
        }

        // -------------------------------------------------------------- Menu items — Task (priority 400–499)

        [MenuItem(Const.MenuGroupTaskClear, false, 401)]
        private static void TaskClear()
        {
            TaskHelper.ClearAllTasks();
        }
    }
}
