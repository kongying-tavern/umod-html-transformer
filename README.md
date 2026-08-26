# H2X HTML Transformer

纯 C# 的 HTML 转换库。无 Unity 依赖，通过 UPM 使用。

以**简化的通用 HTML** 为唯一内容源：同一套 HTML 既可以直接交给 HTML 渲染器渲染，也可以通过 Transformer 转换成其他文本表达形式供别的渲染器使用，尽可能保证不同渲染器之间的样式与行为一致。

## 特性

- 纯 C#，无 Unity / DLL 依赖，源码编译（内置 **原版** HtmlAgilityPack v1.12.4）
- 单一内容源、多渲染目标
- 管道式转换架构，转换器 / 插件可插拔
- 底座与具体转换器分离：转换目标由所用 Transformer 决定，与内容源无关

## 开发约定

- `Runtime/HtmlAgilityPack` 是 **vendored 原版** HtmlAgilityPack v1.12.4 的裁剪副本（仅保留参与编译的源码），**禁止改动其任何源码文件**；来源、保留清单与升级方式见 [VENDORED.md](Runtime/HtmlAgilityPack/VENDORED.md)。需要调整解析行为时，在应用层运行时配置实现（如 `SanitizeConfig.AddTag(tag, type)` 对 `HtmlNode.ElementsFlags` 的写入）。
- **程序集结构**：包内两个源码程序集——`HtmlAgilityPack.Vendored`（vendor 底层解析）与 `HtmlTransformer`（底座 + 转换器），各自带 `.asmdef`，与 dotnet 工程的 `ProjectReference` 一一对应。dotnet 侧以 netstandard2.1 作兼容参考目标；消费项目为 .NET Standard 2.0 级别时同样适用（当前实现未使用任何 2.1-only API）。
- **运行测试**：测试工程已在解决方案中，仓库根目录直接：

  ```powershell
  dotnet test
  ```

  按名称过滤（跑单个插件/规则测试类）：

  ```powershell
  dotnet test --filter "FullyQualifiedName~H2UnityColorExtensionTest"
  ```

  > 测试统一跑在 net6.0（直接消费库的 netstandard2.0 资产）。net472 桌面 testhost 在部分环境无法启动，不作为默认目标；库仍以 net472 + netstandard2.0 双目标构建，后者用于在 dotnet 构建期暴露 Unity 不可用的 API。

- **代码格式化**：使用 SDK 内置的 `dotnet format`（无需额外安装）。校验只针对本仓库代码，排除 vendored HAP：

  ```powershell
  dotnet format umod-html-transformer.sln --verify-no-changes --include "Runtime/HtmlTransformer" --include "DotnetTests~/HtmlTransformer.Tests"
  ```

  `--verify-no-changes` 只检查不改写；去掉该标志即实际执行格式化。

- **发布（打 tag）**：版本号由 `package.json` 的 `version` 字段决定（semver）。正式发布打 **annotated tag**，命名固定 `v` 前缀 + 版本号（`v1.0.0`），与 UPM Git URL 的 `#v1.0.0` 锚一一对应，message 简述本版要点：

  ```powershell
  git tag -a v1.0.0 -m "v1.0.0: H2X HTML Transformer 首个正式版本"
  ```

## 通过 UPM 使用（Unity）

包根包含 `package.json`（名称 `site.yuanshen.htmltransformer`），最低 Unity **2021.3**，纯 C# 源码编译、无 DLL。三种安装方式：

**方式一：Package Manager（推荐）**

`Window > Package Manager > + > Add package by git URL…`，粘贴：

```
https://github.com/kongying-tavern/umod-html-transformer.git
```

需固定版本时追加 tag 锚（UPM 的 Git tag 强制 `v` 前缀）：

```
https://github.com/kongying-tavern/umod-html-transformer.git#v1.0.0
```

**方式二：本地磁盘**

`+ > Add package from disk…`，选择仓库根目录（含 `package.json` 的文件夹）。

**方式三：直接编辑 manifest**

在 `Packages/manifest.json` 的 `dependencies` 中声明：

```json
"dependencies": {
  "site.yuanshen.htmltransformer": "https://github.com/kongying-tavern/umod-html-transformer.git#v1.0.0"
}
```

> 包为源码编译，Unity 导入后自动编译；改动源码会触发重新编译并同步进 `Library/PackageCache`。

## 快速开始

所有转换器继承底座 `HtmlBaseTransformer`，在 `Configure()` 中声明各阶段策略，并以静态 `Transform(html)` 直呼（内部一行代理到 `Process`）：

```csharp
using HtmlTransformer.Base;

public class MyTransformer : HtmlBaseTransformer
{
    public static string Transform(string html) => new MyTransformer().Process(html);

    public override void Configure()
    {
        this.ConfigureSanitize().AddTag("xxx", SanitizeConfig.ElementTypeNormal);
        this.ConfigureTransform()
            .RegisterExtension("xxx", new XxxExtension())
            .SetOrders("xxx");
    }
}

string output = MyTransformer.Transform(html);
```

完整编写步骤见 [编写一个新转换器](Documentation~/writing-a-transformer.md)；底座管线机制见 [底座管线](Documentation~/base-pipeline.md)。

## 内置转换器

- [H2UnityTransformer](Documentation~/transformers/h2-unity.md)：Unity 富文本转换器

## 文档

- [底座管线](Documentation~/base-pipeline.md)：通用转换机制、配置、插件接口
- [编写一个新转换器](Documentation~/writing-a-transformer.md)：底座入门的完整步骤
- [插件与 DI 设计](Documentation~/plugin-design.md)：插件设计思路与配置 / 依赖注入分离设计
- [测试规范](Documentation~/testing.md)：通用测试约定与运行方式
- 各转换器的文档统一放在 `Documentation~/transformers/` 下
