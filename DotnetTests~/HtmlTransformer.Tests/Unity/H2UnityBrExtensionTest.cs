using HtmlTransformer.Tests.Unity.Support;
using Xunit;

namespace HtmlTransformer.Tests.Unity
{
    /// <summary>
    /// H2Unity 转换器 BrExtension 回归测试。
    /// <br> 原位替换为换行文本节点（整体末尾的换行由 Finalize 移除）。
    /// </summary>
    public class H2UnityBrExtensionTest : H2UnityTestBase
    {
        #region 格式转换

        public static readonly TheoryData<string, string, string> Format_Data =
            new TheoryData<string, string, string>
            {
                {"br 转换文本换行", "a<br>b", "a\nb"},
                {"连续两个 br", "a<br><br>b", "a\n\nb"},
                {"前缀 br", "<br>x", "\nx"},
                {"尾随 br 换行被移除", "text<br>", "text"},
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
                {"仅 br 输出为空", "<br>", ""},
                {"p 内唯一 br 保留一行换行", "<p><br></p>", "\n"},
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
                {"br 属性被剥离", "x<br id=\"i\" class=\"c\">y", "x\ny"},
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
                {"br 周围顺序保持", "a<b>x</b><br>c", "a<b>x</b>\nc"},
                {"br 位于容器内", "<b>a<br>b</b>", "<b>a\nb</b>"},
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
                {"标签名大小写不敏感", "a<BR>b", "a\nb"},
                {"p 与 br 相邻分段", "x<br><p>y</p>", "x\ny"},
                {"br 位于 color 内", "<color style=\"--color:#abc\">a<br>b</color>", "<color=#AABBCC>a\nb</color>"},
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
