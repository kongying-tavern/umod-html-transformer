# HybridCLR 兼容性判断

结论：**本库（`HtmlTransformer` + vendored `HtmlAgilityPack`）可安全进入 HybridCLR 热更侧（Hot Update）dll，无阻断性问题；XML/XPath 部分有明确依赖，需 link.xml 保链路。**

## 程序集定位

- 两个 asmdef 均 `noEngineReferences: true`：纯 C#，零 UnityEngine 依赖，天然具备热更侧资质；
- `HtmlTransformer` 对 `HtmlAgilityPack` 的使用面：XPath 导航（`SelectNodes` / `SelectSingleNode`）+ `HtmlEntity.DeEntitize`（实体反转义，char 循环 + 静态字典，无反射）+ 节点 API（`LoadHtml`/`OuterHtml`/`CreateElement` 等）；另有 `SanitizeConfig` 写入全局静态 `HtmlNode.ElementsFlags`（r10 核查）——均无反射、无动态加载；
- 未暴露 HAP 的附加 API（`GetEncapsulatedData<T>` 泛型反射封装、`HtmlWeb` 网络爬取）给公开接口。

## 风险定位表

| HAP 风险 API | 位置 | 热更侧会被调用吗 | HybridCLR 影响 |
|---|---|---|---|
| `Activator.CreateInstance` / `MakeGenericType`（泛型反射） | `HtmlNode.Encapsulator.cs` | 否（死代码，全仓零调用点，r3 逐行核实） | 解释器可执行运行时泛型实例化；仅取方法指针/转 delegate 等场景才需 AOT 泛型补全 |
| `AppDomain.LoadFile` 动态加载 | `HtmlWeb.cs` | 否（库不做网络抓取） | IL2CPP 下不可用，调用即异常 |
| `Environment.GetCommandLineArgs` / 文件 IO | `HtmlCmdLine.cs` / `IOLibrary.cs` | 否（本机调试工具） | 无影响 |
| `System.Xml.XPath` 全链路 | `HtmlNodeNavigator` 等 | **是（每次 XPath 查询必走）** | 见下文，需 link.xml 保留 |
| 线程/异步（`Task`/`Thread`） | `HtmlWeb.cs` / `MixedCodeDocument.cs` | 否 | 无影响 |

## XML/XPath 部分（重要）

### 真实调用链

```
HtmlNode.SelectNodes("//tag")
  → HtmlNodeNavigator（HAP 内实现，派生自 XPathNavigator）
  → nav.Select(xpath)           // XPathNavigator.Select(string)  —— System.Xml 基类方法
  → XPathExpression.Compile()   // System.Xml.XPath 编译引擎
  → XPathNodeIterator          // 求值迭代
```

每次 XPath 查询都走一遍 `System.Xml.XPath` 的编译-求值管道，`XPathExpression.Compile` 等成员位于基类实现中（IL2CPP 下可能被裁）。该链路**无泛型实例**，不触发 AOT 泛型补全，但**不能赌 IL2CPP 默认保留 System.Xml，必须显式 link.xml。**

### link.xml（主工程 Assets/link.xml，r2 修订版）

> r2 攻击者视角核查（基于本地 .NET 6 System.Private.Xml 实测定性，与 Unity 2021.3 同源 CoreFX 引擎）：
> ① 2021.3 的 .NET Standard 2.1 类库中 **System.Xml 只是编译期门面（类型转发）**，实现程序集是 **System.Private.Xml**——原 link.xml 写 `System.Xml` 不生效，等于没保；
> ② 2021.2 起 **Mono 与 IL2CPP 共用同一套 CoreCLR 系类库，XPath 引擎公共与 internal 类型同名**，link.xml 一套两用；
> ③ 引擎内部类型在 **`MS.Internal.Xml.XPath`** 命名空间；原文档的 `XPathContext`/`XPathStringBuilder` 是 .NET Framework 时代 internal 类，CoreFX 引擎中**不存在**（linker 仅报警告）；
> ④ `XPathNodeIterator.MoveNext/Current` 为抽象成员，虚实现位于 internal 子类（XPathSelectionIterator/XPathEmptyIterator/XPathArrayIterator→Query/CompiledXpathExpr 求值链），点名保留不可靠，**按命名空间整保**。

