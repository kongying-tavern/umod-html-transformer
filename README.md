# H2X HTML Transformer

纯 C# 的 HTML 转换库。无 Unity 依赖，通过 UPM 使用。

这个库的作用是维护一套简化版的 HTML：它可以直接用于网页显示，也可以转换之后给其他场景显示。转换的目标是尽量保证显示和行为与网页版一致——只能是尽可能还原，不承诺完全相同。

## 特性

- 纯 C#，无 Unity / DLL 依赖，源码编译（内置 **原版** HtmlAgilityPack v1.12.4）
- 一套 HTML，既能直接用于网页，也能转换给其他显示场景
- 管道式转换架构，转换器 / 插件可插拔
- 底座与具体转换器分离：转换目标由所用 Transformer 决定，与内容源无关

## 开发约定

### 定位：纯 C# 库，UPM 只是分发方式

- 包里的代码只使用标准 .NET API，不引用任何 Unity 的类库（asmdef 已声明 `noEngineReferences`，误用 Unity API 会直接编译失败）；
- 日常开发和测试全部在 dotnet 环境完成，不需要安装 Unity；
- UPM 导入后由 Unity 编译同一份源码。所以每个程序集有两份工程描述：`.csproj` 给 dotnet 构建与测试用，`.asmdef` 给 Unity 编译用，它们指向的是同一批 `.cs` 文件。

### Vendored HtmlAgilityPack

- `Runtime/HtmlAgilityPack` 是原版 HtmlAgilityPack v1.12.4 的裁剪副本，仅保留参与编译的源码
- **禁止改动其任何源码文件**
- 来源、保留清单与升级方式见 [VENDORED.md](Runtime/HtmlAgilityPack/VENDORED.md)
- 需要调整解析行为时，在应用层运行时配置实现（如 `SanitizeConfig.AddTag(tag, type)` 对 `HtmlNode.ElementsFlags` 的写入）

### 程序集结构

包内两个源码程序集，各自带 `.asmdef`，与 dotnet 工程的 `ProjectReference` 一一对应：

| 程序集 | 内容 |
|---|---|
| `HtmlAgilityPack.Vendored` | vendor 底层 HTML 解析 |
| `HtmlTransformer` | 底座 + 各转换器 |

关于兼容参考目标：

- dotnet 侧以 netstandard2.1 构建，用于在编译期暴露 Unity 不可用的 API（如 `System.Drawing`）
- 使用本包的项目如果设置的是 .NET Standard 2.0 兼容级别，同样适用：当前实现没有用到任何 2.1 才有的 API

### 运行测试

测试工程已在解决方案中，仓库根目录直接：

```powershell
dotnet test
```

按名称过滤（跑单个插件/规则测试类）：

```powershell
dotnet test --filter "FullyQualifiedName~H2UnityColorExtensionTest"
```

补充说明：

- 测试统一跑在 net6.0 上，直接引用库按 netstandard2.1 编译出的版本
- .NET Framework 版本的自动化测试进程（testhost，负责运行单元测试的后台程序）在部分电脑上无法启动，所以不用它来跑测试；但库本身仍然同时面向 net472 和 netstandard2.1 两种目标框架编译
- 测试类之间不并行运行，原因见 [测试规范](Documentation~/testing.md)

### 代码格式化

使用 SDK 内置的 `dotnet format`，无需额外安装。校验只针对本仓库代码，排除 vendored HAP：

```powershell
dotnet format umod-html-transformer.sln --verify-no-changes --include "Runtime/HtmlTransformer" --include "DotnetTests~/HtmlTransformer.Tests"
```

`--verify-no-changes` 只检查不改写；去掉该标志即实际执行格式化。

### .meta 文件

本包要通过 UPM 分发，因此仓库必须携带所有资源的 `.meta` 文件（Unity 用它记录资源 GUID 与导入设置）。

- 生成方式：运行 `Tool~/gen-meta.ps1`（Windows）或 `Tool~/gen-meta.sh`（macOS / Linux）。脚本只为缺失的文件生成 meta，已存在的一律保持不变，可以反复执行；
- 首次批量生成已经离线完成；之后**每新增文件，跑一次脚本，并把新出现的 `.meta` 与该文件放进同一批提交**；
- 不要把 `*.meta` 加入 `.gitignore`；
- 离线生成的导入器块可能与 Unity 生成的略有差异：Unity 首次打开时会保留 GUID、只规范化其余字段，因此这种做法是安全的。

三个以 `~` 结尾的目录——`Documentation~`、`DotnetTests~`、`Tool~`——不需要 `.meta`：Unity 导入器会跳过 `~` 目录（这正是这个后缀的用途），所以也不会为它们生成。

### 发布（打 tag）

- 版本号由 `package.json` 的 `version` 字段决定（semver）
- 正式发布打 **annotated tag**，命名固定 `v` 前缀 + 版本号（如 `v1.0.0`）
- tag 与 UPM Git URL 的 `#v1.0.0` 锚一一对应
- tag message 简述本版要点

```powershell
git tag -a v1.0.0 -m "v1.0.0: H2X HTML Transformer 首个正式版本"
```

## 通过 UPM 使用（Unity）

包根包含 `package.json`。三种安装方式：

- 包名：`site.yuanshen.htmltransformer`
- 最低 Unity：**2021.3**
- 形态：纯 C# 源码编译，无 DLL

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
- [文档编写规范](Documentation~/documentation.md)：写文档时的语言与结构约定
- 各转换器的文档统一放在 `Documentation~/transformers/` 下
