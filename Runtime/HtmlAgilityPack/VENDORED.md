# Vendored: Html Agility Pack

- **来源**:[zzzprojects/html-agility-pack](https://github.com/zzzprojects/html-agility-pack),tag `1.12.4`
- **许可证**:MIT(见同目录 [LICENSE](LICENSE))
- **约定**:**禁止改动任何源码文件的内容**。需要调整解析行为时,在应用层运行时配置实现
  (如 `SanitizeConfig.AddTag(tag, type)` 对 `HtmlNode.ElementsFlags` 的写入)。
- **升级方式**:从上游对应 tag 重新复制以下清单中的文件,覆盖后逐字节 diff 校验。

## 保留清单

| 路径 | 说明 |
|---|---|
| `*.cs`(根目录 39 个) | 上游 `src/HtmlAgilityPack.Shared/*.cs`,唯一参与编译的实现 |
| `Metro/*.cs`(2 个) | 随 Shared 原样保留,`#if METRO` 保护,Unity 下不参与编译 |
| `LICENSE` | 上游许可证原文 |

## 相对上游删除的内容

- 各目标框架文件夹(`Net20`…`UAP10` 等):仅含 AssemblyInfo / csproj / snk,无实现代码
- `src/Tests/`:上游自带测试(NUnit / MSTest / xunit),不属于本包
- `src/HtmlAgilityPack.sln`、shared project 元数据(`.shproj`/`.projitems`)、测试 html fixtures、`.editorconfig` 等工程周边文件
