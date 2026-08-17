using HtmlAgilityPack;
using HtmlTransformer.Core.Base.Config;

namespace HtmlTransformer.Core.Base.DI
{
    public class ParserLoader
    {
        private void Preprocess(DataConfig dataConfig, LoadConfig loadConfig, string html)
        {
            html = html ?? "";
            dataConfig.RawHtml = html;
            html = loadConfig.InvokePreprocessor(html);
            dataConfig.DocHtml = html;
        }

        private void Load(DataConfig dataConfig, OutputConfig outputConfig)
        {
            string html = dataConfig.DocHtml ?? "";
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            dataConfig.Doc = doc;
        }

        private void Sanitize(DataConfig dataConfig, SanitizeConfig sanitizeConfig)
        {
            var doc = dataConfig.Doc;
            doc = sanitizeConfig.InvokeSanitize(doc);
            dataConfig.Doc = doc;
        }

        public void Execute(
            DataConfig dataConfig,
            OutputConfig outputConfig,
            LoadConfig loadConfig,
            SanitizeConfig sanitizeConfig,
            string html
        )
        {
            this.Preprocess(dataConfig, loadConfig, html);
            this.Load(dataConfig, outputConfig);
            this.Sanitize(dataConfig, sanitizeConfig);
        }
    }
}
