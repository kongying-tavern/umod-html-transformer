using HtmlAgilityPack;
using System;

namespace HtmlTransformer.Base.Config
{
    public class FinalizeConfig
    {
        private Action<HtmlDocument> _beforeFinalize = null;
        private Func<string, string> _afterFinalize = null;

        public FinalizeConfig RegisterBeforeFinalizeHook(Action<HtmlDocument> beforeFinalize)
        {
            if (beforeFinalize == null)
            {
                return this;
            }

            _beforeFinalize = beforeFinalize;
            return this;
        }

        public FinalizeConfig UnregisterBeforeFinalizeHook()
        {
            _beforeFinalize = null;
            return this;
        }

        public void InvokeBeforeFinalizeHook(HtmlDocument doc)
        {
            if (doc == null)
            {
                return;
            }

            if (_beforeFinalize != null)
            {
                _beforeFinalize(doc);
            }
        }

        public FinalizeConfig RegisterAfterFinalizeHook(Func<string, string> afterFinalize)
        {
            if (afterFinalize == null)
            {
                return this;
            }

            _afterFinalize = afterFinalize;
            return this;
        }

        public FinalizeConfig UnregisterAfterFinalizeHook()
        {
            _afterFinalize = null;
            return this;
        }

        public string InvokeAfterFinalizeHook(string html)
        {
            html = html ?? "";
            if (_afterFinalize != null)
            {
                html = _afterFinalize(html);
            }

            return html;
        }
    }
}
