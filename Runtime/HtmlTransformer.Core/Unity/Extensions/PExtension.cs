using HtmlAgilityPack;
using HtmlTransformer.Core.Base.Extensions;
using System.Linq;

namespace HtmlTransformer.Core.Unity.Extensions
{
    public class PExtension : IExtensionInterface
    {
        public void Transform(HtmlDocument doc)
        {
            var pNodes = doc.DocumentNode.SelectNodes("//p");
            if (pNodes != null)
            {
                var pNodesList = pNodes.ToList(); // Convert to list to avoid modification during iteration
                foreach (var node in pNodesList)
                {
                    // Create a text node with a newline character
                    var textNode = doc.CreateTextNode("\n");

                    // Insert the text node after the p element
                    var nextSibling = node.NextSibling;
                    var parent = node.ParentNode;

                    if (parent != null)
                    {
                        parent.InsertAfter(textNode, node);

                        // Unwrap the p element (remove the tag but keep its position)
                        parent.RemoveChild(node);
                    }
                }
            }
        }
    }
}
