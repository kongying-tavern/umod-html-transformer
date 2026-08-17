using HtmlTransformer.Tests.Unity.Support;
using Xunit;

namespace HtmlTransformer.Tests.Unity
{
    /// <summary>
    /// 底座规则：所有标签/转换器共通的管线行为。
    /// 不是针对某个插件的转换逻辑，而是 Load/Sanitize/Normalize/Finalize 的通用规则。
    /// </summary>
    public class H2UnityBaseRuleTest : H2UnityTestBase
    {
        #region 预处理：移除换行

        public static readonly TheoryData<string, string, string> PreprocessNewline_Data =
            new TheoryData<string, string, string>
            {
                {"移除 CRLF", "a\r\nb", "ab"},
                {"移除 LF", "a\nb", "ab"},
                {"移除 CR", "a\rb", "ab"},
                {"移除 p 内部 CRLF", "<p>a\r\nb</p>", "ab"},
            };

        [Theory]
        [MemberData(nameof(PreprocessNewline_Data))]
        public void Test_PreprocessNewline(string description, string input, string expected)
        {
            AssertTransform(description, input, expected);
        }

        #endregion

        #region 文本与实体

        public static readonly TheoryData<string, string, string> TextEntity_Data =
            new TheoryData<string, string, string>
            {
                {"纯文本保留", "text", "text"},
                {"反转义 &amp;", "a &amp; b", "a & b"},
                {"反转义 &lt;", "&lt;", "<"},
                {"反转义 &gt;", "&gt;", ">"},
                {"反转义 &quot;", "&quot;q&quot;", "\"q\""},
                {"反转义 &copy;", "&copy;", "\u00a9"},
                {"反转义 &nbsp;", "&nbsp;", "\u00a0"},
                {"反转义十进制实体", "&#233;", "é"},
                {"反转义十六进制实体", "&#x4e2d;", "中"},
                {"未知实体原样保留", "&unknown;", "&unknown;"},
                {"双层实体只解一层", "&amp;amp;", "&amp;"},
                {"组合实体", "&lt;&amp;&gt;", "<&>"},
            };

        [Theory]
        [MemberData(nameof(TextEntity_Data))]
        public void Test_TextEntity(string description, string input, string expected)
        {
            AssertTransform(description, input, expected);
        }

        #endregion

        #region 非法标签解包（子内容保留）

        public static readonly TheoryData<string, string, string> DisallowedUnwrap_Data =
            new TheoryData<string, string, string>
            {
                {"div 解包保留文本", "<div>text</div>", "text"},
                {"div 解包保留合法子标签", "<div><b>x</b></div>", "<b>x</b>"},
                {"div 解包保留实体子内容", "<div>a&lt;b</div>", "a<b"},
                {"多层非法解包", "<span><div>x</div></span>", "x"},
                {"表格整体解包", "<table><tr><td>cell</td></tr></table>", "cell"},
                {"非法标签夹在文本中", "a<span>&amp;</span>b", "a&b"},
                {"白名单标签作为文本子节点保留", "a<b>x</b>c", "a<b>x</b>c"},
            };

        [Theory]
        [MemberData(nameof(DisallowedUnwrap_Data))]
        public void Test_DisallowedUnwrap(string description, string input, string expected)
        {
            AssertTransform(description, input, expected);
        }

        #endregion

        #region Normalize 标签归一化

        public static readonly TheoryData<string, string, string> NormalizeMapping_Data =
            new TheoryData<string, string, string>
            {
                {"strong→b", "<strong>x</strong>", "<b>x</b>"},
                {"em→i", "<em>x</em>", "<i>x</i>"},
                {"a→link 带 href", "<a href=\"u\">x</a>", "<link=u>x</link>"},
                {"a→link 无 href 解包", "<a>x</a>", "x"},
                {"ruby→r", "<ruby>x<rt>d</rt></ruby>", "<r>x<rt>d</rt></r>"},
            };

        [Theory]
        [MemberData(nameof(NormalizeMapping_Data))]
        public void Test_NormalizeMapping(string description, string input, string expected)
        {
            AssertTransform(description, input, expected);
        }

        #endregion

        #region 换行输出

        public static readonly TheoryData<string, string, string> NewlineOutput_Data =
            new TheoryData<string, string, string>
            {
                {"p 结尾换行去除", "<p>x</p>", "x"},
                {"br 结尾换行去除", "x<br>", "x"},
                {"首个 br 换行保留", "<br>x", "\nx"},
                {"多个 p 段落", "<p>a</p><p>b</p>", "a\nb"},
            };

        [Theory]
        [MemberData(nameof(NewlineOutput_Data))]
        public void Test_NewlineOutput(string description, string input, string expected)
        {
            AssertTransform(description, input, expected);
        }

        #endregion
    }
}
