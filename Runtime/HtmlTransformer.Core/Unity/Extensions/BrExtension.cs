using HtmlAgilityPack;
using HtmlTransformer.Core.Base.Extensions;
using System.Linq;

namespace HtmlTransformer.Core.Unity.Extensions
{
    public class BrExtension : IExtensionInterface
    {
        public void Transform(HtmlDocument doc)
        {
            var brNodes = doc.DocumentNode.SelectNodes("//br");
            if (brNodes != null)
            {
                var brNodesList = brNodes.ToList(); // Convert to list to avoid modification during iteration
                foreach (var node in brNodesList)
                {
                    // Create a text node with a newline character
                    var textNode = doc.CreateTextNode("\n");

                    // Insert the text node after the br element
                    var nextSibling = node.NextSibling;
                    var parent = node.ParentNode;

                    if (parent != null)
                    {
                        parent.InsertAfter(textNode, node);

                        // Unwrap the br element (remove the tag but keep its position)
                        parent.RemoveChild(node);
                    }
                }
            }
        }
    }
}
