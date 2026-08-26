using HtmlTransformer.Tests.Unity.Support;
using Xunit;

namespace HtmlTransformer.Tests.Unity
{
    /// <summary>
    /// H2Unity 转换器 ColorExtension 回归测试。
    /// <color style="--color: ..."> → <color=#RRGGBB[AA]>，非法值解包保留内容。
    /// </summary>
    public class H2UnityColorExtensionTest : H2UnityTestBase
    {
        #region 格式转换

        public static readonly TheoryData<string, string, string> Format_Data =
            new TheoryData<string, string, string>
            {
                {"#RGB 转 #RRGGBB", "<color style=\"--color: #abc\">x</color>", "<color=#AABBCC>x</color>"},
                {"#RGBA 转 #RRGGBBAA", "<color style=\"--color: #abcd\">x</color>", "<color=#AABBCCDD>x</color>"},
                {"#RRGGBB 大小写归一", "<color style=\"--color: #aBCd12\">x</color>", "<color=#ABCD12>x</color>"},
                {"#RRGGBBAA", "<color style=\"--color: #aBCd1234\">x</color>", "<color=#ABCD1234>x</color>"},
                {"rgb()", "<color style=\"--color: rgb(99, 88, 50)\">x</color>", "<color=#635832>x</color>"},
                {"rgba() 小数透明度", "<color style=\"--color: rgba(99, 88, 50, .52)\">x</color>", "<color=#63583284>x</color>"},
                {"rgba() 百分比透明度", "<color style=\"--color: rgba(99, 88, 50, 52%)\">x</color>", "<color=#63583284>x</color>"},
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
                {"无 style 属性", "<color>x</color>", "x"},
                {"style 缺 --color", "<color style=\"--foo: bar\">x</color>", "x"},
                {"--color 空值", "<color style=\"--color:\">x</color>", "x"},
                {"非法颜色名", "<color style=\"--color: notacolor\">x</color>", "x"},
                {"rgb() 颜色越界", "<color style=\"--color: rgb(99, 300, 50)\">x</color>", "x"},
                {"非法格式", "<color style=\"--color: #cde12\">x</color>", "x"},
                {"--color 大小写敏感", "<color style=\"--Color: #abc\">x</color>", "x"},
                {"非自定义属性 color", "<color style=\"color: #abc\">x</color>", "x"},
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
                {"style 与非白名单属性全清除", "<color style=\"--color: #abc\" id=\"i\" class=\"c\">x</color>", "<color=#AABBCC>x</color>"},
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
                {"文本内容保留", "<color style=\"--color: #abc\">text</color>", "<color=#AABBCC>text</color>"},
                {"保留白名单子标签", "<color style=\"--color: #abc\"><b>x</b></color>", "<color=#AABBCC><b>x</b></color>"},
                {"非法子标签解包", "<color style=\"--color: #abc\"><span><b>y</b></span></color>", "<color=#AABBCC><b>y</b></color>"},
                {"空内容保留宿主", "<color style=\"--color: #abc\"></color>", "<color=#AABBCC></color>"},
                {"嵌套 color 各自转换", "<color style=\"--color:#abc\"><color style=\"--color:#def\">x</color></color>", "<color=#AABBCC><color=#DDEEFF>x</color></color>"},
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
                {"style 多属性共存", "<color style=\"--color: #abc; --size: 20\">x</color>", "<color=#AABBCC>x</color>"},
                {"style 值空格容错", "<color style=\"  --color : #abc  \">x</color>", "<color=#AABBCC>x</color>"},
                {"标签名大小写不敏感", "<COLOR style=\"--color: #abc\">x</COLOR>", "<color=#AABBCC>x</color>"},
                {"color 位于 p 内", "<p><color style=\"--color:#abc\">x</color></p>", "<color=#AABBCC>x</color>"},
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
