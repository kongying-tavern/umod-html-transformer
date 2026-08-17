# H2UnityTransformer

HTML → Unity TextMeshPro 富文本。

命名空间：`HtmlTransformer.Core.Unity`

## 验证场景

```csharp
string html = "<p><color style=\"--color: #abc\">r &amp; g</color></p>";
string richText = H2UnityTransformer.Transform(html);
// "<color=#AABBCC>r & g</color>"
```

## 管线配置（本转换器）

| 阶段 | 配置 |
|------|------|
| Load | 预处理器移除所有 `\r` / `\n` |
| Sanitize | 白名单（每个标签都声明解析类型）：`p br b strong i em u size(style) color(style) a(href) link(href) ruby r rt`；其中 `br=ElementTypeVoid`，其余为 `ElementTypeNormal` |
| Normalize | `strong→b`、`em→i`、`a→link`、`ruby→r` |
| Transform | 依次执行：`r` `color` `size` `a` `p` `br` |
| Finalize | ① `collval="值"` → `=值`；② HTML 实体反转义（`&amp;`→`&` 等）；③ 移除末尾换行 |

> `a→link` 的映射在 Transform 之前完成，所以链接插件实际处理的是归一化后的 `link` 元素。

## 转换结果格式

| HTML 输入 | 输出 |
|-----------|------|
| `<b>x</b>` | `<b>x</b>` |
| `<strong>x</strong>` | `<b>x</b>` |
| `<i>x</i>` | `<i>x</i>` |
| `<em>x</em>` | `<i>x</i>` |
| `<u>x</u>` | `<u>x</u>` |
| `<color style="--color: #abc">x</color>` | `<color=#AABBCC>x</color>` |
| `<size style="--size: 20">x</size>` | `<size=20>x</size>` |
| `<a href="u">x</a>` | `<link=u>x</link>` |
| `<link href="u">x</link>` | `<link=u>x</link>` |
| `<r>主<rt>注</rt></r>` | `<r>主<rt>注</rt></r>` |
| `<ruby>主<rt>注</rt></ruby>` | `<r>主<rt>注</rt></r>` |
| `<p>x</p>` | `x\n`（结尾时去掉换行） |
| `<br>` | `\n` |

## 插件一览

| 插件 | 类型 | 行为 |
|------|------|------|
| `ColorExtension` | 行内样式 → `collval` | `--color` 转 `#RRGGBB[AA]` 写入 `collval` |
| `SizeExtension` | 行内样式 → `collval` | `--size` 转纯数字写入 `collval` |
| `AExtension` | 链接 → `collval` | `href` 写入 `collval` |
| `RubyExtension` | 结构重组 | 把若干 `rt` 定义拼接到 `r` 末尾 |
| `PExtension` | 块级换行 | `p` 解包并在末尾追加 `\n` |
| `BrExtension` | 换行 | `br` 替换为 `\n` 文本节点 |

其中 `color`、`size`、`a`（链接）是「转换型」，输出 TMP 富文本标签；`p`、`br`、`r` 是「结构型」，负责换行与段落布局。

## 插件详解

### ColorExtension —— 颜色

- **输入**：`<color style="--color: <颜色>">内容</color>`
- **输出**：`<color=#RRGGBB[AA]>内容</color>`
- **支持的颜色格式**：`#RGB`、`#RGBA`、`#RRGGBB`、`#RRGGBBAA`、`rgb(R, G, B)`、`rgba(R, G, B, A)`（小数或百分比透明度）
- **规则**：
  - 有效颜色 → 归一为 `#RRGGBB[AA]` 大写，写入 `collval`；
  - 颜色无效、缺失或 `--color` 键不存在 → **解包**（删除标签，保留内容）；
  - 触发转换时清除原标签全部属性（含 `style`），只保留 `collval`；
  - `--color` 键**大小写敏感**（`--Color` / `color` 均不被识别）；
  - 内容、嵌套标签保留；嵌套的 `color` 各自转换。

### SizeExtension —— 字号

- **输入**：`<size style="--size: <数字>">内容</size>`
- **输出**：`<size=N>内容</size>`
- **规则**：
  - 仅接受**纯整数**（含负数、0）；数字后带单位（如 `12px`）、小数、其它字符 → 解包；
  - 其他规则与 `ColorExtension` 相同（属性清除、解包保留内容、键大小写敏感）。

### AExtension —— 链接

- **输入**：`<a href="URL">内容</a>`（Normalize 后已变为 `link`）
- **输出**：`<link=URL>内容</link>`
- **规则**：
  - `href` 非空 → 写入 `collval`；`href` 缺失、为空或全空白 → 解包；
  - 清除除 `href` 外的全部属性（含 `onclick`/`target` 等危险或非白名单属性）；
  - 内容保留；
  - `link` 是普通容器，空内容输出完整闭合标签（`<link=u></link>`）；
  - `<a>` 不能嵌套 `<a>`（HAP 隐式闭合规则，同 HTML 规范），嵌套 `a` 会摊平为兄弟节点。

### RubyExtension —— 注音（ruby）

- **输入**：`<r>主体<rt>注音</rt></r>`（`<ruby>` 经 Normalize 变为 `<r>`）
- **输出**：`<r>主体<rt>注音</rt></r>`
- **规则**：
  - 收集中所有 `rt` 的文本并拼接，合并为一个 `<rt>` 追加到主体之后；
  - 多个 `rt` 合并：`<r>a<rt>1</rt>b<rt>2</rt></r>` → `<r>ab<rt>12</rt></r>`；
  - 无 `rt` → 只保留主体内容；
  - 重建时丢弃原标签属性。

### PExtension —— 段落

- **输入**：`<p>内容</p>`
- **输出**：内容 + 末尾换行（整体末尾的换行由 Finalize 移除）
- **规则**：`p` 解包，内容保留；多个 `p` 之间产生段落换行。

### BrExtension —— 换行

- **输入**：`<br>`
- **输出**：`\n`
- **规则**：替换为单个换行文本节点，无内容包袱；`br` 声明为 `ElementTypeVoid`（`<br>` 自闭合，不补 `</br>`）。

## 测试

回归测试的约定、嵌套代表类别与用例分组见 [H2UnityTransformer 测试说明](h2-unity-testing.md)。
