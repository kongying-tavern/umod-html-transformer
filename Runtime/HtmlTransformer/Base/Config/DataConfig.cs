using HtmlAgilityPack;

namespace HtmlTransformer.Base.Config
{
    public class DataConfig
    {
        public string RawHtml { get; set; } = "";

        public string DocHtml { get; set; } = "";

        public HtmlDocument Doc { get; set; }
    }
}
