using HtmlTransformer.Tests.Unity.Support;
using Xunit;

namespace HtmlTransformer.Tests.Unity
{
    /// <summary>
    /// H2Unity 转换器 RubyExtension 回归测试。
    /// <r>主体<rt>注释</rt></r>：多个 rt 合并上移到主体之后，空 rt 丢弃，ruby 归一为 r。
    /// </summary>
    public class H2UnityRubyExtensionTest : H2UnityTestBase
    {
        #region 格式转换

        public static readonly TheoryData<string, string, string> Format_Data =
            new TheoryData<string, string, string>
            {
                {"r 单 rt 拼接", "<r>主<rt>注</rt></r>", "<r>主<rt>注</rt></r>"},
                {"ruby 归一为 r", "<ruby>主<rt>注</rt></ruby>", "<r>主<rt>注</rt></r>"},
                {"多 rt 合并移到主体之后", "<r>a<rt>1</rt>b<rt>2</rt></r>", "<r>ab<rt>12</rt></r>"},
                {"rt 先于主体时归位", "<r><rt>注</rt>主</r>", "<r>主<rt>注</rt></r>"},
                {"仅含 rt 保留宿主", "<r><rt>注</rt></r>", "<r><rt>注</rt></r>"},
                {"无 rt 保留主体", "<r>主</r>", "<r>主</r>"},
            };

        [Theory]
        [MemberData(nameof(Format_Data))]
        public void Test_Format(string description, string input, string expected)
        {
            AssertTransform(description, input, expected);
        }

        #endregion

        #region 无效值解包

        public static readonly TheoryData<string, string, string> Invalid_Data =
            new TheoryData<string, string, string>
            {
                {"空 rt 解包", "<r>主<rt></rt></r>", "<r>主</r>"},
                {"rt 独立于 r 外保持原样", "<rt>注</rt><r>主</r>", "<rt>注</rt><r>主</r>"},
            };

        [Theory]
        [MemberData(nameof(Invalid_Data))]
        public void Test_Invalid(string description, string input, string expected)
        {
            AssertTransform(description, input, expected);
        }

        #endregion

        #region 属性清理

        public static readonly TheoryData<string, string, string> Attribute_Data =
            new TheoryData<string, string, string>
            {
                {"r/rt 非白名单属性全清除", "<r id=\"i\" class=\"c\"><rt id=\"j\">注</rt></r>", "<r><rt>注</rt></r>"},
            };

        [Theory]
        [MemberData(nameof(Attribute_Data))]
        public void Test_Attribute(string description, string input, string expected)
        {
            AssertTransform(description, input, expected);
        }

        #endregion

        #region 内容保留

        public static readonly TheoryData<string, string, string> Content_Data =
            new TheoryData<string, string, string>
            {
                {"文本与子标签混合保留", "<r>前<b>中</b>后<rt>注</rt></r>", "<r>前<b>中</b>后<rt>注</rt></r>"},
                {"rt 内白名单子标签归一保留", "<r><b>主</b><rt><em>注</em></rt></r>", "<r><b>主</b><rt><i>注</i></rt></r>"},
                {"非法子标签解包后重组", "<r><span><b>主</b></span><rt><span>注</span></rt></r>", "<r><b>主</b><rt>注</rt></r>"},
                {"空内容保留宿主", "<r></r>", "<r></r>"},
            };

        [Theory]
        [MemberData(nameof(Content_Data))]
        public void Test_Content(string description, string input, string expected)
        {
            AssertTransform(description, input, expected);
        }

        #endregion

        #region 容错与组合

        public static readonly TheoryData<string, string, string> Tolerance_Data =
            new TheoryData<string, string, string>
            {
                {"r 内含 br", "<r>a<br>b<rt>注</rt></r>", "<r>a\nb<rt>注</rt></r>"},
                {"r 包裹 color", "<r>主<color style=\"--color:#abc\">x</color><rt>注</rt></r>", "<r>主<color=#AABBCC>x</color><rt>注</rt></r>"},
                {"r 位于 p 内", "<p><r>主<rt>注</rt></r></p>", "<r>主<rt>注</rt></r>"},
                {"标签名大小写不敏感", "<R>主<RT>注</RT></R>", "<r>主<rt>注</rt></r>"},
            };

        [Theory]
        [MemberData(nameof(Tolerance_Data))]
        public void Test_Tolerance(string description, string input, string expected)
        {
            AssertTransform(description, input, expected);
        }

        #endregion
    }
}