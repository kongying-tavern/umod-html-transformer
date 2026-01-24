using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HtmlTransformer.Core.Base.Config
{
    public class SanitizeConfig
    {
        private Dictionary<string, HashSet<string>> _allowedTags = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// 添加标签，
        /// AddTag("div", ":all")，不进行属性过滤
        /// AddTag("div", "class", "style")，进行属性过滤
        /// AddTag("div")，过滤掉所有属性
        /// </summary>
        public SanitizeConfig AddTag(string tagName, params string[] allowedAttributes)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return this;
            }

            HashSet<string> attributesSet;
            if (allowedAttributes == null || allowedAttributes.Length == 0)
            {
                // No attributes specified, create empty set (will filter all attributes)
                attributesSet = new HashSet<string>();
            }
            else if (allowedAttributes.Length == 1 && allowedAttributes[0] == ":all")
            {
                // Special case ":all" - allow all attributes
                attributesSet = new HashSet<string> { ":all" };
            }
            else
            {
                // Regular case - allow only specified attributes
                attributesSet = new HashSet<string>(allowedAttributes);
            }

            _allowedTags[tagName] = attributesSet;

            return this;
        }

        public SanitizeConfig RemoveTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return this;
            }

            _allowedTags.Remove(tagName);
            return this;
        }

        public HtmlDocument InvokeSanitize(HtmlDocument doc)
        {
            if (doc == null)
            {
                return null;
            }

            // Create a new document for sanitized content
            var sanitizedDoc = new HtmlDocument();
            sanitizedDoc.LoadHtml(doc.DocumentNode.OuterHtml);

            // Iterate through all nodes and remove disallowed tags/attributes
            var allNodes = sanitizedDoc.DocumentNode.DescendantsAndSelf().ToList();
            foreach (var node in allNodes.ToList()) // ToList to avoid modification during iteration
            {
                // Skip the document root node
                if (node.NodeType == HtmlNodeType.Document)
                    continue;

                if (!_allowedTags.ContainsKey(node.Name.ToLower()))
                {
                    // If tag is not allowed, remove it but preserve its children (if any)
                    var nextSibling = node.NextSibling;
                    var parent = node.ParentNode;

                    if (parent != null)
                    {
                        // Move children to parent if they exist
                        if (node.HasChildNodes)
                        {
                            var childrenList = node.ChildNodes.ToList();
                            foreach (var child in childrenList)
                            {
                                parent.InsertBefore(child, nextSibling);
                            }
                        }

                        // Remove the node itself
                        node.Remove();
                    }
                }
                else
                {
                    // Tag is allowed, but check attributes
                    var allowedAttrs = _allowedTags[node.Name.ToLower()];

                    // If ":all" is in the allowed attributes, skip attribute filtering
                    if (allowedAttrs.Contains(":all"))
                    {
                        // Do nothing - allow all attributes
                    }
                    else
                    {
                        var attrsToRemove = new List<HtmlAttribute>();

                        foreach (var attr in node.Attributes)
                        {
                            // If attribute is not in allowed list, remove it
                            if (!allowedAttrs.Contains(attr.Name))
                            {
                                attrsToRemove.Add(attr);
                            }
                        }

                        foreach (var attr in attrsToRemove)
                        {
                            node.Attributes.Remove(attr);
                        }
                    }
                }
            }

            return sanitizedDoc;
        }
    }
}