```
<linker>
  <!-- 2021.3 .NET Standard 2.1(Mono/IL2CPP 共用)实现程序集; System.Xml 仅为编译期 facade -->
  <assembly fullname="System.Private.Xml">
    <!-- HAP 导航器基类依赖(XmlNameTable/IXPathNavigable 等), 保热更侧基类解析 -->
    <namespace fullname="System.Xml" preserve="all"/>
    <!-- 公共 XPath API: 导航/编译/迭代 -->
    <namespace fullname="System.Xml.XPath" preserve="all"/>
    <!-- 引擎内部: 编译(XPathParser/XPathScanner/QueryBuilder)+求值(Query/CompiledXpathExpr)
         +迭代器(XPathSelectionIterator/XPathEmptyIterator/XPathArrayIterator),
         XPathNodeIterator.MoveNext/Current 的虚实现全在此 -->
    <namespace fullname="MS.Internal.Xml.XPath" preserve="all"/>
  </assembly>
  <!-- HAP 编入热更 dll 时本段为无操作(热更程序集不参与主工程裁剪);
       仅当 HAP 留主工程 AOT 时才需要: SelectNodes/SelectSingleNode 除 HtmlNodeNavigator
       (preserve=all 已含 (HtmlDocument,HtmlNode) ctor 与 CurrentNode) 外,
       还 new HtmlNodeCollection(null) 并调用 Add/Count -->
  <assembly fullname="HtmlAgilityPack.Vendored">
    <type fullname="HtmlAgilityPack.HtmlNodeNavigator" preserve="all"/>
    <type fullname="HtmlAgilityPack.HtmlNodeCollection" preserve="all"/>
  </assembly>
</linker>
```

## 落地建议

1. **主包排除（关键，r10 核查）**：两个 asmdef 默认会被 Unity 自动编入主包（`autoReferenced: true`）。必须从主包里排除这两个程序集（如 HybridCLR 的 HotUpdate 程序集列表机制），只由热更 dll 编译携带，否则同一类型在主包与热更侧各有一份，会加载冲突或热更不生效；
2. **热更侧打包**：`Runtime/HtmlTransformer` 编入热更 dll，使用 `H2UnityTransformer`；HAP 应根据布局决定（见附录 r8：推荐 HAP 留 AOT 主工程，只引用不编译）；
3. **link.xml**：放入主工程 `Assets/link.xml`（内容如上）；
4. **源码零改动**：无需为 HybridCLR 改任何 C#；
5. **边界**：任何人直接使用 HAP 附加 API（`GetEncapsulatedData<T>` / `HtmlWeb`）做热更业务时会撞反射/动态加载限制——本库公开接口未暴露，当前安全。


---

## 附录 r8:备选布局对比(link.xml 维护 vs 热更灵活性)

**结论:布局 A(HAP 留 AOT 主工程、HtmlTransformer 进热更侧)最优。** link.xml 属三布局公共成本,换布局省不掉。

| 布局 | HybridCLR 兼容 | link.xml | 规则热更 |
|---|---|---|---|
| A:HAP AOT + HT 热更 | ✅ 热更→AOT 单向依赖,与 asmdef 关系一致,零改动 | 必需 | ✅ 全在热更侧 |
| B:HT AOT + HAP 热更 | ❌ AOT 静态引用热更程序集,IL2CPP 编译期引用不存在,须接口化拆引用、动源码 | 必需 | ❌ 规则冻结 |
| C:整库 AOT | ✅ 无额外约束 | 必需 | ❌ 不能热更 |

**link.xml 省不掉**:XPath 字符串驱动(`Compile("//tag")` 运行时发生),IL2CPP 看不到字符串;三布局 System.Xml 均 AOT,保链路不可省(r2 修订: System.Private.Xml 按 System.Xml/System.Xml.XPath/MS.Internal.Xml.XPath 三命名空间整保)。省"维护成本"只能靠脚本自动生成/校验,非换布局可解。

**选 A**:①规则即业务——扩展/标签映射/扩展顺序/pre-post 钩子全在 HtmlTransformer,全可热更;新格式加标签、改语义不发客户端,正对该场景。②零改动合规——asmdef 方向不变只拆编译分组;热更侧 `new HtmlDocument()`、跨边界传 DOM、调 AOT 的 `SelectNodes` 均 HybridCLR 常规模式,无泛型补全需求。③代价可控——HAP 是通用解析执行器,规则迭代不碰它;Encapsulator/HtmlWeb 留 AOT,风险持平;日后需热更解析内核,把 HAP 移入热更包即可,迁移平缓。

**落地**:HAP 编入主工程并从热更包排除;HT 编热更 dll;link.xml 照旧;热更包不重复带 HAP。

---

## 附录 r5:静态初始化与双实例边界(OptionDefaultStreamEncoding / HtmlEntity)

**结论:无静态构造器陷阱,但有"静态配置分区/类型割裂"边界;双挂载不产生状态冲突。**

1. **OptionDefaultStreamEncoding** 是**实例字段**(HtmlDocument.cs:131),仅在实例构造器赋值(258-271,按编译宏取 UTF8 或 Encoding.Default)。每文档各一份,与静态初始化无关,无陷阱。

