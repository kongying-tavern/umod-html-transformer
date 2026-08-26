using HtmlAgilityPack;
using HtmlTransformer.Base.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace HtmlTransformer.Unity.Extensions
{
    public class RubyExtension : IExtensionInterface
    {
        public void Transform(HtmlDocument doc)
        {
            var rNodes = doc.DocumentNode.SelectNodes("//r");
            if (rNodes != null)
            {
                var rNodesList = rNodes.ToList(); // Convert to list to avoid modification during iteration
                foreach (var node in rNodesList)
                {
                    // Clone the node to work with
                    var nodeClone = node.Clone();

                    // Get definition (rt elements)
                    var defList = nodeClone.SelectNodes(".//rt");
                    var defContentList = new List<string>();
                    if (defList != null)
                    {
                        foreach (var rt in defList)
                        {
                            defContentList.Add(rt.InnerHtml);
                        }
                    }
                    var defContent = defContentList.Count > 0 ? string.Join("", defContentList) : "";

                    // Remove rt elements from clone
                    if (defList != null)
                    {
                        var defListAsList = defList.ToList();
                        foreach (var rt in defListAsList)
                        {
                            rt.Remove();
                        }
                    }

                    // Get main content
                    var mainContent = nodeClone.InnerHtml;

                    // Rebuild ruby element
                    var newEl = doc.CreateElement("r");
                    if (string.IsNullOrWhiteSpace(defContent))
                    {
                        newEl.InnerHtml = mainContent;
                    }
                    else
                    {
                        var combinedContent = $"{mainContent}<rt>{defContent}</rt>";
                        newEl.InnerHtml = combinedContent;
                    }

                    // Replace the original node with the new one
                    node.ParentNode.ReplaceChild(newEl, node);
                }
            }
        }
    }
}
