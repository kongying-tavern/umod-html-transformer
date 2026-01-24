using HtmlAgilityPack;
using HtmlTransformer.Core.Base.Extensions;
using HtmlTransformer.Core.Base.Utils;
using System.Collections.Generic;
using System.Linq;

namespace HtmlTransformer.Core.Unity.Extensions
{
    public class SizeExtension : IExtensionInterface
    {
        public void Transform(HtmlDocument doc)
        {
            var sizeNodes = doc.DocumentNode.SelectNodes("//size");
            if (sizeNodes != null)
            {
                var sizeNodesList = sizeNodes.ToList(); // Convert to list to avoid modification during iteration
                foreach (var node in sizeNodesList)
                {
                    var styleAttr = node.Attributes["style"];
                    var styleAttrs = styleAttr != null ? HtmlParseUtils.GetStyleAttrs(styleAttr.Value) : new Dictionary<string, string>();
                    var size = styleAttrs.ContainsKey("--size") ? styleAttrs["--size"] : "";

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

                    string sizeValue = "";
                    if (!string.IsNullOrWhiteSpace(size))
                    {
                        sizeValue = HtmlParseUtils.SizeToNumber(size);
                    }

                    if (string.IsNullOrWhiteSpace(sizeValue))
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
                        node.SetAttributeValue("collval", sizeValue);
                    }
                }
            }
        }
    }
}
