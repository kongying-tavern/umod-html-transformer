# Unity 导入冒烟测试

发布前必须实际验证包能被 Unity 正常导入、编译。仓库提供自动化脚本，无需手动建工程。

## 运行

PowerShell（Windows）：

```powershell
pwsh -File Tool~/smoke-test.ps1
```

Bash（Windows git-bash / WSL / Linux，行为与 PowerShell 版一致）：

```bash
bash Tool~/smoke-test.sh
```

两个脚本都支持手动指定 Unity 可执行文件（`-UnityPath` 参数 / `UNITY_PATH` 环境变量）。目标主版本的**单一来源是 `package.json` 的 `"unity"` 字段**（UPM 规范字段），脚本自动读取；升级 Unity 主版本时只改 package.json 一处。需要临时覆盖可传 `-UnityVersion` 参数 / `UNITY_VERSION` 环境变量。

脚本做的事：

- 在临时目录建一个最小 Unity 工程；
- 清理仓库内的 `bin/`、`obj/` 目录（dotnet 构建残留被 Unity 误认作包资产生成 meta，所以导入前先删掉；它们已被 `.gitignore` 排除，删除不影响仓库）；
- 把本仓库以本地包的形式写进 `manifest.json`；
- 用 Unity 批处理模式（无窗口）导入并编译本包；
- 检查导入日志里有没有编译错误，输出 PASS / FAIL；
- 收尾时删除临时工程。

Unity 版本探测：找 Unity Hub 的默认安装目录 `%LOCALAPPDATA%\Programs\Unity Hub\Editor`，取其中 2021.3.x 的最新版本。探测不到（比如装在非默认位置）时报错退出，手动指定路径：

```powershell
pwsh -File Tool~/smoke-test.ps1 -UnityPath "<你的 Unity 安装目录>\Editor\Unity.exe"
```

## 临时工程

- 路径：`%TEMP%\upm-smoke-时间戳`，如 `C:\Users\<用户名>\AppData\Local\Temp\upm-smoke-20260827183947`；
- 内容：`Packages/manifest.json`（注入本仓库）、`ProjectSettings/`、`Library/`（Unity 导入缓存）、`scaffold.log` 与 `import.log`（两次运行的 Unity 日志）；
- 测试通过（PASS）：脚本自动删除整个目录；
- 测试失败（FAIL）：目录保留，方便进 `Library` 和日志定位问题，排查完手动删除。

## Unity 许可证（最常见失败原因）

**无头模式不绕过许可证校验。** 批处理启动的 Unity 和图形界面一样要求本机已激活许可——无头只是"怎么跑"，不是"要不要许可"。本机从没激活过时，Phase 1 即失败：

```
No valid Unity license. Please activate your license in the Hub.
```

属于"没许可"，先用 Unity Hub 打开任意项目完成激活，再重跑。

另一种情况是**有许可但联网刷新失败**。Unity 云许可（entitlement）机制启动时会联网换取访问令牌，机器访问不了外网时刷新失败，日志出现：

```
[Licensing::Module] Error: Access token is unavailable; failed to update
```

这**不是脚本或包的问题**——本机许可仍然有效，Unity 用本地许可继续完成导入，测试照常通过。判断方法：机器能访问外网、且用 Unity Hub 打开过任何项目（说明已激活），就放心跑。

两种情况小结：

| 日志特征 | 含义 | 处置 |
|---|---|---|
| `No valid Unity license` | 没激活过 | Hub 激活后重跑 |
| `Access token is unavailable` | 已激活但联网刷新失败 | 可忽略，测试照常有效 |

脚本任何一步失败都会自动打印 Unity 日志尾部（最后 15 行）与退出码，按输出排查即可。
