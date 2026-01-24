using System.Linq;
using HtmlTransformer.Core.Base.Config;

namespace HtmlTransformer.Core.Base.DI
{
    public class ParserFinalizer
    {
        public string Finalize(DataConfig dataConfig, FinalizeConfig finalizeConfig)
        {
            // Before Hook
            var doc = dataConfig.Doc;
            finalizeConfig.InvokeBeforeFinalizeHook(doc);

            string html = "";
            var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
            if (bodyNode != null)
            {
                // Get inner HTML of body (excluding the body tags themselves)
                html = string.Concat(bodyNode.ChildNodes.Select(n => n.OuterHtml));
            }

            // After Hook
            html = finalizeConfig.InvokeAfterFinalizeHook(html);

            return html;
        }
    }
}
