using HtmlAgilityPack;

namespace HtmlTransformer.Core.Base.Extensions
{
    public interface IExtensionInterface
    {
        void Transform(HtmlDocument doc);
    }
}
