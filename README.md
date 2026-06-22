# Unity Skills CSharp

一个 Unity Editor 包，为 AI 辅助的 Unity 资源操作提供自动化能力。在 Unity Editor 内部启动本地 HTTP 服务器，并提供 Python 异步客户端，使 Claude Code Skill 能够查询编译状态、通过 MenuItem 执行编辑器操作。

## 环境要求

- Unity 2022.3+
- [Newtonsoft.Json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@latest)（`com.unity.nuget.newtonsoft-json`）
- Python 3.9+，需安装 `aiohttp`（`pip install aiohttp`）
- Windows（HTTP 服务基于 `HttpListener`）

## 安装

### 1. 添加 UPM 包

通过 Unity Package Manager — **Add package from disk** 或 **Add package from git URL**：

```
https://github.com/FallingXun/Unity-Skills-CSharp.git
```

或直接编辑 `Packages/manifest.json`：

```json
{
  "dependencies": {
    "com.xun.unity-skills-csharp": "https://github.com/FallingXun/Unity-Skills-CSharp.git"
  }
}
```

### 2. 安装 Claude Code Skill

Unity 编译完成后，会自动将 Skill 文件安装到 `.claude/skills/unity-skills-csharp/`。也可以手动触发菜单 `Unity Skills CSharp > Install`。

### 3. 安装 Python 依赖

```bash
pip install aiohttp
```

## 配置

首次启动时若未检测到有效配置，会自动打开配置窗口（`Unity Skills CSharp > Settings`），设置：

- **Port** — HTTP 服务器监听端口（默认 7800）
- **.claude Directory** — `.claude` 目录路径（如 `D:/Projects/MyGame/.claude`）

点击 **Save** 保存配置，配置将写入 `Assets/Unity Skills CSharp/Editor/Config/config.json` 并自动同步到 `.claude/skills/unity-skills-csharp/assets/config.json`。

```json
{ "port": 7800, "claudeDir": "/absolute/path/.claude" }
```

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `port` | `7800` | HTTP 服务器监听端口 |
| `claudeDir` | *(空)* | `.claude` 目录的绝对路径，用于 Skill 安装和配置同步 |

## HTTP API

服务器在 `http://localhost:{port}/` 上提供两个接口，所有响应均包含 `success` 字段，失败时附加 `error` 字段。

### GET `/status` — 查询状态

```json
{ "success": true, "status": "idle", "port": 7800 }
```

| `status` | 含义 |
|----------|------|
| `idle` | 空闲，可接受操作 |
| `compiling` | 脚本编译中 |
| `compile_error` | 编译失败（附带 `errors` 数组） |
| `executing` | 正在执行 MenuItem |

### POST `/call` — 执行 MenuItem

**请求：**
```json
{ "menuItem": "File/Save Project" }
```

**响应（成功）：**
```json
{ "success": true, "menuItem": "File/Save Project" }
```

**响应（失败）：**
```json
{ "success": false, "error": "timeout: main thread did not respond within 10s" }
```

## Python 客户端

```python
from unity_client import check_unity_status, execute_menu_item

# 查询状态
result = await check_unity_status()
# {"success": true, "status": "idle", "port": 7800}

# 执行菜单操作
result = await execute_menu_item("File/Save Project")
# {"success": true, "menuItem": "File/Save Project"}
```

执行前等待编译完成：

```python
from unity_client import UnityClient

async with UnityClient() as client:
    await client.connect()
    await client.wait_for_idle(timeout=60)
    print(await client.call("Assets/Reimport All"))
```

命令行：

```bash
python unity_client.py status
python unity_client.py call "File/Save Project"
```

## 编辑器菜单

所有菜单项统一在 `Unity Skills CSharp` 下：

| 菜单项 | 说明 |
|--------|------|
| Settings | 打开配置窗口，设置端口和 .claude 目录 |
| Install | 安装 Skill 文件到 `.claude/skills/unity-skills-csharp/` |
| Update | 更新 Skill 文件，并同步 config.json 到 .claude |
| Start | 手动启动 HTTP 服务器 |
| Stop | 停止服务器 |
| Restart | 重启服务器 |
| Auto Start ✓ | 切换启动时自动开启（持久化到 `EditorPrefs`） |
| Clear | 清理任务文件夹 |

## 包结构

```
Editor/
  Core/
    Const.cs                    # 全局常量定义
    Initialization.cs           # [InitializeOnLoad] 入口，所有 MenuItem 分组管理
  Server/
    UnityHttpServer.cs          # HTTP 服务端
  Tools/
    SkillHelper.cs              # 将 Skill 文件安装到 .claude/skills/
    TaskHelper.cs               # 清理任务文件夹
    ConfigHelper.cs             # JSON 配置读写与同步
  Windows/
    ConfigWindow.cs             # 配置编辑器窗口
  UnitySkillsCSharp.Editor.asmdef
~unity-skills-csharp/           # Claude Code Skill 资源包
  SKILL.md
  assets/
    config.json                 # 端口与 .claude 路径配置（同步副本）
    template/csharp/
      Task_xxx.cs               # 任务脚本模板
  references/
    unity-http-communication.md
    unity-operating-task.md
  scripts/
    unity_client.py             # Python 异步客户端（读取 config.json）
```

## License

[MIT](LICENSE)
