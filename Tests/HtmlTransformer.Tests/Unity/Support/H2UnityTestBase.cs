using HtmlTransformer.Core.Unity;
using Xunit;

namespace HtmlTransformer.Tests.Unity.Support
{
    /// <summary>
    /// H2UnityTransformer 全流程回归测试的统一基座。
    /// 所有插件测试都通过公开入口 Transform() 走完整管线，保证覆盖真实组合行为。
    /// </summary>
    public abstract class H2UnityTestBase
    {
        private const string BlueColor = "\u001b[34m";
        private const string ResetColor = "\u001b[0m";

        protected static string Transform(string html)
        {
            return H2UnityTransformer.Transform(html);
        }

        protected static void AssertTransform(string description, string input, string expected)
        {
            System.Console.WriteLine($"{BlueColor}{nameof(H2UnityTransformer)}.{nameof(H2UnityTransformer.Transform)}: {description}{ResetColor}");
            Assert.Equal(expected, Transform(input));
        }
    }
}
