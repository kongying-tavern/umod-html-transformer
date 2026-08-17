# 插件与 DI 设计

本库把「转换策略」与「执行行为」分离：**配置（Config）** 声明「做什么」，**DI（依赖注入）** 负责执行。插件是挂在 Transform 阶段上的扩展点，承接具体输出目标的规则。

## DI（依赖注入）设计

### 为什么叫 DI

`Base/DI` 下的 `ParserLoader`、`ParserTransformer`、`ParserFinalizer` 是管线的**工作者**（worker）。它们不自行创建或持有配置与运行数据，而是由 `HtmlBaseTransformer` 在 `Process()` 里把依赖通过**方法参数注入**（method injection）交给它们——这就是「依赖注入（DI）」的直接体现。

### 三层结构

| 层 | 命名空间 | 内容 | 职责 |
|----|----------|------|------|
| 依赖（配置） | `Base.Config` | `SanitizeConfig`、`TransformConfig`…（策略声明）、`DataConfig`（运行数据） | 描述「做什么」，承载阶段间数据 |
| 注入方 | `HtmlBaseTransformer` | 持有全部配置实例 | 在 `Process()` 中把依赖注入给 DI |
| 被注入方（DI） | `Base.DI` | `ParserLoader`、`ParserTransformer`、`ParserFinalizer` | 无状态地执行，接受注入的依赖完成工作 |

### 注入方式

```csharp
public string Process(string html)
{
    this.InternalConfigure(false);

    this.loader.Execute(
        this.dataConfig, this.outputConfig, this.loadConfig, this.sanitizeConfig, html
    );
    this.transformer.Normalize(this.dataConfig, this.normalizeConfig);
    this.transformer.Transform(this.dataConfig, this.transformConfig);

    html = this.finalizer.Finalize(this.dataConfig, this.finalizeConfig);
    return html;
}
```

DI 侧没有字段、不缓存状态，依赖全部来自调用参数；`Configure()` 只负责往配置里声明策略。

### 为什么这么分

- **依赖是纯数据**：配置类只承载声明与数据，可独立测试；
- **DI 是无状态行为**：不含业务规则，只执行注入的配置，一套 DI 服务所有转换器；
- **新增转换器零改动**：只继承 `HtmlBaseTransformer` + `Configure()` 堆配置，DI 与 Config 都不用动。

### DataConfig：阶段间传递

| 字段 | 含义 |
|------|------|
| `RawHtml` | 原始输入 |
| `DocHtml` | 预处理后的 HTML（`LoadConfig` 预处理器产物） |
| `Doc` | 解析后的 `HtmlDocument`（净化 → 归一化 → 插件就地修改） |

## 插件设计

### 定位

Transform 阶段按名字常量（`ExtensionOrders`）**有序**执行一组插件，插件把 `HtmlDocument` 就地修改为输出目标特有的表现形式。

### 接口

```csharp
using HtmlAgilityPack;

public interface IExtensionInterface
{
    void Transform(HtmlDocument doc);
}
```

### 设计约束

- **只依赖 `HtmlAgilityPack.HtmlDocument`**：不依赖具体输出目标，任何转换器可复用；
- **就地修改、无返回值**：插件直接改 `doc`，插件之间不传参、无返回值依赖，管线在阶段末尾统一状态传递；
- **无跨调用状态**：插件是一次性执行的纯动作，不保留跨调用中间状态；
- **命名即标签**：插件名与处理的标签名一致（`color`、`p`、`br`…），执行顺序由 `ExtensionOrders` 确定。

### 两类模式

| 模式 | 作用 | 例子 | 特点 |
|------|------|------|------|
| 转换型 | 行内样式 / 链接 → `collval` 属性 | `ColorExtension`、`SizeExtension`、`AExtension` | 把 CSS 风格输入转成输出目标认识的标签形态 |
| 结构型 | 换行 / 段落 / 重组 | `BrExtension`、`PExtension`、`RubyExtension` | 处理布局与结构，不产生新属性 |

### 顺序敏感

执行顺序 = 注册顺序（`SetOrders` 整体重排，`MoveExtensionBefore` / `MoveExtensionAfter` 微调）。顺序取舍由插件间依赖决定，例如归一化先完成 `ruby → r`，`RubyExtension` 才能只面向 `r` 处理。

### 编写模板

```csharp
using HtmlAgilityPack;
using HtmlTransformer.Core.Base.Extensions;

public class XxxExtension : IExtensionInterface
{
    public void Transform(HtmlDocument doc)
    {
        var nodes = doc.DocumentNode.SelectNodes("//xxx");
        if (nodes == null)
        {
            return;
        }
        foreach (var node in nodes)
        {
            // 就地修改 node
        }
    }
}
```

在转换器里注册并定序：

```csharp
this.ConfigureTransform()
    .RegisterExtension("xxx", new XxxExtension())
    .SetOrders("color", "size", "xxx", "p", "br");
```

实际插件的写法见 [H2UnityTransformer（Unity 富文本）](transformers/h2-unity.md) 的「插件一览」与「插件详解」。
