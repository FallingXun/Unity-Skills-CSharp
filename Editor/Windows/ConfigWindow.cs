using UnityEditor;
using UnityEngine;

namespace UnitySkillsCSharp
{
    /// <summary>
    /// Editor window for configuring the HTTP server port and .claude directory.
    /// Saves to Assets/Unity Skills CSharp/Editor/Config/config.json and syncs to .claude.
    /// </summary>
    public class ConfigWindow : EditorWindow
    {
        private int    m_Port;
        private string m_ClaudeDir;

        private static bool s_IsReopening;
        private bool        m_Saved;

        /// <summary>
        /// Open the config window, or focus it if already open.
        /// </summary>
        public static void Open()
        {
            var window = GetWindow<ConfigWindow>("Unity Skills CSharp Config");
            window.minSize = new Vector2(Const.ConfigWindowMinWidth, Const.ConfigWindowMinHeight);
            window.LoadConfig();
            window.Show();
        }

        public static void OpenFromMenu() => Open();

        private void OnEnable()
        {
            minSize = new Vector2(Const.ConfigWindowMinWidth, Const.ConfigWindowMinHeight);
        }

        private void OnDisable()
        {
            // Reopen if config is still invalid AND user hasn't just saved successfully
            // Use delayCall to break the immediate close → disable → reopen loop
            if (m_Saved || s_IsReopening) return;
            if (!ConfigHelper.Valid)
            {
                s_IsReopening = true;
                EditorApplication.delayCall += () =>
                {
                    s_IsReopening = false;
                    if (!ConfigHelper.Valid)
                        Open();
                };
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // ---- Server Port ----
            EditorGUILayout.LabelField("Server Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            int newPort = EditorGUILayout.IntField("Port", m_Port);
            if (newPort != m_Port) m_Port = Mathf.Clamp(newPort, 1, 65535);

            // ---- Claude Directory ----
            EditorGUILayout.BeginHorizontal();
            m_ClaudeDir = EditorGUILayout.TextField(".claude Directory", m_ClaudeDir);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select .claude Directory", m_ClaudeDir, "");
                if (!string.IsNullOrEmpty(selected))
                    m_ClaudeDir = selected;
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                // Real-time changes — could trigger dirty state if needed
            }

            GUILayout.Space(10);

            // ---- Save Button ----
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(m_ClaudeDir));
            if (GUILayout.Button("Save", GUILayout.Height(30)))
            {
                SaveSettings();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void LoadConfig()
        {
            var (port, claudeDir) = ConfigHelper.Load();
            m_Port      = port > 0 && port <= 65535 ? port : 7800;
            m_ClaudeDir = claudeDir ?? "";
        }

        private void SaveSettings()
        {
            if (string.IsNullOrEmpty(m_ClaudeDir))
            {
                EditorUtility.DisplayDialog("Save Failed", ".claude Directory cannot be empty.", "OK");
                return;
            }

            ConfigHelper.Save(m_Port, m_ClaudeDir);
            ConfigHelper.SyncToClaude(m_ClaudeDir);

            m_Saved = true;

            // Restart HTTP server with new port
            UnityHttpServer.StopIfRunning();
            UnityHttpServer.StartWithPort(m_Port);

            Debug.Log($"{Const.LogPrefixConfigWindow} Settings saved and server restarted on port {m_Port}.");
        }
    }
}
