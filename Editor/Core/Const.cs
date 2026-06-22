namespace UnitySkillsCSharp
{
    /// <summary>
    /// All shared constants for the Unity Skills CSharp package.
    /// </summary>
    public static class Const
    {
        // -------------------------------------------------------------- menu group headers

        public const string MenuGroupConfigHeader = "Unity Skills CSharp/Config";
        public const string MenuGroupSkillHeader  = "Unity Skills CSharp/Skill";
        public const string MenuGroupServerHeader = "Unity Skills CSharp/Server";
        public const string MenuGroupTaskHeader   = "Unity Skills CSharp/Task";

        // -------------------------------------------------------------- menu items — Config

        public const string MenuGroupConfig = MenuGroupConfigHeader + "/Settings";

        // -------------------------------------------------------------- menu items — Skill

        public const string MenuGroupSkillInstall = MenuGroupSkillHeader + "/Install";
        public const string MenuGroupSkillUpdate  = MenuGroupSkillHeader + "/Update";

        // -------------------------------------------------------------- menu items — Server

        public const string MenuGroupServerStart     = MenuGroupServerHeader + "/Start";
        public const string MenuGroupServerStop      = MenuGroupServerHeader + "/Stop";
        public const string MenuGroupServerRestart   = MenuGroupServerHeader + "/Restart";
        public const string MenuGroupServerAutoStart = MenuGroupServerHeader + "/Auto Start";

        // -------------------------------------------------------------- menu items — Task

        public const string MenuGroupTaskClear = MenuGroupTaskHeader + "/Clear";

        // -------------------------------------------------------------- log prefixes

        public const string LogPrefixHttpServer     = "[UnityHttpServer]";
        public const string LogPrefixSkillInstaller = "[SkillHelper]";
        public const string LogPrefixTaskHelper     = "[TaskHelper]";
        public const string LogPrefixConfigHelper   = "[ConfigHelper]";
        public const string LogPrefixConfigWindow   = "[ConfigWindow]";
        public const string LogPrefixInitialization = "[Initialization]";

        // -------------------------------------------------------------- paths

        public const string SourceFolder = "~unity-skills-csharp";
        public const string DestName     = "unity-skills-csharp";
        public const string SkillsDir    = ".claude/skills";
        public const string TaskDir      = "Assets/Unity Skills CSharp/Editor/Task";

        // -------------------------------------------------------------- HTTP

        public const string PathStatus  = "/status";
        public const string PathCall    = "/call";

        public const string KeySuccess  = "success";
        public const string KeyError    = "error";
        public const string KeyStatus   = "status";
        public const string KeyPort     = "port";
        public const string KeyErrors   = "errors";
        public const string KeyMenuItem = "menuItem";

        public const string StatusIdle         = "idle";
        public const string StatusCompiling    = "compiling";
        public const string StatusCompileError = "compile_error";
        public const string StatusExecuting    = "executing";
        public const string StatusUnknown      = "unknown";

        public const int    CallTimeoutSec     = 10;
        public const int    ThreadJoinMs       = 1000;
        public const string HttpServerThreadName = "UnityHttpServerThread";
        public const string ContentTypeJson    = "application/json; charset=utf-8";

        // -------------------------------------------------------------- prefs

        public const string AutoStartPref = "UnityHttpServer.AutoStart";

        // -------------------------------------------------------------- window

        public const float ConfigWindowMinWidth  = 420f;
        public const float ConfigWindowMinHeight = 160f;
    }
}
