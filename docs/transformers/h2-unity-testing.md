# H2UnityTransformer 测试说明

[测试规范](../testing.md) 在 H2Unity 转换器上的实例化。输出目标是 Unity TMP 富文本字符串，「输入 / 期望输出」均为字符串，用例用三元组。基座与类名 / 文件名命名沿用通用规范（`H2UnityTestBase`、`H2UnityBaseRuleTest`、`H2Unity*ExtensionTest`）。

## 嵌套（组合）测试的代表性类别

多插件组合只取三类代表类别求交点，不全量笛卡尔积：

| 类别 | 标签 | 含义 |
|------|------|------|
| 纯换行 | `br` | 最影响行距的结构型标签 |
| 块状 | `p` | 解包并追加换行的块级 / 段落 |
| 行内 | `color` | 转换型（带属性改写）的代表 |

组合用例 = 代表类别之间的交点（如 `<p>…<color>…</p>`）+ 有代表性的非法组合反例（如转换型标签缺属性出现在结构型内部）。

## 插件用例分组

每个插件测试按五组 `#region` 组织：`格式转换` / `无效值解包` / `属性清理` / `内容保留` / `容错与组合`。

| 分组 | 覆盖点 |
|------|--------|
| 格式转换 | 合法输入 → 期望输出格式 |
| 无效值解包 | 缺属性 / 非法值 → 解包并保留内容 |
| 属性清理 | 非白名单属性被移除，仅留转换所用属性 |
| 内容保留 | 文本、白名单子标签、空内容、同级嵌套各自转换 |
| 容错与组合 | 多属性共存、空格容错、大小写、与代表类别嵌套 |

## 空白字符语义

空白的折叠是**渲染层**行为（CSS `white-space` / Unity TMP 富文本渲染），管线不参与；Base 通则见[测试规范](../testing.md)。下面是 H2Unity 由其 `Configure()` 声明决定的特有边界，由 `H2UnityBaseRuleTest` 的「空白字符处理」region 锁定：

- `&nbsp;`（可多个）经 Finalize 反转义为 U+00A0——渲染层不折叠它，作为「间隔一个空格」的显式空白；
- Load 预处理器移除源码全部 `\r\n`，故文本内无源码换行；**结构换行仅由 br / p 产出**；
- 由此推论：普通空白绝不产生换行（多空格 ≠ 换行）。

## 当前覆盖

- `H2UnityBaseRuleTest`：管线级底座规则（预处理去换行、实体、非法标签解包、归一化、空白保真、换行输出）
- `H2UnityColorExtensionTest`：Color 插件 25 例
- `H2UnitySizeExtensionTest`：Size 插件 25 例
- `H2UnityAExtensionTest`：链接插件 19 例
- `H2UnityRubyExtensionTest`：注音插件 17 例
- `H2UnityPExtensionTest`：段落插件 13 例
- `H2UnityBrExtensionTest`：换行插件 13 例
