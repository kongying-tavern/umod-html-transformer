# 编写一个新的转换器

基于底座 `HtmlBaseTransformer` 继承编写。底座管线机制见 [底座管线](base-pipeline.md)，插件设计与扩展口见 [插件与 DI 设计](plugin-design.md)。

## 入口约定

基座只定义实例入口 `Process(html)`；每个转换器**约定**提供一个 `public static string Transform(string html)` 一行代理，让调用方直接以「转换器名.Transform」使用：

```csharp
public class MyTransformer : HtmlBaseTransformer
{
    public static string Transform(string html)
    {
        return new MyTransformer().Process(html);
    }
}
```

不把入口硬编码进基座的原因：C# 静态方法不参与继承，基座无法在静态上下文获得具体转换器的 `Configure()` 声明；代理行足够薄，由各转换器自己维护更清晰。

## Configure() 声明各阶段策略

```csharp
public class MyTransformer : HtmlBaseTransformer
{
    public static string Transform(string html) => new MyTransformer().Process(html);

    public override void Configure()
    {
        this.ConfigureLoad()
            .RegisterPreprocessor(html => /* 预处理 */);
        this.ConfigureSanitize()
            .AddTag("xxx", SanitizeConfig.ElementTypeNormal);
        this.ConfigureNormalize()
            .AddTagMapping(...);
        this.ConfigureTransform()
            .RegisterExtension("xxx", new XxxExtension())
            .SetOrders("xxx");
        this.ConfigureFinalize()
            .RegisterAfterFinalizeHook(html => /* 后处理 */);
    }
}
```

| 阶段 | 声明内容 | 是否必需 |
|------|----------|----------|
| Load | 预处理器（如去换行） | 可选 |
| Sanitize | 白名单 `AddTag(tag, type, attrs…)`；`type` 为解析类型（必传，见 [底座管线](base-pipeline.md) 的解析类型表与已知问题） | 必需 |
| Normalize | `AddTagMapping` 标签归一化 | 按需 |
| Transform | 注册插件并 `SetOrders` 定序 | 按需 |
| Finalize | 后处理钩子（如实体反转义） | 可选 |

## 插件编写

Transform 阶段插件按名字**有序**执行，就地修改 `HtmlDocument`，是承接输出目标规则的主要扩展点。接口、设计约束、两类模式与编写模板见 [插件与 DI 设计](plugin-design.md)。

## 命名空间与文档约定

- 新转换器放在独立命名空间（如 `HtmlTransformer.Xxx`）；
- 转换文档在 `docs/transformers/` 下新建对应文件；
- 回归测试遵循 [测试规范](testing.md)，测试说明随转换器文档放置。

## 参考实现

内置的 [H2UnityTransformer](transformers/h2-unity.md) 是完整示例：白名单标签逐一声明解析类型、四组归一化映射、五个插件的注册与定序、Finalize 反转义钩子均可对照。
