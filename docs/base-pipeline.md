# 底座管线（HtmlBaseTransformer）

`HtmlTransformer.Core.Base` 是整个 HTML 转换器的通用底座，与具体输出目标无关。它的核心思想：以一套**简化的通用 HTML** 为内容源，通过不同 Transformer 转换成各渲染器可用的文本表达形式（HTML 本身可以直接渲染同一套内容），尽可能保持不同渲染器之间样式与行为的一致。

底座把转换过程拆成 5 个阶段，每个阶段由「配置」与「执行器」两部分组成，由 `HtmlBaseTransformer`（抽象类）统一编排。

## 管线概览

```
        ┌─────────────────────────────────────────────┐
HTML ──►│ Load ─► Sanitize ─► Normalize ─► Transform ─►│──► Finalize ─► 富文本
        └─────────────────────────────────────────────┘
```

对应 `HtmlBaseTransformer.Process(html)` 的执行流程：

| # | 阶段 | 执行器 | 配置类 | 职责 |
|---|------|--------|--------|------|
| 1 | Load | `ParserLoader` | `LoadConfig` / `OutputConfig` | 预处理输入、解析为 `HtmlDocument` |
| 2 | Sanitize | `ParserLoader` | `SanitizeConfig` | 按白名单净化标签与属性 |
| 3 | Normalize | `ParserTransformer` | `NormalizeConfig` | 标签归一化 |
| 4 | Transform | `ParserTransformer` | `TransformConfig` | 按序执行扩展（插件） |
| 5 | Finalize | `ParserFinalizer` | `FinalizeConfig` | 提取 body 内容、收尾后处理 |

## 各阶段说明

### 1. Load（预处理 + 解析）

- 先执行 `LoadConfig` 注册的**预处理器**（如去换行），结果存入 `dataConfig.DocHtml`；
- 以预处理结果解析 `HtmlDocument`；
- 随后执行 `SanitizeConfig` 的净化。

### 2. Sanitize（白名单净化）

规则（`SanitizeConfig`）：

- `AddTag("div", "class", "style")`：允许指定属性；未列出的属性被移除；
- `AddTag("div", ":all")`：不进行属性过滤；
- `AddTag("div")`：过滤掉所有属性；
- **白名单之外的标签被「解包」**：标签删除但子节点保留（文本不会丢失）；
- **文本节点永远保留**，不会因净化被清空。

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

插件只依赖 `HtmlAgilityPack.HtmlDocument`，与具体输出目标解耦。

## 工具类

`HtmlParseUtils`（`Base/Utils`）提供插件可复用的解析工具：

| 方法 | 说明 |
|------|------|
| `GetStyleAttrs(style)` | 解析 CSS `style` 值，返回键值对（如 `--color`） |
| `ColorToHex(color)` | 颜色转 `#RRGGBB[AA]`，支持 `#RGB/#RGBA/#RRGGBB/#RRGGBBAA/rgb()/rgba()` |
| `SizeToNumber(size)` | 尺寸字符串转纯数字（非法/带单位返回空串） |

## 编写一个新的转换器

继承 `HtmlBaseTransformer`，在 `Configure()` 中声明管线策略：

```csharp
public class H2XxxTransformer : HtmlBaseTransformer
{
    public override void Configure()
    {
        this.ConfigureLoad().RegisterPreprocessor(html => /* 预处理 */);
        this.ConfigureSanitize().AddTag(...);
        this.ConfigureNormalize().AddTagMapping(...);
        this.ConfigureTransform()
            .RegisterExtension("xxx", new XxxExtension())
            .SetOrders("xxx");
        this.ConfigureFinalize()
            .RegisterAfterFinalizeHook(html => /* 后处理 */);
    }
}
```

新转换器放在独立命名空间（如 `HtmlTransformer.Core.Xxx`），文档在 `docs/transformers/` 下新建对应文件。
