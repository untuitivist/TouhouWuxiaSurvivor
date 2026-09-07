# 域名部署与双端发布

## alpha-0.1.1 双端发布完成（2026-09-07）

- 本轮发布共用动态贴图修复，Windows 文件版本为 0.1.1.0；两端从同一干净提交构建，正式导出产物新增长局绘制对照检查。
- 已从干净源码提交 `71e322076ea1ebf9a1d95066383485caa190ac2b` 构建并发布两端。后续提交仅记录验收结果，不改变已交付产物的源码修订。
- Windows：`release/TouhouWuxiaSurvivor_alpha-0.1.1.exe`，193,121,856 字节；SHA-256：`1E01EB37525B9ECE3955F8F3A70EAE95CABD021855E77697E53E124AE6E51C5E`。内嵌 Microsoft.NETCore.App 8.0.6，22 项独立 EXE 检查通过，其中包含灵梦、魔理沙各 72 项精灵对照和 12 处完整战斗对照；隔离目录检查前后均仅含 EXE，截图写入外部验收目录。
- Web：实际启用 `alpha-0.1.1-71e3220-20260907T040525Z`（目录时间为 UTC），本机构建 `20260907-114847-651`，服务器 active.json 已复核版本、源码、threadSupport=false 和配套 Windows 哈希。核心下载 30,585,080 字节（约 30.59 MB），不含页面、脚本及协议开销。
- 本地验收：20 次原始下载、两位角色动态像素回归、有／无隔离 × 原始／gzip 四组共 20 项游戏流程、六项加载异常场景及 DPR 1/3 渲染检查通过；源码 29 项核心、UI、14 项 JavaScript 与七项部署安全测试通过。
- 公网：正常桌面、触控和移除页面隔离响应头的触控入口全部通过；无隔离入口实际没有 SharedArrayBuffer。触控刷新保存、多指操作、竖屏暂停正常，失败请求与运行错误均为空，根站点及既有路由状态不变。报告：`artifacts/deployment/alpha-0.1.1-71e3220-20260907T040525Z/public-verification/report.json`。
- 回滚备份：`/srv/touhou-survivor/backups/alpha-0.1.1-71e3220-20260907T040525Z/`。完整版本日志同时保存在 `release/CHANGELOG_alpha-0.1.1.md`；全部旧版与回滚资源保留。发黑未单独复现、手机真机性能未验收的限制不变。

## alpha-0.1.0 双端发布完成（2026-09-07）

- 用户确认本轮发布号为 alpha-0.1.0，Windows 文件版本为 0.1.0.0；仍属 alpha 阶段。alpha-0.0.10 是保留的本地未发布候选，不是线上发布历史。
- Windows EXE 与单线程 Web 均来自干净源码提交 `07b96e7195283479ebb6c26a7ec7d2c2fb69ef1f`，已推送并部署；后续验收脚本与文档提交不改变这两个已验证的游戏产物。
- 下方首次部署及开发阶段记录保留为历史，最终交付状态以本节后续验收记录为准。

- Windows：`release/TouhouWuxiaSurvivor_alpha-0.1.0.exe`，193,108,552 字节，内嵌 Microsoft.NETCore.App 8.0.6；SHA-256：`082CD2969F7CE89C7A3973A5693267700E4F10186E7FC631BABDADA15110882F`。仅 EXE 目录且排除全局 .NET 的 20 项检查通过，报告位于 `artifacts/alpha-0.1.0-export-validation/report.json`。
- Web：实际启用目录 `alpha-0.1.0-07b96e7-20260907T022831Z`，目录时间为 UTC；服务器 `active.json` 已复核 `threadSupport=false`、版本及同源 Windows 哈希。核心下载共 30,580,142 字节（约 30.58 MB），不含页面、脚本和协议开销。
- 本地：20 次真实原始资源下载（单请求、完整字节、SHA-256、零请求失败）；有／无隔离 × 原始／gzip 的 20 项游戏流程；六项加载异常场景及 DPR 1/3 渲染验证全部通过。高 DPI 桌面模拟不代表手机帧率。
- 公网：正常桌面、触控与移除页面隔离响应头的触控环境通过，后者实际为 `crossOriginIsolated=false` 且 `SharedArrayBuffer` 不存在；两种触控环境均验证刷新保存、多指操作和竖屏暂停。根站点、`/tusharedata/` 与拒绝路由状态保持不变。
- 首轮公网进入战斗后，验收将引擎每十秒输出的 WASM 依赖等待状态误判为异常。核对实际导出引擎后，仅将启动期间完整、精确匹配的三行已知等待日志单独保留；未知依赖、不完整日志、启动后错误、WebGL 错误、页面异常和失败请求仍不放行，六项分类回归通过。初次失败证据保存在 `public-verification-initial-wait-diagnostic`，未隐藏或删除。
- 公网最终报告及截图：`artifacts/deployment/alpha-0.1.0-07b96e7-20260907T022831Z/public-verification/report.json`。本机到硅谷的三种入口启动时间分别约 69.2、18.4、59.9 秒，首次检查约 149.5 秒；加载时间随网络与缓存变化，不能称为秒开。小米浏览器、Android/iPhone 真机性能仍待复测。
- 回滚备份：`/srv/touhou-survivor/backups/alpha-0.1.0-07b96e7-20260907T022831Z/`；旧线上资源、历史 EXE、完整更新记录及未发布候选全部保留。