2. **HtmlEntity 实体表**有**显式静态构造器**(HtmlEntity.cs:54-579),一次性向 _entityName/_entityValue 填入约 250 个实体并置 _maxEntitySize。触发点=首次访问任一静态成员或创建实例:本库使用面下是首次读属性实体值(HtmlAttribute.cs:205/233 走 DeEntitize)或首次 Entitize;纯 XPath 导航不触发。该类型无 beforefieldinit,HybridCLR 解释执行 .cctor 与 CLR 语义一致:每类型只执行一次、线程安全,无竞态;代价仅热更侧首次使用的一次性解释执行成本(约 500 次 Add)。EntityName/EntityValue getter 直出内部字典(理论可被外部改写),库内无写入路径,cctor 后内容恒同。

3. **双挂载(AOT 一份 + 热更 dll 一份)** 会编出两个程序集,.NET 类型标识=程序集+全名,故为两个不同的 HtmlEntity/HtmlDocument,各自独立静态存储、各自执行一次 cctor:
   - 不是状态冲突:实体表两侧内容恒同、互不覆盖;
   - 真正的边界是**静态配置分区**:在 AOT 副本设置的 HtmlDocument.DefaultBuilder/MaxDepthLevel/HtmlEntity.UseWebUtility 对热更副本不可见(反之亦然),"配置一次全局生效"会静默失效;
   - **类型割裂**:热更产出的 HtmlNode/HtmlDocument 不能传给 AOT 侧代码(is/cast 即 InvalidCastException),跨边界传递应禁止;
   - 双份表内存与双份初始化成本(解释执行更慢)。

**建议**:与 r8 布局 A 一致——HAP 只留在 AOT 主工程(单一类型同一性,statics 共享、cctor 一次),热更 dll 引用之,勿两边各自编译;若双副本不可避免,静态配置须在热更侧 init 重新应用,且禁止 HAP 对象跨边界传递。

---

## 附录 r9:可执行验证清单(静态判断的落地验证)

前置:Unity 2021.3.45f1(支持矩阵内),IL2CPP,安卓需 SDK/NDK(r23);包:com.code-philosophy.hybridclr(git)+本库(file:)。

1. `dotnet test` 182 例过(基线);
2. 建工程装两包;HotUpdateScripts/ 放热更代码(asmdef autoReferenced:false);HybridCLR Installer 打补丁;Generate>All 自动生成 link.xml/AOTGenericReferences;
3. 构建脚本编热更 dll 放 StreamingAssets;运行时 Assembly.Load 调入;
4. 热更 dll 自检:导出 182 组「输入→期望」为内嵌 JSON,逐条比对,输出 pass/总数;
5. Generate>All 后**以生成物为准核对 link.xml**(见 r2: 手写 HtmlAgilityPack.Vendored 条目在热更布局下是无操作,Generate 会自动保 System.Xml.XPath);
6. 先 Windows IL2CPP,再安卓真机(Strip off/on 各一次);
7. 真实富文本样本与 Editor 输出 diff。

**静态分析最可能出错的一点**(r9 证伪):手写 link.xml 保 HtmlAgilityPack.Vendored——热更程序集不参与主工程链接器裁剪,该条目无效;正确做法是 HybridCLR Generate 自动生成的 link.xml(它按热更引用自动保 System.Xml.XPath)。真机才能见分晓:两套 XPath 实现(.NET 测试 vs Unity 内置)的虚拟调用、状态差异,以及 System.Private.Xml(AOT)与解释器中的 HtmlNodeNavigator 的协作。

---

## 附录 r7:TFM/程序集名核查(dotnet vs asmdef 差异化)

**结论:兼容,但两个硬前提——热更包只用 netstandard2.1 产物;程序集名与 asmdef 对齐。**

1. **net472 产物勿入热更包**(实测 AssemblyRef):net472 引用 `System.Xml 4.0.0.0`(完整框架标识),Unity 2021.3 的 2.1 档无此标识,按名解析失败。netstandard2.1 产物仅引用 `netstandard 2.1.0.0`,经转发折叠解析到 System.Xml.XPath,**兼容**——热更包只用 netstandard2.1 产物。
2. **#if 分支**:`NET8_0` 均不定义恒走 #else,XPath 文件分支正确。符号名差异(csproj 定义 `NETSTANDARD`,Unity 定义 `NET_STANDARD_2_1`)只影响 HtmlWeb.cs 的 HttpClient/HttpWebRequest 走向,库未经过这些分支,无影响。
3. **程序集名(已修复)**:asmdef 名 `HtmlAgilityPack.Vendored`,csproj 原未设 AssemblyName → dotnet 产出 `HtmlAgilityPack.dll`,与 Unity 侧引用名不符,热更加载会失败。**已在本仓库修复**:两个 csproj 显式声明 `<AssemblyName>`(HtmlAgilityPack.Vendored / HtmlTransformer)与 asmdef 名对齐。