using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;

namespace HtmlTransformer.Core.Base.Config
{
    public class NormalizeConfig
    {
        private Dictionary<string, HashSet<string>> _normalizeTags = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// 添加标签映射
        /// </summary>
        /// <param name="replaceTagName">替换标签名</param>
        /// <param name="findTagNames">匹配标签名</param>
        public NormalizeConfig AddTagMapping(string replaceTagName, params string[] findTagNames)
        {
            if (string.IsNullOrWhiteSpace(replaceTagName))
            {
                return this;
            }

            var allowedAttributes = new HashSet<string>(findTagNames);
            _normalizeTags[replaceTagName] = allowedAttributes;

            return this;
        }

        public NormalizeConfig RemoveTagMapping(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return this;
            }

            _normalizeTags.Remove(tagName);
            return this;
        }

        public void ApplyNormalizer(HtmlDocument doc)
        {
            if (doc == null)
            {
                return;
            }

            foreach (var kvp in _normalizeTags)
            {
                string replaceTagName = kvp.Key;
                var findTagNames = kvp.Value;

                if (string.IsNullOrWhiteSpace(replaceTagName))
                {
                    continue;
                }
                else if (findTagNames.Count == 0)
                {
                    continue;
                }

                // Create XPath selector for all tags to find
                string xpathSelector = string.Join(" | ", findTagNames.Select(tag => $"//{tag}"));

                var nodes = doc.DocumentNode.SelectNodes(xpathSelector);
                if (nodes != null)
                {
                    var nodesArray = nodes.ToArray(); // Create array to avoid modification during iteration

                    foreach (var node in nodesArray)
                    {
                        // Create a new node with the desired tag name
                        var newNode = doc.CreateElement(replaceTagName);

                        // Copy attributes
                        foreach (var attr in node.Attributes)
                        {
                            newNode.Attributes.Append(attr.Clone());
                        }

                        // Copy child nodes
                        foreach (var childNode in node.ChildNodes)
                        {
                            newNode.AppendChild(childNode.Clone());
                        }

                        // Replace the old node with the new one
                        node.ParentNode.ReplaceChild(newNode, node);
                    }
                }
            }
        }
    }
}
