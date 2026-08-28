# 底座管线（HtmlBaseTransformer）

`HtmlTransformer.Base` 是所有转换器共用的底座，不关心具体的输出目标。

工作方式：输入是一套简化版的通用 HTML，网页可以直接渲染它；每个 Transformer 把它转换成自己的场景所需要的格式。转换以网页版的表现为基准，尽量做到接近。

底座把转换过程拆成 5 个阶段，每个阶段由「配置（Config）」与「管线工作者（`Base/Pipeline`）」配合完成，由 `HtmlBaseTransformer`（抽象类）统一编排。

## 管线概览

```
        ┌─────────────────────────────────────────────┐
HTML ──►│ Load ─► Sanitize ─► Normalize ─► Transform ─►│──► Finalize ─► 富文本
        └─────────────────────────────────────────────┘
```

对应 `HtmlBaseTransformer.Process(html)` 的执行流程：

| # | 阶段 | 管线工作者 | 配置类 | 职责 |
|---|------|------|--------|------|
| 1 | Load | `ParserLoader` | `LoadConfig` / `OutputConfig` | 预处理输入、解析为 `HtmlDocument` |
| 2 | Sanitize | `ParserLoader` | `SanitizeConfig` | 按白名单净化标签与属性 |
| 3 | Normalize | `ParserTransformer` | `NormalizeConfig` | 标签归一化 |
| 4 | Transform | `ParserTransformer` | `TransformConfig` | 按序执行扩展（插件） |
| 5 | Finalize | `ParserFinalizer` | `FinalizeConfig` | 提取 body 内容、收尾后处理 |

> 管线工作者（`Base/Pipeline`）：无状态地执行各阶段，依赖由 `HtmlBaseTransformer.Process()` 以方法参数注入，设计细节见 [插件与管线设计](plugin-design.md)。

## 各阶段说明

### 1. Load（预处理 + 解析）

- 先执行 `LoadConfig` 注册的**预处理器**（如去换行），结果存入 `dataConfig.DocHtml`；
- 以预处理结果解析 `HtmlDocument`；
- 随后执行 `SanitizeConfig` 的净化。

### 2. Sanitize（白名单净化）

规则（`SanitizeConfig`）：

- `AddTag(tag, type, "class", "style")`：声明标签的**解析类型**并允许指定属性；未列出的属性被移除；
- `AddTag(tag, type, ":all")`：不进行属性过滤；
- `AddTag(tag, type)`：过滤掉所有属性；
- `AddTag` 的第二参数（解析类型）原则上应省略，用 `SanitizeConfig` 提供的语义化常量（见下表）或 `HtmlElementFlag` 组合值显式声明；类型会同步写入 `HtmlNode.ElementsFlags`，配置须在首次解析前完成；
- `ClearTagFlags()`：清空 `HtmlNode.ElementsFlags` 全部预置（所有标签回落普通容器），须在首次解析前调用；
- `ClearTagFlags(tag)`：移除单个标签的解析类型，使其回落普通容器，须在首次解析前调用；
- **白名单之外的标签被「解包」**：标签删除但子节点保留（文本不会丢失）；
- **文本节点永远保留**，不会因净化被清空。

解析类型（`HtmlElementFlag`，[Flags] 可组合）：

| 含义 | `SanitizeConfig` 语义常量 | 原始值 |
|------|---------------------------|--------|
| 普通容器：可有内容与子节点 | `ElementTypeNormal` | `0` |
| 空元素（void）：只能空内容 | `ElementTypeEmpty` | `HtmlElementFlag.Empty` |
| 闭合标签等价于开标签 | `ElementTypeClosed` | `HtmlElementFlag.Closed` |
| void 自闭合（如 `<br>`） | `ElementTypeVoid` | `Empty \| Closed` |
| CData：内容原样保留，不解析为子标签 | `ElementTypeCData` | `HtmlElementFlag.CData` |
| 可重叠（历史语义，如 form/a） | `ElementTypeCanOverlap` | `HtmlElementFlag.CanOverlap` |

写入 `HtmlNode.ElementsFlags` 存在**并发竞态**与**单进程多套声明互斥**两个已知缺陷，详见文末 [已知问题](#已知问题known-issue)。

### 3. Normalize（标签归一化）

`AddTagMapping(replaceTagName, params findTagNames)` 把不同写法的标签**归一化**到统一的目标标签，属性与子节点一并保留。

单个源标签：

```csharp
AddTagMapping("b", "strong");   // <strong> → <b>
```

多个源标签映射到同一个目标标签，需放在**同一次调用**里（同名目标再次调用会整组覆盖）：

```csharp
AddTagMapping("i", "em", "dfn");   // <em> 和 <dfn> → <i>
```

应用示例：

| 输入 | 输出 |
|------|------|
| `<strong>x</strong>` | `<b>x</b>` |
| `<em>x</em>` / `<dfn>x</dfn>` | `<i>x</i>` |

### 4. Transform（扩展 / 插件执行）

`TransformConfig` 维护一个**有序**扩展列表：

- `RegisterExtension(name, extension)` 注册插件；
- `SetOrders(...)` / `MoveExtensionBefore` / `MoveExtensionAfter` 调整执行顺序；
- 每个插件实现 `IExtensionInterface.Transform(HtmlDocument)`，在文档上就地修改节点。

### 5. Finalize（收尾）

- 先执行 `FinalizeConfig` 注册的 before 钩子（可再修改文档）；
- 提取 `<body>` 内部 HTML（片段未包裹 `html/body` 时退化为取 `DocumentNode` 子节点）；
- 再执行 after 钩子对字符串做后处理（属性改写、实体反转义、去尾换行等）。

## 插件接口（IExtensionInterface）

所有转换器插件实现同一个接口：

```csharp
public interface IExtensionInterface
{
    void Transform(HtmlDocument doc);
}
```

插件只依赖 `HtmlAgilityPack.HtmlDocument`，与具体输出目标解耦。设计思路、约束与编写规范见 [插件与管线设计](plugin-design.md)。

## 工具类

`HtmlParseUtils`（`Base/Utils`）提供插件可复用的解析工具：

| 方法 | 说明 |
|------|------|
| `GetStyleAttrs(style)` | 解析 CSS `style` 值，返回键值对（如 `--color`） |
| `ColorToHex(color)` | 颜色转 `#RRGGBB[AA]`，支持 `#RGB/#RGBA/#RRGGBB/#RRGGBBAA/rgb()/rgba()` |
| `SizeToNumber(size)` | 尺寸字符串转纯数字（非法/带单位返回空串） |

## 编写一个新的转换器

从零编写一个新转换器（入口约定、`Configure()` 模板、插件、命名空间与文档约定）见 [编写一个新转换器](writing-a-transformer.md)。

## 已知问题（Known Issue）

`HtmlNode.ElementsFlags` 是 HAP 的全局可变字典，本库通过 `AddTag(tag, type)` 与 `ClearTagFlags()` 在运行时改写它（这是「不改 HAP 源码」方案的代价），存在两个已知缺陷：

- **不防并发**：HAP 内部对 `ElementsFlags` 的读取不加锁，配置阶段的写入若与解析并发即竞态。规避：配置集中在进程启动、首个解析之前一次性完成，之后不再写。
- **全局限一**：全局只有一张表，同一进程同时运行多套「解析类型声明」不同的转换器会互相覆盖。规避：进程内只维护一套解析类型声明。

`static` 帮不上忙——它只锁住字段引用，锁不住字典内容的增改。
