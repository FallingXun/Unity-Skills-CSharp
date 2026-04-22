using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnitySkillsCSharp
{
    public enum ServerStatus
    {
        Idle,
        Compiling,
        CompileError,
        Executing,
    }

    [InitializeOnLoad]
    public static class UnityHttpServer
    {
        // -------------------------------------------------------------- constants

        public const int DefaultPort = 7800;

        private const string k_MenuRoot        = "Unity Skills CSharp/Http Server";
        private const string k_MenuStart       = k_MenuRoot + "/Start";
        private const string k_MenuStop        = k_MenuRoot + "/Stop";
        private const string k_MenuRestart     = k_MenuRoot + "/Restart";
        private const string k_MenuAutoStart   = k_MenuRoot + "/Auto Start";

        private const string k_AutoStartPref   = "UnityHttpServer.AutoStart";
        private const string k_ConfigFile      = "config.ini";
        private const string k_ConfigSection   = "server";
        private const string k_ConfigPortKey   = "port";
        private const string k_ConfigSkillPath = ".claude/skills/unity-skills-csharp/assets";
        private const string k_LogPrefix       = "[UnityHttpServer]";
        private const string k_ThreadName      = "UnityHttpServerThread";
        private const string k_ContentType     = "application/json; charset=utf-8";

        private const string k_PathStatus      = "/status";
        private const string k_PathCall        = "/call";

        private const string k_KeySuccess      = "success";
        private const string k_KeyError        = "error";
        private const string k_KeyStatus       = "status";
        private const string k_KeyPort         = "port";
        private const string k_KeyErrors       = "errors";
        private const string k_KeyMenuItem     = "menuItem";

        private const string k_StatusIdle         = "idle";
        private const string k_StatusCompiling    = "compiling";
        private const string k_StatusCompileError = "compile_error";
        private const string k_StatusExecuting    = "executing";
        private const string k_StatusUnknown      = "unknown";

        private const int    k_CallTimeoutSec  = 10;
        private const int    k_ThreadJoinMs    = 1000;

        // -------------------------------------------------------------- fields

        private static HttpListener m_Listener;
        private static Thread m_ListenerThread;
        private static int m_Port;
        private static volatile ServerStatus m_Status = ServerStatus.Idle;
        private static volatile string[] m_CompileErrors = Array.Empty<string>();

        private static readonly Queue<Action> m_MainThreadQueue = new Queue<Action>();
        private static readonly object m_QueueLock = new object();
        private static readonly object m_CompileErrorsLock = new object();

        public static bool AutoStart
        {
            get => EditorPrefs.GetBool(k_AutoStartPref, true);
            set => EditorPrefs.SetBool(k_AutoStartPref, value);
        }

        public static int Port => m_Port;

        public static void SetPort(int port)
        {
            bool wasRunning = m_Listener != null;
            if (wasRunning) StopServer();
            m_Port = port;
            SavePortToIni(port);
            if (wasRunning) StartServer();
        }

        // -------------------------------------------------------------- lifecycle

        static UnityHttpServer()
        {
            m_Port = LoadPortFromIni();

            CompilationPipeline.compilationStarted += _ =>
            {
                m_Status = ServerStatus.Compiling;
                m_CompileErrors = Array.Empty<string>();
            };
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            EditorApplication.update += ProcessQueue;
            EditorApplication.quitting += Shutdown;

            if (AutoStart)
                StartServer();
        }

        [MenuItem(k_MenuStart)]
        public static void StartMenuItem()
        {
            if (m_Listener != null) { Debug.Log($"{k_LogPrefix} Already running."); return; }
            StartServer();
        }

        [MenuItem(k_MenuStop)]
        public static void StopMenuItem() => StopServer();

        [MenuItem(k_MenuRestart)]
        public static void RestartMenuItem() { StopServer(); StartServer(); }

        [MenuItem(k_MenuAutoStart)]
        public static void ToggleAutoStart() => AutoStart = !AutoStart;

        [MenuItem(k_MenuAutoStart, true)]
        public static bool ToggleAutoStartValidate()
        {
            Menu.SetChecked(k_MenuAutoStart, AutoStart);
            return true;
        }

        private static void StartServer()
        {
            UnitySkillInstaller.Install();
            EnsureConfigFile();
            if (!TryBindListener())
            {
                Debug.LogError($"{k_LogPrefix} Failed to start on port {Port}.");
                return;
            }

            Debug.Log($"{k_LogPrefix} Listening on http://localhost:{Port}/");
            m_ListenerThread = new Thread(ListenLoop) { IsBackground = true, Name = k_ThreadName };
            m_ListenerThread.Start();
        }

        private static void StopServer()
        {
            m_Listener?.Stop();
            m_Listener = null;
            m_ListenerThread?.Join(k_ThreadJoinMs);
            m_ListenerThread = null;
        }

        private static void Shutdown()
        {
            EditorApplication.update -= ProcessQueue;
            StopServer();
        }
         
        private static bool TryBindListener()
        {
            try
            {
                m_Listener = new HttpListener();
                m_Listener.Prefixes.Add($"http://localhost:{Port}/");
                m_Listener.Start();
                return true;
            }
            catch
            {
                m_Listener = null;
                return false;
            }
        }

        // -------------------------------------------------------------- listen loop

        private static void ListenLoop()
        {
            while (m_Listener != null && m_Listener.IsListening)
            {
                try
                {
                    var ctx = m_Listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(ctx));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        // -------------------------------------------------------------- request handlers

        private static void HandleRequest(HttpListenerContext ctx)
        {
            string path = ctx.Request.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();
            try
            {
                switch (path)
                {
                    case k_PathStatus: SendResponse(ctx, HandleStatus());          break;
                    case k_PathCall:   SendResponse(ctx, HandleCall(ctx.Request)); break;
                    default:           SendResponse(ctx, Response(false, "endpoint not found"), 404); break;
                }
            }
            catch (Exception e)
            {
                SendResponse(ctx, Response(false, e.Message), 500);
            }
        }

        private static JObject HandleStatus()
        {
            var obj = Response(true);
            obj[k_KeyStatus] = StatusToString(m_Status);
            obj[k_KeyPort]   = Port;

            if (m_Status == ServerStatus.CompileError && m_CompileErrors.Length > 0)
            {
                var arr = new JArray();
                foreach (var msg in m_CompileErrors)
                    arr.Add(msg);
                obj[k_KeyErrors] = arr;
            }

            return obj;
        }

        private static JObject HandleCall(HttpListenerRequest req)
        {
            string body;
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                body = reader.ReadToEnd();

            JObject payload;
            try { payload = JObject.Parse(body); }
            catch { return Response(false, "request body is not valid JSON"); }

            string menuItem = payload[k_KeyMenuItem]?.Value<string>();
            if (string.IsNullOrEmpty(menuItem))
                return Response(false, $"missing '{k_KeyMenuItem}' in request body");

            var tcs = new TaskCompletionSource<bool>();

            lock (m_QueueLock)
            {
                ServerStatus prevStatus = m_Status;
                m_MainThreadQueue.Enqueue(() =>
                {
                    try
                    {
                        m_Status = ServerStatus.Executing;
                        bool ok = EditorApplication.ExecuteMenuItem(menuItem);
                        m_Status = prevStatus;
                        tcs.SetResult(ok);
                    }
                    catch (Exception e)
                    {
                        m_Status = prevStatus;
                        tcs.SetException(e);
                    }
                });
            }

            try
            {
                if (!tcs.Task.Wait(TimeSpan.FromSeconds(k_CallTimeoutSec)))
                    return Response(false, $"timeout: main thread did not respond within {k_CallTimeoutSec}s");
            }
            catch (AggregateException ae)
            {
                return Response(false, ae.InnerException?.Message ?? "unknown error");
            }

            var result = Response(tcs.Task.Result);
            result[k_KeyMenuItem] = menuItem;
            return result;
        }

        // -------------------------------------------------------------- main-thread queue

        private static void OnBeforeAssemblyReload()
        {
            // Fallback: fires only if beforeAssemblyReload was not unsubscribed
            // (e.g. play-mode domain reload outside of a compilation cycle).
            m_Status = ServerStatus.Idle;
            StopServer();
        }

        private static void OnAssemblyCompilationFinished(string _, CompilerMessage[] messages)
        {
            bool hasErrors = false;
            foreach (var msg in messages)
                if (msg.type == CompilerMessageType.Error) { hasErrors = true; break; }

            if (!hasErrors) return;

            lock (m_CompileErrorsLock)
            {
                var errors = new List<string>(m_CompileErrors);
                foreach (var msg in messages)
                    if (msg.type == CompilerMessageType.Error)
                        errors.Add(msg.message);
                m_CompileErrors = errors.ToArray();
            }
        }

        private static void OnCompilationFinished(object context)
        {
            if (m_CompileErrors.Length > 0)
            {
                m_Status = ServerStatus.CompileError;
            }
            else
            {
                // No errors — domain reload is imminent.
                // Unsubscribe beforeAssemblyReload since we handle cleanup here.
                // The static constructor will re-subscribe and restart after reload.
                AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
                m_Status = ServerStatus.Idle;
                StopServer();
            }
        }

        private static void ProcessQueue()
        {
            if (EditorApplication.isCompiling && m_Status != ServerStatus.Compiling)
                m_Status = ServerStatus.Compiling;

            lock (m_QueueLock)
            {
                while (m_MainThreadQueue.Count > 0)
                    m_MainThreadQueue.Dequeue()?.Invoke();
            }
        }

        // -------------------------------------------------------------- helpers

        private static string GetConfigPath()
        {
            return Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                k_ConfigSkillPath, k_ConfigFile);
        }

        private static void EnsureConfigFile()
        {
            string configPath = GetConfigPath();
            if (!File.Exists(configPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                File.WriteAllText(configPath,
                    $"[{k_ConfigSection}]\n{k_ConfigPortKey} = {DefaultPort}\n");
            }
        }

        private static int LoadPortFromIni()
        {
            string configPath = GetConfigPath();
            if (!File.Exists(configPath))
                return DefaultPort;

            string currentSection = "";
            foreach (var line in File.ReadAllLines(configPath))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed[0] == ';' || trimmed[0] == '#') continue;
                if (trimmed[0] == '[' && trimmed[trimmed.Length - 1] == ']')
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2).Trim().ToLowerInvariant();
                    continue;
                }
                if (currentSection != k_ConfigSection) continue;
                int eq = trimmed.IndexOf('=');
                if (eq < 0) continue;
                string key = trimmed.Substring(0, eq).Trim();
                string val = trimmed.Substring(eq + 1).Trim();
                if (key == k_ConfigPortKey && int.TryParse(val, out int port))
                    return port;
            }
            return DefaultPort;
        }

        private static void SavePortToIni(int port)
        {
            string configPath = GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));

            if (File.Exists(configPath))
            {
                var lines = new List<string>(File.ReadAllLines(configPath));
                bool inSection = false, found = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    string trimmed = lines[i].Trim();
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        inSection = trimmed.Substring(1, trimmed.Length - 2).Trim().ToLowerInvariant() == k_ConfigSection;
                        continue;
                    }
                    if (!inSection) continue;
                    int eq = trimmed.IndexOf('=');
                    if (eq < 0) continue;
                    if (trimmed.Substring(0, eq).Trim() == k_ConfigPortKey)
                    {
                        lines[i] = $"{k_ConfigPortKey} = {port}";
                        found = true;
                        break;
                    }
                }
                if (!found) lines.Add($"{k_ConfigPortKey} = {port}");
                File.WriteAllText(configPath, string.Join("\n", lines) + "\n");
            }
            else
            {
                File.WriteAllText(configPath,
                    $"[{k_ConfigSection}]\n{k_ConfigPortKey} = {port}\n");
            }
        }

        private static void SendResponse(HttpListenerContext ctx, JObject body, int statusCode = 200)
        {
            var res = ctx.Response;
            res.StatusCode      = statusCode;
            res.ContentType     = k_ContentType;
            byte[] buf          = Encoding.UTF8.GetBytes(body.ToString(Newtonsoft.Json.Formatting.None));
            res.ContentLength64 = buf.Length;
            res.OutputStream.Write(buf, 0, buf.Length);
            res.Close();
        }

        private static string StatusToString(ServerStatus status)
        {
            switch (status)
            {
                case ServerStatus.Idle:         return k_StatusIdle;
                case ServerStatus.Compiling:    return k_StatusCompiling;
                case ServerStatus.CompileError: return k_StatusCompileError;
                case ServerStatus.Executing:    return k_StatusExecuting;
                default:                        return k_StatusUnknown;
            }
        }

        private static JObject Response(bool success, string error = null)
        {
            var obj = new JObject { [k_KeySuccess] = success };
            if (error != null) obj[k_KeyError] = error;
            return obj;
        }
    }
}
