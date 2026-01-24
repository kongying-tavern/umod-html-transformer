using HtmlTransformer.Core.Base;
using HtmlTransformer.Core.Unity.Extensions;
using System.Text.RegularExpressions;

namespace HtmlTransformer.Core.Unity
{
    public class H2UnityTransformer : HtmlBaseTransformer
    {
        public static string Transform(string html)
        {
            var runner = new H2UnityTransformer();
            return runner.Process(html);
        }

        public override void Configure()
        {
            this.ConfigureLoad()
                .RegisterPreprocessor((string html) =>
                {
                    return Regex.Replace(html, @"[\r\n]", "");
                });
            this.ConfigureSanitize()
                .AddTag("p")
                .AddTag("br")
                .AddTag("b")
                .AddTag("strong")
                .AddTag("i")
                .AddTag("em")
                .AddTag("u")
                .AddTag("size", "style")
                .AddTag("color", "style")
                .AddTag("a", "href")
                .AddTag("link", "href")
                .AddTag("ruby")
                .AddTag("r")
                .AddTag("rt");
            this.ConfigureNormalize()
                .AddTagMapping("b", "strong")
                .AddTagMapping("i", "em")
                .AddTagMapping("link", "a")
                .AddTagMapping("r", "ruby");
            this.ConfigureTransform()
                .RegisterExtension("color", new ColorExtension())
                .RegisterExtension("size", new SizeExtension())
                .RegisterExtension("r", new RubyExtension())
                .RegisterExtension("a", new AExtension())
                .RegisterExtension("p", new PExtension())
                .RegisterExtension("br", new BrExtension())
                .SetOrders("r", "color", "size", "a", "p", "br");
            this.ConfigureFinalize()
                .RegisterAfterFinalizeHook((html) =>
                {
                    // Transform `color` and `size` values
                    // Updated regex pattern to match collval attributes
                    var valuePattern = @"\s+collval\s*=\s*""\s*([^\s""]+)\s*""";
                    html = Regex.Replace(html, valuePattern, "=$1");

                    // Remove trailing new line due to `p` and `br`
                    if (html.EndsWith("\n"))
                    {
                        html = html.Substring(0, html.Length - 1);
                    }

                    return html;
                });
        }
    }
}
