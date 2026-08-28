using HtmlAgilityPack;
using HtmlTransformer.Base.Extensions;
using HtmlTransformer.Base.Utils;
using System.Collections.Generic;
using System.Linq;

namespace HtmlTransformer.Unity.Extensions
{
    public class ColorExtension : IExtensionInterface
    {
        public void Transform(HtmlDocument doc)
        {
            var colorNodes = doc.DocumentNode.Descendants("color");
            if (colorNodes.Any())
            {
                var colorNodesList = colorNodes.ToList(); // Convert to list to avoid modification during iteration
                foreach (var node in colorNodesList)
                {
                    var styleAttr = node.Attributes["style"];
                    var styleAttrs = styleAttr != null ? HtmlParseUtils.GetStyleAttrs(styleAttr.Value) : new Dictionary<string, string>();
                    var color = styleAttrs.ContainsKey("--color") ? styleAttrs["--color"] : "";

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

                    string colorValue = "";
                    if (!string.IsNullOrWhiteSpace(color))
                    {
                        colorValue = HtmlParseUtils.ColorToHex(color);
                    }

                    if (string.IsNullOrWhiteSpace(colorValue))
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
                        node.SetAttributeValue("collval", colorValue);
                    }
                }
            }
        }
    }
}
