using System.Linq;
using HtmlTransformer.Base.Config;

namespace HtmlTransformer.Base.Pipeline
{
    public class ParserFinalizer
    {
        public string Finalize(DataConfig dataConfig, FinalizeConfig finalizeConfig)
        {
            // Before Hook
            var doc = dataConfig.Doc;
            finalizeConfig.InvokeBeforeFinalizeHook(doc);

            string html = "";
            var bodyNode = doc.DocumentNode.Descendants("body").FirstOrDefault();
            if (bodyNode != null)
            {
                // Get inner HTML of body (excluding the body tags themselves)
                html = string.Concat(bodyNode.ChildNodes.Select(n => n.OuterHtml));
            }
            else
            {
                // No body element (e.g. fragment loaded without html/body wrapper)
                html = string.Concat(doc.DocumentNode.ChildNodes.Select(n => n.OuterHtml));
            }

            // After Hook
            html = finalizeConfig.InvokeAfterFinalizeHook(html);

            return html;
        }
    }
}
