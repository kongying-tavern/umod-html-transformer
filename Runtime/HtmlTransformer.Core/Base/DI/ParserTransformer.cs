using HtmlTransformer.Core.Base.Config;

namespace HtmlTransformer.Core.Base.DI
{
    public class ParserTransformer
    {
        public void Normalize(DataConfig dataConfig, NormalizeConfig normalizeConfig)
        {
            var doc = dataConfig.Doc;
            normalizeConfig.ApplyNormalizer(doc);
        }

        public void Transform(
            DataConfig dataConfig,
            TransformConfig transformConfig
        )
        {
            var doc = dataConfig.Doc;
            transformConfig.ApplyTransformers(doc);
        }
    }
}
