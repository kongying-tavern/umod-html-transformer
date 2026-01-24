using HtmlTransformer.Core.Base.Extensions;
using System.Collections.Generic;

namespace HtmlTransformer.Core.Base.Config
{
    public class TransformConfig
    {
        public Dictionary<string, IExtensionInterface> Extensions { get; } = new Dictionary<string, IExtensionInterface>();

        public List<string> ExtensionOrders { get; } = new List<string>();

        public TransformConfig RegisterExtension(string name, IExtensionInterface extension)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return this;
            }

            Extensions[name] = extension;
            ExtensionOrders.Add(name);

            return this;
        }

        public TransformConfig UnregisterExtension(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return this;
            }

            Extensions.Remove(name);
            ExtensionOrders.Remove(name);

            return this;
        }

        public TransformConfig SetOrders(params string[] orders)
        {
            var orderList = new List<string>(orders);
            ExtensionOrders.Clear();
            ExtensionOrders.AddRange(orderList);
            return this;
        }

        public TransformConfig MoveExtensionBefore(string findName, string beforeName)
        {
            if (string.IsNullOrWhiteSpace(findName) || string.IsNullOrWhiteSpace(beforeName))
            {
                return this;
            }

            // Test whether source and target are present
            int findIndex = ExtensionOrders.IndexOf(findName);
            int beforeIndex = ExtensionOrders.IndexOf(beforeName);
            if (findIndex == -1 || beforeIndex == -1)
            {
                // Not found
                return this;
            }

            ExtensionOrders.RemoveAt(findIndex);
            // Redo find to avoid array shift effect
            // No need to test absence because of previous test
            beforeIndex = ExtensionOrders.IndexOf(beforeName);
            ExtensionOrders.Insert(beforeIndex, findName);

            return this;
        }

        public TransformConfig MoveExtensionAfter(string findName, string afterName)
        {
            if (string.IsNullOrWhiteSpace(findName) || string.IsNullOrWhiteSpace(afterName))
            {
                return this;
            }

            // Test whether source and target are present
            int findIndex = ExtensionOrders.IndexOf(findName);
            int afterIndex = ExtensionOrders.IndexOf(afterName);
            if (findIndex == -1 || afterIndex == -1)
            {
                // Not found
                return this;
            }

            ExtensionOrders.RemoveAt(findIndex);
            // Redo find to avoid array shift effect
            // No need to test absence because of previous test
            afterIndex = ExtensionOrders.IndexOf(afterName);
            ExtensionOrders.Insert(afterIndex + 1, findName);

            return this;
        }

        public void ApplyTransformers(HtmlAgilityPack.HtmlDocument doc)
        {
            if (doc == null)
            {
                return;
            }

            foreach (string name in ExtensionOrders)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                IExtensionInterface extension;
                if (Extensions.TryGetValue(name, out extension))
                {
                    extension.Transform(doc);
                }
            }
        }
    }
}
