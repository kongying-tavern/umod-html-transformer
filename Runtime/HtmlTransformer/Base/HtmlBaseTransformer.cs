using HtmlTransformer.Base.Config;
using HtmlTransformer.Base.DI;

namespace HtmlTransformer.Base
{
    public abstract class HtmlBaseTransformer
    {
        private bool isConfigured = false;
        private readonly DataConfig dataConfig = new DataConfig();
        private readonly OutputConfig outputConfig = new OutputConfig();
        private readonly LoadConfig loadConfig = new LoadConfig();
        private readonly SanitizeConfig sanitizeConfig = new SanitizeConfig();
        private readonly NormalizeConfig normalizeConfig = new NormalizeConfig();
        private readonly TransformConfig transformConfig = new TransformConfig();
        private readonly FinalizeConfig finalizeConfig = new FinalizeConfig();

        private readonly ParserLoader loader = new ParserLoader();
        private readonly ParserTransformer transformer = new ParserTransformer();
        private readonly ParserFinalizer finalizer = new ParserFinalizer();

        public abstract void Configure();

        public OutputConfig ConfigureOutput()
        {
            return this.outputConfig;
        }

        public LoadConfig ConfigureLoad()
        {
            return this.loadConfig;
        }

        public SanitizeConfig ConfigureSanitize()
        {
            return this.sanitizeConfig;
        }

        public NormalizeConfig ConfigureNormalize()
        {
            return this.normalizeConfig;
        }

        public TransformConfig ConfigureTransform()
        {
            return this.transformConfig;
        }

        public FinalizeConfig ConfigureFinalize()
        {
            return this.finalizeConfig;
        }

        private void InternalConfigure(bool force)
        {
            if (force)
            {
                Configure();
                this.isConfigured = true;
            }
            else if (!this.isConfigured)
            {
                Configure();
                this.isConfigured = true;
            }
        }

        public string Process(string html)
        {
            this.InternalConfigure(false);
            // Load
            this.loader.Execute(
                this.dataConfig,
                this.outputConfig,
                this.loadConfig,
                this.sanitizeConfig,
                html
            );
            this.transformer.Normalize(
                this.dataConfig,
                this.normalizeConfig
            );
            this.transformer.Transform(
                this.dataConfig,
                this.transformConfig
            );

            html = this.finalizer.Finalize(
                this.dataConfig,
                this.finalizeConfig
            );

            return html;
        }
    }
}
