using HtmlAgilityPack;
using HtmlTransformer.Base.Extensions;
using System.Linq;

namespace HtmlTransformer.Unity.Extensions
{
    public class PExtension : IExtensionInterface
    {
        public void Transform(HtmlDocument doc)
        {
            var pNodes = doc.DocumentNode.Descendants("p");
            if (pNodes.Any())
            {
                var pNodesList = pNodes.ToList(); // Convert to list to avoid modification during iteration
                foreach (var node in pNodesList)
                {
                    var parent = node.ParentNode;
                    if (parent == null)
                    {
                        continue;
                    }

                    // Create a text node with a newline character
                    var textNode = doc.CreateTextNode("\n");

                    // Append the newline at the end of the p element
                    node.AppendChild(textNode);

                    // Unwrap the p element (keep its content and position)
                    parent.RemoveChild(node, true);
                }
            }
        }
    }
}
