using HtmlTransformer.Tests.Unity.Support;
using Xunit;

namespace HtmlTransformer.Tests.Unity
{
    /// <summary>
    /// H2Unity 转换器 PExtension 回归测试。
    /// <p> 解包保留内容，末尾追加换行（整体末尾的换行由 Finalize 移除），多个 p 之间分段。
    /// </summary>
    public class H2UnityPExtensionTest : H2UnityTestBase
    {
        #region 格式转换

        public static readonly TheoryData<string, string, string> Format_Data =
            new TheoryData<string, string, string>
            {
                {"p 解包", "<p>x</p>", "x"},
                {"多个 p 行间换行", "<p>a</p><p>b</p>", "a\nb"},
                {"p 内保留白名单子标签", "<p><i>a</i><b>b</b></p>", "<i>a</i><b>b</b>"},
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
                {"空 p 输出为空", "<p></p>", ""},
                {"未闭合 p 容错", "<p>a", "a"},
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
                {"p 非白名单属性全清除", "<p id=\"i\" class=\"c\">x</p>", "x"},
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
                {"文本与子标签混合保留", "<p>前<b>中</b>后</p>", "前<b>中</b>后"},
                {"非法子标签解包", "<p><span><b>y</b></span></p>", "<b>y</b>"},
                {"文本与空子标签顺序保持", "<p>a<i></i>b</p>", "a<i></i>b"},
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
                {"连续 p 分段", "<p>a</p><p>b</p><p>c</p>", "a\nb\nc"},
                {"p 内含 br", "<p>a<br>b</p>", "a\nb"},
                {"p 位于 span 解包内", "<span><p>x</p></span>", "x"},
                {"标签名大小写不敏感", "<P>x</P>", "x"},
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