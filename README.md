# Unity Skills CSharp

一个 Unity Editor 包，为 AI 辅助的 Unity 资源操作提供自动化能力。在 Unity Editor 内部启动本地 HTTP 服务器，并提供 Python 异步客户端，使 Claude Code Skill 能够查询编译状态、通过 MenuItem 执行编辑器操作。

## 环境要求

- Unity 2022.3+
- [Newtonsoft.Json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@latest)（`com.unity.nuget.newtonsoft-json`）
- Python 3.9+，需安装 `aiohttp`（`pip install aiohttp`）
- Windows（INI 读写依赖 `kernel32.dll`）

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

Unity 编译完成后，HTTP 服务器会自动启动并将 Skill 文件安装到 `.claude/skills/unity-skills-csharp/`。也可以手动触发：

```
Unity Skills CSharp > Skills > Install
```

### 3. 安装 Python 依赖

```bash
pip install aiohttp
```

## 配置

共享配置文件位于：

```
.claude/skills/unity-skills-csharp/assets/config.ini
```

```ini
[server]
port = 7800

[project]
root_path = ""
```

| Section | Key | 默认值 | 说明 |
|---------|-----|--------|------|
| `[server]` | `port` | `7800` | HTTP 服务器监听端口 |
| `[project]` | `root_path` | *(空)* | Skill 安装目标路径，为空时使用 Unity 工程根目录 |

通过代码修改端口（同步写回 INI 并自动重启服务器）：

```csharp
UnitySkillsCSharp.UnityHttpServer.SetPort(8080);
```

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

`Unity Skills CSharp > Server`

| 菜单项 | 说明 |
|--------|------|
| Start | 手动启动 HTTP 服务器 |
| Stop | 停止服务器 |
| Restart | 重启服务器 |
| Auto Start ✓ | 切换启动时自动开启（持久化到 `EditorPrefs`） |

`Unity Skills CSharp > Skills > Install` — 将 Skill 文件复制到 `.claude/skills/unity-skills-csharp/`。

## 包结构

```
Editor/
  Server/
    UnityHttpServer.cs       # HTTP 服务端（[InitializeOnLoad]）
  Tools/
    UnitySkillInstaller.cs   # 将 Skill 文件安装到 .claude/skills/
  Utils/
    IniUtils.cs              # kernel32 INI 读写封装
  UnitySkillsCSharp.Editor.asmdef
~unity-skills-csharp/        # Claude Code Skill 资源包
  SKILL.md
  assets/
    config.ini               # 端口与工程路径配置
    template/csharp/
      Task_xxx.cs            # 任务脚本模板
  references/
    unity-http-communication.md
    unity-operating-task.md
  scripts/
    unity_client.py          # Python 异步客户端
```

## License

[MIT](LICENSE)
