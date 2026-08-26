using HtmlAgilityPack;

namespace HtmlTransformer.Base.Extensions
{
    public interface IExtensionInterface
    {
        void Transform(HtmlDocument doc);
    }
}
