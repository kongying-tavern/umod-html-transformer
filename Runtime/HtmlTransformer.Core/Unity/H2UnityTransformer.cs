using HtmlAgilityPack;
using HtmlTransformer.Core.Base;
using HtmlTransformer.Core.Base.Config;
using HtmlTransformer.Core.Unity.Extensions;
using System.Text.RegularExpressions;

namespace HtmlTransformer.Core.Unity
{
    public class H2UnityTransformer : HtmlBaseTransformer
    {
        public static string Transform(string html) => new H2UnityTransformer().Process(html);

        public override void Configure()
        {
            this.ConfigureLoad()
                .RegisterPreprocessor((string html) =>
                {
                    return Regex.Replace(html, @"[\r\n]", "");
                });
            this.ConfigureSanitize()
                .AddTag("p", SanitizeConfig.ElementTypeNormal)
                .AddTag("br", SanitizeConfig.ElementTypeVoid)
                .AddTag("b", SanitizeConfig.ElementTypeNormal)
                .AddTag("strong", SanitizeConfig.ElementTypeNormal)
                .AddTag("i", SanitizeConfig.ElementTypeNormal)
                .AddTag("em", SanitizeConfig.ElementTypeNormal)
                .AddTag("u", SanitizeConfig.ElementTypeNormal)
                .AddTag("size", SanitizeConfig.ElementTypeNormal, "style")
                .AddTag("color", SanitizeConfig.ElementTypeNormal, "style")
                .AddTag("a", SanitizeConfig.ElementTypeNormal, "href")
                .AddTag("link", SanitizeConfig.ElementTypeNormal, "href")
                .AddTag("ruby", SanitizeConfig.ElementTypeNormal)
                .AddTag("r", SanitizeConfig.ElementTypeNormal)
                .AddTag("rt", SanitizeConfig.ElementTypeNormal);
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

                    // Transform HTML entities
                    html = HtmlEntity.DeEntitize(html);

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
