using Xunit;
using System;
using System.Collections.Generic;
using HtmlTransformer.Core.Base.Utils;

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
    }
}
