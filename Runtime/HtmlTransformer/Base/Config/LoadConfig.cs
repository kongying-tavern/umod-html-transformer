using System;

namespace HtmlTransformer.Base.Config
{
    public class LoadConfig
    {
        private Func<string, string> _preprocessor = null;

        public LoadConfig RegisterPreprocessor(Func<string, string> preprocessor)
        {
            if (preprocessor == null)
            {
                // Do Nothing
                return this;
            }
            _preprocessor = preprocessor;

            return this;
        }

        public LoadConfig UnregisterPreprocessor()
        {
            _preprocessor = null;
            return this;
        }

        public string InvokePreprocessor(string html)
        {
            html = html ?? "";
            if (_preprocessor != null)
            {
                html = _preprocessor(html);
            }
            return html;
        }
    }
}
