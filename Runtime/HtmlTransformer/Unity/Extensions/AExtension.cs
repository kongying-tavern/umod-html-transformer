using HtmlAgilityPack;
using HtmlTransformer.Base.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace HtmlTransformer.Unity.Extensions
{
    public class AExtension : IExtensionInterface
    {
        public void Transform(HtmlDocument doc)
        {
            var nodes = doc.DocumentNode.SelectNodes("//link");
            if (nodes != null)
            {
                var nodesList = nodes.ToList(); // Convert to list to avoid modification during iteration
                foreach (var node in nodesList)
                {
                    var hrefAttr = node.Attributes["href"];
                    string href = hrefAttr != null ? hrefAttr.Value : "";

                    // Clear all attributes
                    var attrsToRemove = new List<HtmlAttribute>();
                    foreach (var attr in node.Attributes)
                    {
                        attrsToRemove.Add(attr);
                    }

                    foreach (var attr in attrsToRemove)
                    {
                        node.Attributes.Remove(attr);
                    }

                    if (string.IsNullOrWhiteSpace(href))
                    {
                        // unwrap the element (remove the tag but keep its content)
                        var nextSibling = node.NextSibling;
                        var parent = node.ParentNode;

                        if (parent != null)
                        {
                            // Move children to parent
                            if (node.HasChildNodes)
                            {
                                var children = new List<HtmlNode>();
                                foreach (var child in node.ChildNodes)
                                {
                                    children.Add(child);
                                }

                                foreach (var child in children)
                                {
                                    parent.InsertBefore(child, nextSibling);
                                }
                            }

                            node.Remove();
                        }
                    }
                    else
                    {
                        node.SetAttributeValue("collval", href);
                    }
                }
            }
        }
    }
}
