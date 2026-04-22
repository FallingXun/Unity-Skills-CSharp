---
name: unity-skills-csharp
description: 对 Unity 的资源、预制体等操作行为，先编写 C# 编辑器脚本，再通过本地 HTTP 服务与 Unity Editor 通信，查询编译状态、执行 MenuItem 方法。Unity 侧运行 UnityHttpServer.cs（C# Editor 脚本），调用侧使用 unity_client.py（Python 异步客户端）。
---

## 基础规则

- 用户每个需要使用 Unity 进行操作的需求，作为一个任务，编写为一个 C# 脚本。
- 每个任务脚本包括多个步骤方法和一个执行方法。
- 每个步骤方法实现一个或多个操作行为。

## 任务执行流程

当提供了需求内容时，需要按如下步骤执行：

1. 根据[任务脚本模板](assets/template/csharp/Task_xxx.cs)创建一个 C# 任务脚本。脚本路径为 `Assets/Unity Skills CSharp/Editor/Task_{任务编号}.cs`，任务编号格式为 `{日期}_{3位序号}`，如 `Task_20260417_001.cs`。
2. 拆分任务步骤，可按不同方式进行拆分，如：
   - 方式一：创建预制体节点结构树 → 对每个节点添加所需组件 → 设置组件属性 → 设置根节点引用
   - 方式二：逐节点处理（创建节点 + 添加组件 + 设置属性 + 设置引用）→ ... → 最终关系绑定
3. 为每个步骤编写具体操作逻辑。编写时优先参考[基础操作模板](references/unity-operating-task.md)，再根据不同项目的常用组件模板进行补充调整。
4. 每隔 2 秒查询编译状态，等待脚本编译完成，如有错误则进行修复。
5. 通过菜单栏 `Unity Skills CSharp/{任务编号}/Execute` 执行任务。
6. 如果执行失败，根据返回的错误信息重新创建新的任务脚本，整理未完成内容后再次执行。

## 参考文档

| 文档 | 说明 |
|------|------|
| [HTTP 通信](references/unity-http-communication.md) | HTTP API、Python 客户端用法、端口配置 |
| [操作任务模板](references/unity-operating-task.md) | 预制体编辑、创建节点、查找组件等常用代码片段 |

## 文件结构

```
assets/
  config.ini                        # 端口配置（C# 和 Python 共享）
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
