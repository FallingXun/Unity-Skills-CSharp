# Unity 操作任务
- 对 Unity 的资源、预制体等操作行为，常用的 C# 任务编辑模板。

## 脚本生成规范
### 任务脚本
- 创建一个 C# 任务脚本，需要按照[任务脚本模板](assets/template/csharp/Task_xxx.cs)进行。
- 任务脚本路径为： `Assets/Unity Skills CSharp/Editor/{日期}/Task_{任务编号}.cs`，任务编号为`{日期}_{3位序号}`，如：`Assets/Unity Skills CSharp/Editor/20260417/Task_20260417_001.cs`。
### 任务步骤
- 拆分任务步骤。可按不同方式进行拆分，如：
    - 方式一：创建预制体节点结构树 → 对每个节点添加所需组件 → 为每个组件设置对应属性 → 设置根节点组件的引用关系 → ...
    - 方式二：创建节点一 + 添加组件 + 设置组件属性 + 设置根节点组件的引用 → 创建节点二 + 添加组件 + 设置组件属性 + 设置根节点组件的引用  → ... → 设置根节点组件引用的预制体 → ...
    - 方式三：...
- 为每个步骤编写具体操作逻辑时，优先参考下方[基础操作模板](#基础操作模板)，再根据不同项目的常用组件模板进行补充调整。

---

## 基础操作模板

> 以下代码片段可直接复制到任务脚本的步骤方法中使用。

### 1. 预制体编辑基本框架

**预制体层级修改必须使用三步流程**：`LoadPrefabContents → 修改 → SaveAsPrefabAsset / UnloadPrefabContents`，**禁止**对 `AssetDatabase.LoadAssetAtPath` 返回的资源对象直接做层级修改。**所有添加节点、修改层级、增减组件的操作都必须在此框架内完成。**

```csharp
var prefabPath = "{预制体路径}";
var contents = PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var root = contents.transform;
    // === 在此处执行所有修改 ===

    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
}
finally
{
    PrefabUtility.UnloadPrefabContents(contents);
}
```

### 2. 创建新预制体并保存

**必须先在内存构建完整层级 → 一次性 `SaveAsPrefabAsset` → `DestroyImmediate` 内存对象。**

```csharp
var savePath = "{预制体路径}";

// 幂等检查
if (AssetDatabase.LoadAssetAtPath<GameObject>(savePath) != null)
{
    Debug.Log($"Already exists: {savePath}"); return;
}

// 1. 内存中构建根节点（按类型挂载必要组件）
var rootGo = new GameObject("{预制体名}");
var rootRt = rootGo.AddComponent<RectTransform>();
rootRt.sizeDelta = new Vector2(600, 400);
rootGo.AddComponent<CanvasRenderer>();

// Panel 类型示例根节点组件：
rootGo.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
rootGo.AddComponent<GraphicRaycaster>();

// 2. 添加子节点...（用 CreateNode / CN / Stretch 辅助方法）

// 3. 设置子节点组件及引用绑定关系等

// 4. 保存资源
PrefabUtility.SaveAsPrefabAsset(rootGo, savePath);

// 5. 清理内存（必须）
UnityEngine.Object.DestroyImmediate(rootGo);
Debug.Log($"Prefab created: {savePath}");
```

---

### 3 辅助方法：查找节点 / 查找组件

放在类的顶部，供各步骤方法共用。

```csharp
// 查找指定路径的组件（找不到时输出 Warning，返回 null）
private static T FC<T>(Transform root, string path) where T : Component
{
    var tf = root.Find(path);
    if (tf == null) { Debug.LogWarning($"[Task] Path not found: {path}"); return null; }
    var c = tf.GetComponent<T>();
    if (c == null) Debug.LogWarning($"[Task] {typeof(T).Name} not found on: {path}");
    return c;
}

// 查找指定路径的 Transform（找不到时输出 Warning，返回 null）
private static Transform FT(Transform root, string path)
{
    var tf = root.Find(path);
    if (tf == null) Debug.LogWarning($"[Task] Path not found: {path}");
    return tf;
}
```

---

### 4. 辅助方法：创建 UI 节点

创建带 `RectTransform` + `CanvasRenderer` 的基础 UI 节点。**UI 界面所有节点都必须挂载 `RectTransform`。**

```csharp
// 完整版（指定锚点和轴心）
private static GameObject CreateNode(string name, Transform parent,
    Vector2 anchoredPos, Vector2 sizeDelta,
    Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
{
    var go = new GameObject(name);
    var rt = go.AddComponent<RectTransform>();
    rt.SetParent(parent, false);
    rt.anchorMin        = anchorMin;
    rt.anchorMax        = anchorMax;
    rt.pivot            = pivot;
    rt.anchoredPosition = anchoredPos;
    rt.sizeDelta        = sizeDelta;
    go.AddComponent<CanvasRenderer>();
    return go;
}

// 简化版：居中锚点 (0.5, 0.5)
private static GameObject CN(string name, Transform parent, Vector2 pos, Vector2 size)
    => CreateNode(name, parent, pos, size,
        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

// 全屏拉伸节点（anchorMin=0,0  anchorMax=1,1  offset=0）
private static GameObject Stretch(string name, Transform parent)
{
    var go = new GameObject(name);
    var rt = go.AddComponent<RectTransform>();
    rt.SetParent(parent, false);
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.offsetMin = Vector2.zero;
    rt.offsetMax = Vector2.zero;
    go.AddComponent<CanvasRenderer>();
    return go;
}
```

---