## 已确认的部署目标

- 入口：`https://allinagent.top/TouhouSurvivor/`。
- 服务器：`ubuntu@170.106.119.27`，SSH 私钥由用户在本机提供，不入库、不上传。
- Web 服务：现有 Caddy；根站点、`/tusharedata/` 和现有拒绝路由必须保持原样。
- GitHub：`https://github.com/untuitivist/TouhouWuxiaSurvivor.git`，分支 `main`。
- 服务器源码目录：`/home/ubuntu/touhou-survivor`；首次 clone，后续 `git pull --ff-only origin main`。检测到工作区修改、错误远端、错误分支或提交不一致时停止，不强制覆盖。

## 为什么 Git 与游戏产物分开

同一仓库维护 C# 源码、资源和部署脚本。Windows 构建机完成实验 Web 编译及验证，服务器通过 Git 获取同一提交的代码，通过 SCP 接收已验证的静态产物。服务器不安装另一套 Godot/.NET 构建环境，也不把近 100 MB 的生成文件或 SDK 缓存写入 Git 历史。

`git pull` 负责源码和部署脚本更新，并不把 C# 自动变成浏览器能运行的文件。一次完整网页更新需要“提交干净源码 → 同源双端构建与验证 → 推送 → 服务器拉取 → 上传并激活产物 → 公网验证”。

## 执行更新

在已配置的本机仓库根目录执行：

```bat
build_release.cmd
pwsh -NoProfile -File tools/rebirth/verify_release.ps1
build_web.cmd -Threadless
tools\threadless\verify.cmd
tools\platform\verify_loading.cmd --compatible
git push origin main
deploy_web.cmd -KeyPath "<private-key-path>" -Threadless
tools\platform\verify_deployment.cmd
```

游戏源文件必须先完成日常提交。若在构建后修改玩法、资源、导出配置或游戏更新日志，必须重新构建验证；部署工具校验源文件与产物 SHA-256，不会悄悄部署未经测试的内容。部署脚本本身也必须提交并推送，让服务器运行精确的同一提交。

alpha-0.0.10 起本轮发布选择单线程兼容构建，入口取自 `artifacts/web-compatible-latest.json`。部署门禁要求原始/gzip、有隔离/无隔离四组完整游戏回归通过，并要求同版本、同源码提交的独立 Windows EXE 验收记录；不再把仅一端成功算作完整发布。服务器可以保留隔离响应头，但兼容产物自身不再依赖隔离或共享内存。公网回归另测移除页面隔离头的启动环境，不通过伪造浏览器能力完成测试。

首次连接需先人工检查服务器身份并将 SSH 主机公钥记录到忽略目录 `artifacts/deployment/known_hosts`；部署命令使用严格校验，不关闭主机身份检查。不要提交私钥、SSH 登录信息导出、用户会话或产物目录。

## 激活方式

- 原始构建文件在 `/srv/touhou-survivor/public/releases/<release-id>/` 中保持字节不变，并校验校验和。
- 稳定入口 `public/index.html` 只改生成配置中的资源 URL，指向完整、不可变的版本目录。浏览器地址保持 `/TouhouSurvivor/`；旧页面继续使用旧资源，不会把旧 HTML 和新 WASM/PCK 混装。
- 大文件在服务器生成 gzip 旁路文件；入口重新验证缓存，带版本号的资源可长期缓存。
- Caddy 只增加 `touhou-survivor.caddy` 的独立导入；跨源隔离、安全头、压缩和静态文件规则仅作用于游戏子路径。不存在的资源返回 404，不返回根站点 SPA。
- 所有配置和旧入口在 `/srv/touhou-survivor/backups/<release-id>/` 保留；激活前检查配置指纹，使用部署锁，并运行 `caddy validate` 后才平滑 reload。自动健康检查失败时恢复旧配置和旧入口。
- 上传包和历史版本不自动删除；清理前按用户要求列出范围并询问。

## 回滚与检查

`/srv/touhou-survivor/active.json` 记录当前来源提交、版本、入口哈希和备份位置。本机 `artifacts/deployment/latest.json` 指向本次输出和公网验证报告。

首次部署若需要完全撤回路由，可用该次备份的 `Caddyfile` 恢复主配置，先 `caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile`，再 `systemctl reload caddy`。后续更新通常只需恢复该次备份的 `index.html`：旧资源一直保留。回滚前检查其后是否有其他站点配置修改，不能覆盖他人更新。

公网验证检查真实 TLS、重定向、WASM MIME、隔离响应头、压缩、缓存、缺失文件 404，以及原有站点状态；浏览器检查正常入口、中文、桌面开局、移动/F3、触控、刷新存档和竖屏暂停。模拟手机不等于 Android/iPhone 实机验收，真实音效和弱网体验仍需用户实测。

