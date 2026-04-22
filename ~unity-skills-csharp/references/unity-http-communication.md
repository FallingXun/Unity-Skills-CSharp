# Unity HTTP 通信

通过本地 HTTP 服务与 Unity Editor 通信，查询编译状态、执行 MenuItem 方法。

## 安装

```bash
pip install aiohttp
```

端口配置共享 `assets/config.ini`（安装后位于 `.claude/skills/unity-skills-csharp/assets/config.ini`）：

```ini
[server]
port = 7800
```

通过代码修改端口（同步写回 INI 并自动重启）：

```csharp
UnitySkills.UnityHttpServer.SetPort(8080);
```

## HTTP API

### GET `/status` — 查询状态

```json
{ "success": true, "status": "idle", "port": 7800 }
```

编译失败时附带错误列表：

```json
{ "success": true, "status": "compile_error", "port": 7800,
  "errors": ["Assets/Foo.cs(10,5): error CS0246: ..."] }
```

| status 值 | 含义 |
|-----------|------|
| `idle` | 空闲，可接受操作 |
| `compiling` | 编译中 |
| `compile_error` | 编译失败 |
| `executing` | 正在执行 MenuItem |

### POST `/call` — 执行 MenuItem

请求体：
```json
{ "menuItem": "File/Save Project" }
```

响应：
```json
{ "success": true, "menuItem": "File/Save Project" }
{ "success": false, "error": "timeout: main thread did not respond within 10s" }
```

所有响应均包含 `success` 字段；失败时附加 `error` 字段。

## Python 客户端用法

### 便捷调用

模块级单例跨调用复用 session：

```python
from unity_client import check_unity_status, execute_menu_item

result = await check_unity_status()
# {"success": true, "status": "idle", "port": 7800}

result = await execute_menu_item("File/Save Project")
# {"success": true, "menuItem": "File/Save Project"}
```

### 显式管理

```python
from unity_client import UnityClient

async with UnityClient() as client:
    await client.connect()
    print(await client.status())
    print(await client.call("Assets/Reimport All"))
    idle = await client.wait_for_idle(timeout=60)
```

### CLI

```bash
python unity_client.py status
python unity_client.py call "File/Save Project"
```

## 常用 MenuItem 示例

| 操作 | MenuItem 路径 |
|------|--------------|
| 保存项目 | `File/Save Project` |
| 重新导入全部 | `Assets/Reimport All` |
| 清除 Console | `Window/General/Clear Console` |
| 运行游戏 | `Edit/Play` |
| 刷新资源 | `Assets/Refresh` |

## 注意事项

- 执行 MenuItem 前建议先调用 `wait_for_idle()` 等待编译完成
- `/call` 接口在主线程执行，超时时间 **10 秒**
- `AutoStart` 持久化在 `EditorPrefs`；端口配置持久化在 `assets/config.ini`
