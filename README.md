# H2X HTML Transformer

纯 C# 的 HTML 转换库。无 Unity 依赖，通过 UPM 使用。

以**简化的通用 HTML** 为唯一内容源：同一套 HTML 既可以直接交给 HTML 渲染器渲染，也可以通过 Transformer 转换成其他文本表达形式供别的渲染器使用，尽可能保证不同渲染器之间的样式与行为一致。

## 特性

- 纯 C#，无 Unity / DLL 依赖，源码编译
- 单一内容源、多渲染目标
- 管道式转换架构，转换器 / 插件可插拔
- 底座与具体转换器分离：转换目标由所用 Transformer 决定，与内容源无关

## 快速开始

```csharp
using HtmlTransformer.Core.Unity;

string html = "<p><color style=\"--color: #abc\">r &amp; g</color></p>";
string richText = H2UnityTransformer.Transform(html);
```

完整示例、输入输出规则见 [H2UnityTransformer](docs/transformers/h2-unity.md)。

## 文档

- [底座管线](docs/base-pipeline.md)：通用转换机制、配置、插件接口
- [插件与 DI 设计](docs/plugin-design.md)：插件设计思路与配置 / 依赖注入分离设计
- [测试规范](docs/testing.md)：通用测试约定，含各转换器测试说明入口
- [H2UnityTransformer](docs/transformers/h2-unity.md)：目前内置的 Unity 富文本转换器
- 新转换器的文档统一追加在 `docs/transformers/` 下
