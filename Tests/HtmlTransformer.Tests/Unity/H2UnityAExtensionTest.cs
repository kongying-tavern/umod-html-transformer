using HtmlTransformer.Tests.Unity.Support;
using Xunit;

namespace HtmlTransformer.Tests.Unity
{
    /// <summary>
    /// H2Unity 转换器 AExtension 回归测试。
    /// <a href="..."> → <link=URL>，href 缺失或为空解包保留内容。
    /// </summary>
    public class H2UnityAExtensionTest : H2UnityTestBase
    {
        #region 格式转换

        public static readonly TheoryData<string, string, string> Format_Data =
            new TheoryData<string, string, string>
            {
                {"a → link", "<a href=\"u\">x</a>", "<link=u>x</link>"},
                {"link 直接等价输入", "<link href=\"u\">x</link>", "<link=u>x</link>"},
                {"URL 保留", "<a href=\"https://yuanshen.site/guide\">指南</a>", "<link=https://yuanshen.site/guide>指南</link>"},
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
                {"无 href", "<a>x</a>", "x"},
                {"href 空值", "<a href=\"\">x</a>", "x"},
                {"href 全空白", "<a href=\"  \">x</a>", "x"},
                {"非 href 属性", "<a id=\"i\">x</a>", "x"},
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
                {"非白名单属性清除", "<a href=\"u\" id=\"i\" class=\"c\">x</a>", "<link=u>x</link>"},
                {"危险属性清除", "<a href=\"u\" target=\"_blank\" onclick=\"alert(1)\">x</a>", "<link=u>x</link>"},
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
                {"文本内容保留", "<a href=\"u\">text</a>", "<link=u>text</link>"},
                {"保留白名单子标签", "<a href=\"u\"><b>x</b></a>", "<link=u><b>x</b></link>"},
                {"非法子标签解包", "<a href=\"u\"><span><b>y</b></span></a>", "<link=u><b>y</b></link>"},
                {"空内容保留宿主", "<a href=\"u\"></a>", "<link=u></link>"},
                {"嵌套于容器内各自转换", "<b><a href=\"u\">x</a></b>", "<b><link=u>x</link></b>"},
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
                {"a 位于 p 内", "<p><a href=\"u\">x</a></p>", "<link=u>x</link>"},
                {"a 内含 br", "<a href=\"u\">a<br>b</a>", "<link=u>a\nb</link>"},
                {"a 包裹 color", "<a href=\"u\"><color style=\"--color:#abc\">x</color></a>", "<link=u><color=#AABBCC>x</color></link>"},
                {"非法组合反例：缺 href 的 a 于 p 内", "<p><a>x</a></p>", "x"},
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
