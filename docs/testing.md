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

## 编写一个转换器的测试

### 1. 基座

转换器目录下建 `Support/XxxTestBase.cs`，封装公开入口与断言：

```csharp
using HtmlTransformer.Core.Xxx;
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

覆盖管线级行为（预处理、实体、非法标签解包、归一化、换行输出等），命名固定为 `XxxBaseRuleTest`。
