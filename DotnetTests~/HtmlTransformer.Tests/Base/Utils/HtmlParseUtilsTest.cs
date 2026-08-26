using Xunit;
using System;
using System.Collections.Generic;
using HtmlTransformer.Base.Utils;

namespace HtmlTransformer.Tests.Base.Utils
{
    public class HtmlParseUtilsTest
    {
        #region GetStyleAttrs
        public static readonly TheoryData<string, string, Dictionary<string, string>> GetStyleAttrs_Data =
            new TheoryData<string, string, Dictionary<string, string>>
            {
                {
                    "Parse",
                    "a: color ;\nb: size;c: var(--variable) ;  \n --d-e: calc(var(--height));",
                    new Dictionary<string, string>
                    {
                        {"a", "color"},
                        {"b", "size"},
                        {"c", "var(--variable)"},
                        {"--d-e", "calc(var(--height))"}
                    }
                },
            };

        [Theory]
        [MemberData(nameof(GetStyleAttrs_Data))]
        public void Test_GetStyleAttrs(
            string description,
            string input,
            Dictionary<string, string> expected
        )
        {
            const string blueColor = "\u001b[34m";
            const string resetColor = "\u001b[0m";
            Console.WriteLine($"{blueColor}{nameof(HtmlParseUtils)}.{nameof(HtmlParseUtils.GetStyleAttrs)}: {description}{resetColor}");

            var result = HtmlParseUtils.GetStyleAttrs(input);
            Assert.Equal(expected, result);
        }
        #endregion

        #region ColorToHex
        public static readonly TheoryData<string, string, string> ColorToHex_Data =
            new TheoryData<string, string, string>
            {
                {"Empty Input", "", ""},
                {"Blank Input", "  \n  ", ""},
                {"Invalid Input", "invalid abc def", ""},
                {"Invalid with Mixed Valid Values", "#cde #123456", ""},
                {"Invalid Hash", "#cde12", ""},
                {"Out-of-range Hash", "#ga1234", ""},
                {"Color with leading spaces", " #abc", "#AABBCC"},
                {"Color with trailing spaces", "#abc ", "#AABBCC"},
                {"Color with RGB Hash", "#aBc", "#AABBCC"},
                {"Color with RGBA Hash", "#aBcd", "#AABBCCDD"},
                {"Color with RRGGBB Hash", "#aBCd12", "#ABCD12"},
                {"Color with RRGGBBAA Hash", "#aBCd1234", "#ABCD1234"},
                {"Color with out-of-range rgb()", "rgb( 99, 300,  50)", ""},
                {"Color with valid rgb()", "rgb( 99, 88,  50)", "#635832"},
                {"Color numeric-alpha-ed rgba() with out-of-range color", "rgb( 99, 300,  50, 0.4)", ""},
                {"Color numeric-alpha-ed rgba() with out-of-range alpha", "rgb( 99, 88,  50, 1.4)", ""},
                {"Color numeric-alpha-ed rgba()", "rgba( 99, 88,  50, .52)", "#63583284"},
                {"Color percent-alpha-ed rgba() with out-of-range color", "rgba( 99, 300,  50, 40%)", ""},
                {"Color percent-alpha-ed rgba() with out-of-range alpha", "rgba( 99, 88,  50, 140%)", ""},
                {"Color percent-alpha-ed rgba()", "rgba( 99, 88,  50, 52%)", "#63583284"},
            };

        [Theory]
        [MemberData(nameof(ColorToHex_Data))]
        public void Test_ColorToHex(string description, string input, string expected)
        {
            const string blueColor = "\u001b[34m";
            const string resetColor = "\u001b[0m";
            Console.WriteLine($"{blueColor}{nameof(HtmlParseUtils)}.{nameof(HtmlParseUtils.ColorToHex)}: {description}{resetColor}");

            var result = HtmlParseUtils.ColorToHex(input);
            Assert.Equal(expected, result);
        }
        #endregion

        #region SizeToNumber
        public static readonly TheoryData<string, string, string> SizeToNumber_Data =
            new TheoryData<string, string, string>
            {
                {"Empty Input", "", ""},
                {"Blank Input", "  \n  ", ""},
                {"Invalid Number", "invalid abc def", ""},
                {"Invalid with Unit", "12px", ""},
                {"Invalid with Mixed Values", "12px 20px", ""},
                {"Float Value", "12.2", ""},
                {"Negative Value", "-2", "-2"},
                {"Zero Value", "0", "0"},
                {"Valid Value", "20", "20"},
            };

        [Theory]
        [MemberData(nameof(SizeToNumber_Data))]
        public void Test_SizeToNumber(string description, string input, string expected)
        {
            const string blueColor = "\u001b[34m";
            const string resetColor = "\u001b[0m";
            Console.WriteLine($"{blueColor}{nameof(HtmlParseUtils)}.{nameof(HtmlParseUtils.SizeToNumber)}: {description}{resetColor}");

            var result = HtmlParseUtils.SizeToNumber(input);
            Assert.Equal(expected, result);
        }
        #endregion
    }
}
