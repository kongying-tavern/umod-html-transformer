# 测试规范

各转换器回归测试遵循统一约定，保证走真实管线、覆盖正反例、用例规模可控。

## 通用约定

适用于任何基于 `HtmlBaseTransformer` 的转换器：

- **全流程回归**：所有用例通过转换器的公开入口（如 `H2UnityTransformer.Transform`）走完整管线，不针对内部阶段做单测；这样插件组合与阶段间交互的真实行为都被覆盖。
- **统一基座**：每个转换器的测试类继承一个封装其公开入口的抽象基座（如 `H2UnityTestBase`），放在该转换器的 `Support/` 目录。
- **用例形态**：`TheoryData<string, string, string>` 的「描述 / 输入 / 期望输出」三元组 + `MemberData` 驱动，描述字段一眼看出测什么。
- **正反例并存**：合法输入（正例）与缺属性 / 非法值 / 越界等（反例）都要有，反例验证「解包 / 容错」路径而不是崩溃。
- **组合测试降维**：多插件嵌套的组合空间很大，不全量笛卡尔积，只取**代表性类别**的交点 + 有代表性的非法组合反例。代表性类别的具体定义归属各转换器，见其测试说明。
- **文件规范**：LF 行尾、末尾空行。

## 测试为什么串行运行

测试工程的 `xunit.runner.json` 关闭了测试类之间的并行。这不是随意配置，和一个共享状态有关：

- HtmlAgilityPack 用静态字典 `HtmlNode.ElementsFlags` 决定每个标签怎么解析；
- `SanitizeConfig.AddTag()` 会写入这个字典，而每个转换器的 `Configure()` 都会调用它；
- 多个转换器的测试类并行运行时，就是并发写同一个非线程安全的字典。

结果是偶发失败：用例单独跑能通过，全量一起跑可能报错（曾在全量运行中随机失败 3 例，逐类隔离后全部通过）。

如果将来要恢复并行，需要先让各转换器不再写同一份全局状态，只把配置改回去没有用。

## 编写一个转换器的测试

### 1. 基座

转换器目录下建 `Support/XxxTestBase.cs`，封装公开入口与断言：

```csharp
using HtmlTransformer.Xxx;
using Xunit;

namespace HtmlTransformer.Tests.Xxx.Support
{
    /// <summary>
    /// XxxTransformer 全流程回归测试的统一基座。
    /// </summary>
    public abstract class XxxTestBase
    {
        private const string BlueColor = "\u001b[34m";
        private const string ResetColor = "\u001b[0m";

        protected static string Transform(string html)
        {
            return XxxTransformer.Transform(html);
        }

        protected static void AssertTransform(string description, string input, string expected)
        {
            System.Console.WriteLine($"{BlueColor}{nameof(XxxTransformer)}.{nameof(XxxTransformer.Transform)}: {description}{ResetColor}");
            Assert.Equal(expected, Transform(input));
        }
    }
}
```

### 2. 插件测试

每个插件一个测试文件，按该转换器的分组约定组织 `#region`：

```csharp
using HtmlTransformer.Tests.Xxx.Support;
using Xunit;

namespace HtmlTransformer.Tests.Xxx
{
    /// <summary>
    /// Xxx 转换器 YyyExtension 回归测试。
    /// </summary>
    public class XxxYyyExtensionTest : XxxTestBase
    {
        #region 格式转换

        public static readonly TheoryData<string, string, string> Format_Data =
            new TheoryData<string, string, string>
            {
                {"测什么", "输入 HTML", "<期望输出>"},
            };

        [Theory]
        [MemberData(nameof(Format_Data))]
        public void Test_Format(string description, string input, string expected)
        {
            AssertTransform(description, input, expected);
        }

        #endregion

        // 其余分组：无效值解包 / 属性清理 / 内容保留 / 容错与组合
    }
}
```

### 3. 底座规则测试

覆盖管线级行为（预处理、实体、非法标签解包、归一化、空白保真、换行输出等），命名固定为 `XxxBaseRuleTest`。

**空白字符语义**：空白的折叠是**渲染层**行为（CSS `white-space` / 富文本渲染器），管线不参与。Base 底座只有一条通则——解析层（HAP）与 Base 管线均**不改动文本空白**：

- 空格 / tab / 首尾 / 混合空白一律**原样保留**（不折叠、不损失）。

底座规则测试至少锁定这一通则。各转换器的额外空白行为（如 `&nbsp;` 反转义、去换行预处理、br/p 结构换行）由它自己声明的 preprocessor / 扩展 / Finalize 钩子决定，属实例化内容，写在该转换器的测试说明里，不上升为通用规范。
