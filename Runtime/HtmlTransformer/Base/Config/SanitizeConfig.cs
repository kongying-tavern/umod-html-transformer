using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HtmlTransformer.Base.Config
{
    public class SanitizeConfig
    {
        /// <summary>
        /// 普通容器类型（无特殊解析行为）。
        /// </summary>
        public static readonly HtmlElementFlag ElementTypeNormal = (HtmlElementFlag)0;

        /// <summary>
        /// CData 内容（原样保留，如 script/style/textarea/title）。
        /// </summary>
        public static readonly HtmlElementFlag ElementTypeCData = HtmlElementFlag.CData;

        /// <summary>
        /// 空元素（void，只能空内容）。
        /// </summary>
        public static readonly HtmlElementFlag ElementTypeEmpty = HtmlElementFlag.Empty;

        /// <summary>
        /// 闭合标签等价于开标签。
        /// </summary>
        public static readonly HtmlElementFlag ElementTypeClosed = HtmlElementFlag.Closed;

        /// <summary>
        /// 可重叠（历史语义，如 form/a）。
        /// </summary>
        public static readonly HtmlElementFlag ElementTypeCanOverlap = HtmlElementFlag.CanOverlap;

        /// <summary>
        /// void 自闭合（Empty | Closed，与 HAP 对 br 的预置一致）。
        /// </summary>
        public static readonly HtmlElementFlag ElementTypeVoid = HtmlElementFlag.Empty | HtmlElementFlag.Closed;

        private Dictionary<string, HashSet<string>> _allowedTags = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// 添加标签并指定其 HTML 解析类型（改写 <see cref="HtmlNode.ElementsFlags"/>，须在首次解析前调用）。
        /// AddTag("div", HtmlElementFlag.Empty, ":all")，不进行属性过滤
        /// AddTag("div", HtmlElementFlag.Empty, "class", "style")，进行属性过滤
        /// AddTag("div", HtmlElementFlag.Empty)，过滤掉所有属性
        /// </summary>
        public SanitizeConfig AddTag(string tagName, HtmlElementFlag elementType, params string[] allowedAttributes)
        {
            return AddTagInternal(tagName, elementType, allowedAttributes);
        }

        /// <summary>
        /// 清空 HtmlNode.ElementsFlags 的全部预置项（须在首次解析前调用）。
        /// 清空后所有标签一律按普通容器解析（void/CData 行为失效），
        /// 需要特殊解析类型的标签须用类型化 AddTag 重新声明。
        /// </summary>
        public SanitizeConfig ClearTagFlags()
        {
            HtmlNode.ElementsFlags.Clear();
            return this;
        }

        /// <summary>
        /// 清除指定标签的解析类型（从 HtmlNode.ElementsFlags 移除该键，
        /// 使其回落为普通容器），须在首次解析前调用。
        /// </summary>
        public SanitizeConfig ClearTagFlags(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                return this;
            }

            HtmlNode.ElementsFlags.Remove(tagName);
            return this;
        }

        private SanitizeConfig AddTagInternal(string tagName, HtmlElementFlag elementType, params string[] allowedAttributes)
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

            HtmlNode.ElementsFlags[tagName] = elementType;

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

                // Skip text nodes; text content should always be preserved
                if (node.NodeType == HtmlNodeType.Text)
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
