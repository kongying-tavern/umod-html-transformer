using System.Text;

namespace HtmlTransformer.Core.Base.Config
{
    public class OutputConfig
    {
        public bool PrettyPrint { get; private set; } = false;

        public OutputConfig SetPrettyPrint(bool prettyPrint)
        {
            this.PrettyPrint = prettyPrint;
            return this;
        }

        public Encoding Charset { get; private set; } = Encoding.UTF8;

        public OutputConfig SetCharset(Encoding charset)
        {
            this.Charset = charset;
            return this;
        }

        public OutputConfig()
        {
            // Constructor for default values
        }
    }
}
