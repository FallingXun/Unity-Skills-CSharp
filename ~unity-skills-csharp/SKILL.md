---
name: unity-skills-csharp
description: 对 Unity 的资源、预制体等操作行为，先编写 C# 编辑器脚本，再通过本地 HTTP 服务与 Unity Editor 通信，查询编译状态、执行 MenuItem 方法。Unity 侧运行 UnityHttpServer.cs（C# Editor 脚本），调用侧使用 unity_client.py（Python 异步客户端）。
---

## 基础规则
- 用户每个需要使用 Unity 进行操作的需求，作为一个任务，编写为一个 C# 脚本。
- 每个任务脚本包括多个步骤方法和一个执行方法。
- 每个步骤方法实现一个或多个操作行为。


## 参考文档

| 文档 | 说明 |
|------|------|
| [Unity HTTP 通信](references/unity-http-communication.md) | HTTP API、Python 客户端用法、端口配置 |
| [操作任务模板](references/unity-operating-task.md) | 预制体编辑、创建节点、查找组件等常用代码片段 |

## 文件结构

```
assets/
  config.json                       # 端口配置（C# 和 Python 共享）
  template/csharp/
    Task_xxx.cs                     # 任务脚本模板
references/
  unity-http-communication.md      # HTTP 通信参考
  unity-operating-task.md          # 操作任务模板参考
scripts/
  unity_client.py                   # Python 异步客户端
```

包内 Editor 脚本（安装后自动生效）：
```
Editor/Server/UnityHttpServer.cs    # HTTP 服务端
Editor/Tools/UnitySkillInstaller.cs # Skill 安装器
```

## 安装与配置

1. 通过 Unity Package Manager 添加本包（本地路径或 Git URL）
2. 确保项目已安装 **Newtonsoft.Json**（Package Manager 搜索 `com.unity.nuget.newtonsoft-json`）
3. 安装 Python 依赖：`pip install aiohttp`
4. Unity 编译完成后，HTTP 服务器自动启动，同时将 Skill 安装到 `.claude/skills/unity-skills-csharp/`

详细的 HTTP API 和 Python 客户端用法参见 [HTTP 通信参考文档](references/unity-http-communication.md)。


## 任务执行流程
当提供了需求内容时，需要按如下步骤执行：

1. 加载[操作任务模板](references/unity-operating-task.md)和[Unity HTTP 通信](references/unity-http-communication.md)。
2. 创建一个 C# 任务脚本。
3. 拆分任务步骤。
4. 为每个步骤编写具体操作逻辑。
5. 触发 Unity 资源刷新。
6. 每隔 2 秒查询编译状态，等待脚本编译完成，如有错误则进行修复。
7. 通过菜单栏 `Unity Skills CSharp/{任务编号}/Execute` 命令，执行任务。
8. 如果执行失败，则根据失败返回的信息，重新创建新的任务和 C# 脚本，将未完成的内容整理到新的脚本中，并再次执行。