## 首次部署记录

- 稳定入口已通过公网验证，实际启用目录：`alpha-0.0.9-f605aa0-20260906T164408Z`，时间戳以 UTC 表示。
- 已部署来源提交：`f605aa0b8f2bea207238412b5533851abc2d2c89`。后续仅部署记录的文档提交不改变该静态产物的来源；站点内嵌日志是构建时的日志。
- 原始文件约 98.2 MB；生成 gzip 旁路文件合计 32,161,645 字节。公网 HEAD 验证 WASM 为 13,080,800 字节、PCK 为 18,973,660 字节，均使用 gzip 和不可变资源缓存。
- 本机公网 Edge 152.0.4191.62 测试：正常桌面入口无诊断参数，能开局、移动和显示 F3；触控模拟通过三指控制、抬手清除、保存后刷新恢复及竖屏暂停。浏览器控制台错误和失败请求均为零。已目视检查真实公网战斗截图。
- 当次环境记录的桌面/触控模拟首次启动分别约 29.4 / 11.3 秒，受网络和缓存状态影响，不是普遍性能承诺，也不是手机真机测量。
- `/` 与 `/tusharedata/` 仍为 200；`/test` 与 `/test/anything` 仍为 404，未被加入游戏隔离头。主 Caddyfile 相比部署前仅增加一行游戏配置导入。
- 当前更新的备份：`/srv/touhou-survivor/backups/alpha-0.0.9-f605aa0-20260906T164408Z/`。若要完全撤销首次游戏路由，部署前主配置位于 `/srv/touhou-survivor/backups/alpha-0.0.9-303fceb-20260906T164102Z/Caddyfile`；恢复前仍需检查其他站点是否有后续改动。
- 本机详细结果：`artifacts/deployment/alpha-0.0.9-f605aa0-20260906T164408Z/public-verification/report.json`，同目录有截图。上线前本地六项部署保护/回滚回归也通过。
- 原 `alpha-0.0.9` EXE SHA-256 仍为 `E21BFEDEC06B3DD1F2E730A97776C5B14BF609F7361FC73D4854509C42043AE8`；本次没有发布新的 Windows 成品。

## 首次加载计量（待发布）

- 加载卡片展示核心资源总量、已接收量、百分比、最近约三秒的接收速度与预计剩余下载时间，约每 250 ms 刷新。总量指 WASM 引擎与 PCK 游戏资源包，不包含少量页面、脚本和协议开销；MB/KB 使用十进制单位。
- 部署工具生成 gzip 旁路文件后，把实际压缩正文长度及不可变 URL 写入入口清单。加载器接收显式 `.wasm.gz` / `.pck.gz` 原始正文并计数，再流式解压给引擎；不能直接拿 Godot 的解压后字节计数冒充网络流量。原始导出文件和引擎 JS 不修改。
- `.gz` 响应必须无 `Content-Encoding`，否则浏览器会提前解压、导致计量失真；加载器会明确失败，公网验证工具也检查该条件及 `Content-Length` 与清单一致。已对现有线上 WASM 旁路文件做只读 HEAD 检查，符合要求，但这不等于新版已上线。
- 没有 `DecompressionStream` 时回退到原资源读取，界面明确标注解压后大小不代表下载流量。缓存命中时速度是缓存读取速度；缓存能否复用取决于浏览器保留情况及刷新行为，不保证每次进入都零下载。
- 八秒没有新数据时显示等待提示，速度归零、进度不假增长；开始收数据后恢复。完整接收后才显示 100%，随后单独提示解压、编译和初始化；初期或无速度时不提供虚假剩余时间。连接或脚本失败可重新加载，不支持跨页面断点续传。
- 手机竖屏下载时不遮住加载统计，真正启动游戏后才出现横屏提示。触控模拟不代表手机真机验收。
- 验证入口：`node --test tools/platform/loader.test.cjs`、`python -B tools/platform/test_deployment.py`、`tools/platform/verify_loading.cmd`（真实导出资源、节流、停顿、中断重试、脚本错误、缓存再访问与响应式截图）、`tools/platform/verify_web.cmd`（原资源路径和完整游戏回归）。先运行 `build_web.cmd` 创建对应源码的隔离测试产物。
- 本次只做日常开发与本地 Web 验证，不升版本号、不导出新 EXE、不更新线上；正式发布时按双端交付约定执行。

## 今后的“发布”定义

正式发布必须同时交付同一版本、同一游戏源码修订的：

1. 自带依赖的 Windows EXE，保留旧文件，遵循既有版本命名及更新日志规则。
2. 更新后的网页部署，并给出公网检查结果及回滚信息。

仅 EXE 成功或仅网页成功都属于部分完成。本次是首次网页试部署，不自动提升版本号，也不重新导出覆盖已有 `alpha-0.0.9` EXE。用户已授权本轮 GitHub 推送和服务器 clone/pull；日常开发提交不自动等于发布。
