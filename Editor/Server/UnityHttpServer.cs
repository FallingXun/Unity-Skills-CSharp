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

    public static class UnityHttpServer
    {
        // -------------------------------------------------------------- fields

        private static HttpListener m_Listener;
        private static Thread m_ListenerThread;
        private static int m_CurrentPort = 7800;
        private static volatile ServerStatus m_Status = ServerStatus.Idle;
        private static volatile string[] m_CompileErrors = Array.Empty<string>();

        private static readonly Queue<Action> m_MainThreadQueue = new Queue<Action>();
        private static readonly object m_QueueLock = new object();
        private static readonly object m_CompileErrorsLock = new object();

        private static bool s_Initialized;

        public static bool AutoStart
        {
            get => EditorPrefs.GetBool(Const.AutoStartPref, true);
            set => EditorPrefs.SetBool(Const.AutoStartPref, value);
        }

        public static int Port => m_CurrentPort;

        // -------------------------------------------------------------- initialization

        /// <summary>
        /// Register compilation hooks and EditorApplication callbacks.
        /// Called once by <see cref="Initialization"/>.
        /// Does NOT auto-start the server.
        /// </summary>
        public static void Initialize()
        {
            if (s_Initialized) return;
            s_Initialized = true;

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
        }

        /// <summary>
        /// Set the port and start the HTTP server.
        /// </summary>
        public static void StartWithPort(int port)
        {
            m_CurrentPort = port;
            StartServer();
        }

        /// <summary>
        /// Stop the server if it is currently running.
        /// </summary>
        public static void StopIfRunning()
        {
            if (m_Listener != null)
                StopServer();
        }

        // -------------------------------------------------------------- public methods

        public static void StartServerMenu()
        {
            if (m_Listener != null) { Debug.Log($"{Const.LogPrefixHttpServer} Already running."); return; }
            StartServer();
        }

        public static void StopServerMenu() => StopServer();

        public static void RestartServerMenu() { StopServer(); StartServer(); }

        public static void ToggleAutoStart() => AutoStart = !AutoStart;

        public static bool AutoStartChecked()
        {
            Menu.SetChecked(Const.MenuGroupServerAutoStart, AutoStart);
            return true;
        }

        // -------------------------------------------------------------- lifecycle

        private static void StartServer()
        {
            if (m_CurrentPort <= 0)
            {
                Debug.LogError($"{Const.LogPrefixHttpServer} Invalid port: {m_CurrentPort}.");
                return;
            }

            if (!TryBindListener())
            {
                Debug.LogError($"{Const.LogPrefixHttpServer} Failed to start on port {Port}.");
                return;
            }

            Debug.Log($"{Const.LogPrefixHttpServer} Listening on http://localhost:{Port}/");
            m_ListenerThread = new Thread(ListenLoop) { IsBackground = true, Name = Const.HttpServerThreadName };
            m_ListenerThread.Start();
        }

        private static void StopServer()
        {
            m_Listener?.Stop();
            m_Listener = null;
            m_ListenerThread?.Join(Const.ThreadJoinMs);
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
                    case Const.PathStatus: SendResponse(ctx, HandleStatus());          break;
                    case Const.PathCall:   SendResponse(ctx, HandleCall(ctx.Request)); break;
                    default:               SendResponse(ctx, Response(false, "endpoint not found"), 404); break;
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
            obj[Const.KeyStatus] = StatusToString(m_Status);
            obj[Const.KeyPort]   = Port;

            if (m_Status == ServerStatus.CompileError && m_CompileErrors.Length > 0)
            {
                var arr = new JArray();
                foreach (var msg in m_CompileErrors)
                    arr.Add(msg);
                obj[Const.KeyErrors] = arr;
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

            string menuItem = payload[Const.KeyMenuItem]?.Value<string>();
            if (string.IsNullOrEmpty(menuItem))
                return Response(false, $"missing '{Const.KeyMenuItem}' in request body");

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
                if (!tcs.Task.Wait(TimeSpan.FromSeconds(Const.CallTimeoutSec)))
                    return Response(false, $"timeout: main thread did not respond within {Const.CallTimeoutSec}s");
            }
            catch (AggregateException ae)
            {
                return Response(false, ae.InnerException?.Message ?? "unknown error");
            }

            var result = Response(tcs.Task.Result);
            result[Const.KeyMenuItem] = menuItem;
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
                // The Initialize() will re-subscribe and restart after reload.
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

        private static void SendResponse(HttpListenerContext ctx, JObject body, int statusCode = 200)
        {
            var res = ctx.Response;
            res.StatusCode      = statusCode;
            res.ContentType     = Const.ContentTypeJson;
            byte[] buf          = Encoding.UTF8.GetBytes(body.ToString(Newtonsoft.Json.Formatting.None));
            res.ContentLength64 = buf.Length;
            res.OutputStream.Write(buf, 0, buf.Length);
            res.Close();
        }

        private static string StatusToString(ServerStatus status)
        {
            switch (status)
            {
                case ServerStatus.Idle:         return Const.StatusIdle;
                case ServerStatus.Compiling:    return Const.StatusCompiling;
                case ServerStatus.CompileError: return Const.StatusCompileError;
                case ServerStatus.Executing:    return Const.StatusExecuting;
                default:                        return Const.StatusUnknown;
            }
        }

        private static JObject Response(bool success, string error = null)
        {
            var obj = new JObject { [Const.KeySuccess] = success };
            if (error != null) obj[Const.KeyError] = error;
            return obj;
        }
    }
}
