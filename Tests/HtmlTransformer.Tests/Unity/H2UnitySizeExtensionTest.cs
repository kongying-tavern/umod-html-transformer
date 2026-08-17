using HtmlTransformer.Tests.Unity.Support;
using Xunit;

namespace HtmlTransformer.Tests.Unity
{
    /// <summary>
    /// H2Unity 转换器 SizeExtension 回归测试。
    /// <size style="--size: ..."> → <size=N>，非法值解包保留内容。
    /// </summary>
    public class H2UnitySizeExtensionTest : H2UnityTestBase
    {
        #region 格式转换

        public static readonly TheoryData<string, string, string> Format_Data =
            new TheoryData<string, string, string>
            {
                {"正整数", "<size style=\"--size: 20\">x</size>", "<size=20>x</size>"},
                {"0", "<size style=\"--size: 0\">x</size>", "<size=0>x</size>"},
                {"负数", "<size style=\"--size: -5\">x</size>", "<size=-5>x</size>"},
                {"正号", "<size style=\"--size: +10\">x</size>", "<size=10>x</size>"},
                {"大整数", "<size style=\"--size: 123456\">x</size>", "<size=123456>x</size>"},
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
                {"无 style 属性", "<size>x</size>", "x"},
                {"style 缺 --size", "<size style=\"--foo: bar\">x</size>", "x"},
                {"--size 空值", "<size style=\"--size:\">x</size>", "x"},
                {"带单位", "<size style=\"--size: 12px\">x</size>", "x"},
                {"小数", "<size style=\"--size: 12.5\">x</size>", "x"},
                {"溢出 int", "<size style=\"--size: 99999999999\">x</size>", "x"},
                {"--size 大小写敏感", "<size style=\"--Size: 20\">x</size>", "x"},
                {"非自定义属性 size", "<size style=\"size: 20\">x</size>", "x"},
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
                {"style 与非白名单属性全清除", "<size style=\"--size: 20\" id=\"i\" class=\"c\">x</size>", "<size=20>x</size>"},
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
                {"文本内容保留", "<size style=\"--size: 20\">text</size>", "<size=20>text</size>"},
                {"保留白名单子标签", "<size style=\"--size: 20\"><b>x</b></size>", "<size=20><b>x</b></size>"},
                {"非法子标签解包", "<size style=\"--size: 20\"><span><b>y</b></span></size>", "<size=20><b>y</b></size>"},
                {"空内容保留宿主", "<size style=\"--size: 20\"></size>", "<size=20></size>"},
                {"嵌套 size 各自转换", "<size style=\"--size:20\"><size style=\"--size:30\">x</size></size>", "<size=20><size=30>x</size></size>"},
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
                {"style 多属性共存", "<size style=\"--size: 20; --color: #abc\">x</size>", "<size=20>x</size>"},
                {"style 值空格容错", "<size style=\"  --size : 20  \">x</size>", "<size=20>x</size>"},
                {"标签名大小写不敏感", "<SIZE style=\"--size: 20\">x</SIZE>", "<size=20>x</size>"},
                {"size 位于 p 内", "<p><size style=\"--size:20\">x</size></p>", "<size=20>x</size>"},
                {"size 内含 br", "<size style=\"--size:20\">a<br>b</size>", "<size=20>a\nb</size>"},
                {"非法组合反例：缺 --size 的 size 于 p 内", "<p><size>x</size></p>", "x"},
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
