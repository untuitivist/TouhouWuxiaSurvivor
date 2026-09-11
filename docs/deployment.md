# 域名部署与双端发布

## Web 首载包体维护（2026-09-11）

- 本次是 alpha-0.1.8 既有产物的交付瘦身，不是新游戏版本，也不重新导出 Windows。候选从已验证的 `20260911-212025-699` Web 产物派生，玩法源仍是 `7aca71e`；原始导出与正式部署完整保留。上线回执另记，不把本节的本地候选当成上线证据。
- 根因之一：固定 C# Web 导出器把 Mono 发布目录中的 26 个 `.a` 静态库也打进 PCK，而这些库已经在模板构建时链接进 WASM，浏览器运行时加载的是 DLL。`optimize_web_payload.py` 只排除该目录直属且具有真实 archive 文件头的 `.a`，其他位置和格式不推测删除；保留全部 ICU 数据、DLL、游戏资源和音频。
- 安全检查：仅接受独立、未加密的 Godot PCK v3；校验路径、条目边界、不重叠、MD5、发布清单 SHA-512。只重写被排除库对应的发布清单项，逐字节比较其余 289 个运行内容，更新入口的精确 PCK 大小；WASM/JS/图标原字节不变。输出及报告必须是新位置，禁止覆盖输入或已有构建。
- 下载正文：PCK 原始大小 45,600,816 → 18,728,740 字节，gzip 20,683,976 → 9,751,200 字节；WASM gzip 11,859,472 字节不变。核心总量 32,543,448 → 21,610,672 字节，减少 10,932,776 字节（33.6%），不计页面/脚本/协议。
- 后续 `build_web.ps1` 自动保留 `unoptimized/TouhouSurvivor` 导出，再生成最终 `site/TouhouSurvivor` 和 `payload-optimization.json`；任何格式或完整性检查失败就阻止构建完成。正式双端发布门禁没有放宽。
- 线路证据：明确无代理的 HTTP/1.1 Range 在 35 秒接收 589,824 字节（约 16.9 KB/s）；源站本地 HTTP/2 同资源全量仅 0.062 秒。独立浏览器 Range 收到约 0.95 MB/25 秒，强制 QUIC 失败。因此减包不能冒充线路提速；不自动购买 CDN、改 DNS/其他站点路由或全局内核设置。证据位于 `artifacts/download-delivery-20260911/`。

## alpha-0.1.8 双端发布完成（2026-09-11）

- Windows 与 Web 来自同一干净提交 `7aca71e68f28f3a1267e81d69a46d3e16e7e58cf`，已推送并由服务器 `git pull --ff-only` 同步。90 个共用 C# 文件在维护目录、Windows stage 和 Web stage 逐字节一致；后续回执提交不改变产物来源。
- Windows：`release/TouhouWuxiaSurvivor_alpha-0.1.8.exe`，195,187,584 字节，文件版本 0.1.8.0；SHA-256 `C33EDED8A071AFFDDDFC87EACB8CCE213DC3151701F7B40A51AE8AAC9E0676CA`。自带 .NET/PCK，隔离目录只有 EXE，31 项独立检查通过。已查看实际魔理沙彩虹魔炮画面。
- Web 构建 `20260911-212025-699`，启用 `alpha-0.1.8-7aca71e-20260911T133928Z`。修正兼容模板正式构建误开 Mono 调试的问题，正式运行启用解释器优化；仍为 WebGL2 单线程、无需共享内存，不启用 WebGPU。模板 SHA-256 `CC6E684E5B94B1A6CB6E0175D5C31FC7CADC93232DBCC24E5926C033B1725F88`。
- 玩法：逐颗四方星流、独立随机质量、多对多引力、药菇续航、最近敌人魔炮与星光共鸣；不设总质量池或保底重星。原作 14 色星星、沿炮身流动的多色魔炮，暂停与减少动态效果行为保留。灵梦、原有操作、选人立绘和历史记录不变。
- 最终门禁：60/60 核心、21/21 平台单测、20 次原始下载、四种压缩/隔离组合共 20 项完整流程、双方普通/故障注入批次颜色、语言持久化、立绘、成长、图鉴、小地图、六类加载异常及 DPR 1/3 均通过。字体覆盖 1275 字符。
- 双角色 × 双语 × 三档战斗全部通过既有门槛；最高档保持 320 敌人及约 1590 弹幕。灵梦中/英文平均 FPS 60.07/60.00、最低均 59；魔理沙两语言平均/最低均 60，核心 P95 5.9/5.4 ms。是桌面 Edge/NVIDIA WebGL2 无隔离实测，不能当作手机真机成绩。
- 同投入三种子持续单体 DPS 门禁通过，0/4/8/12 点魔理沙与灵梦约相差 0%–4%；其他空间场景仍各有优势。现有不加血自动路线：灵梦三种子胜，魔理沙 42 胜、260906 和 781 败；这是路线观察而非“全种子通关”或真实胜率证明。Web 固定 42 两角色旅程均通过。
- 公网直连检查保留失败：180 秒时仅下载约 4.02/32.54 MB，瞬时约 10.9 KB/s。没有放宽战斗门槛或宣称直连速度已解决。随后浏览器仅使用本机现有 HTTP 代理，桌面/触屏/无隔离触屏入口通过，首载 17.734/52.506/19.265 秒；请求错误为空，启动等待诊断保留。服务器路由、下载计量与响应头仍用直连检查通过。
- 计量核心下载 32,543,448 字节（约 32.54 MB），不含页面/脚本/协议；入口 SHA-256 `8dd8a26c28e025aa07fa1299c2c04bec1deef1cc484bd03dd18b97d46a478526`。根站点、`/tusharedata/` 和原拒绝路由保留，Caddy 主配置哈希未变。
- 回滚目录：`/srv/touhou-survivor/backups/alpha-0.1.8-7aca71e-20260911T133928Z/`。31 份历史发行文件哈希未变；旧未发布 0.1.8 候选和失败模板/验证全部归档。新增版本日志快照 `release/CHANGELOG_alpha-0.1.8.md` 及 `release/RECEIPT_alpha-0.1.8.json`，总回执 `artifacts/release-receipts/alpha-0.1.8.json`。
- 公网成功与直连超时分别位于部署目录下 `public-verification/report.json`、`public-verification-direct-180s/report.json`；工作日志 `artifacts/marisa-gravity-20260911/`。真实手机/平板性能和跨境直连下载速度仍未解决或验收。

## alpha-0.1.7 双端发布完成（2026-09-09 UTC）

- Windows 与 Web 游戏产物来自同一干净源码提交 `82563e6535f42e35cfe67318d16a116b7b04dee7`，已推送；服务器经 `git pull --ff-only` 同步后部署。后续回执文档提交不改变已发布产物来源。
- 本版基于回退后的正式 alpha-0.1.6，仅增加灵梦／魔理沙临时选人 AI 立绘、相关布局验收与更新记录字体；局内美术、视角、玩法、数值和操作不变。一般素材方向仍为 Aseprite＋原作素材；AI 原图和分层源稿保留，裁切不冒充手绘。
- Windows：`release/TouhouWuxiaSurvivor_alpha-0.1.7.exe`，195,109,656 字节，文件版本 0.1.7.0；SHA-256 `B2B2F21A4A191E47A2A882CAE9ECBF6E68377853E3EB6007341042586BD8D9C1`。内嵌 PCK 与 .NET，隔离目录仅 EXE、排除全局 .NET 的 28 项检查通过，包含选人中英文截图和两角色批次颜色回归。
- Web：本地构建 `20260910-003000-092`，实际启用 `alpha-0.1.7-82563e6-20260909T164729Z`，继续使用无需共享内存的 C# Web。81 个 C# 文件及两张立绘 PNG 在维护源码、Windows stage 和 Web stage 间逐字节一致。本地构建目录按北京时间命名，部署 ID 和本节日期为 UTC。
- 源码构建零警告／错误，40/40 核心、六组确定性旅程、完整 UI／设置／图鉴／选人、19/19 JavaScript 与 7/7 部署安全测试通过；字体覆盖 1250 字符。预设损坏存档触发的警告属于保留原文件测试。
- Web 20 次原始加载、有／无隔离 × 原始／gzip 四组共 20 项游戏流程、两角色普通与注入颜色回归、中英文三档动态战斗门槛、三项语言、四组桌面／触屏选人、六种加载情境和 DPR 1/3 检查通过。最高档桌面平均中文 57.81、英文 59.30 FPS，最低采样 50／54，模拟步长 p95 均 9.4ms；这不是手机真机性能承诺。
- 公网普通桌面、触控及移除页面隔离头的触控入口均通过，首载分别约 13.179／12.149／12.258 秒；首载耗时会受网络和缓存影响。补验真实操作中英文切换与双向刷新保存、两位角色选择和进入战斗、返回及触屏按钮；运行错误和失败请求为空。已查看普通桌面实际战斗和无隔离触屏英文选人截图。物理手机／平板尚未验收。
- 核心计量下载共 32,502,943 字节（约 32.50 MB）：PCK gzip 20,643,473、WASM gzip 11,859,470；不含页面、脚本和协议开销。入口 SHA-256 `2744f04b22d499302c03382c29498ce3dba3f97f2a59f5e035d3eab919b0abc5` 已通过公网与服务器元数据交叉核对；既有根站点和路由未改变。
- 回滚备份：`/srv/touhou-survivor/backups/alpha-0.1.7-82563e6-20260909T164729Z/`。全部 28 份旧发行文件和四份 AI 原图／源稿共 32 项哈希未变；历史版本日志段落未改写，新增完整 `release/CHANGELOG_alpha-0.1.7.md`、`release/RECEIPT_alpha-0.1.7.json` 和 `artifacts/release-receipts/alpha-0.1.7.json`。
- 失败证据保留：第一次本地批次监听被系统分配到浏览器禁止的 4045 端口，重新分配端口后通过；第一次部署因 Git PATH 使 MSYS tar 误解 Windows 盘符而在本地打包阶段中止，服务器未切换。最终仅为 SSH／SCP 指定 Git 版本、tar 指定 Windows 原生版本；未改发布源码、未放宽验收断言。日志在 `artifacts/release-017/`。
- 公网主报告：`artifacts/deployment/alpha-0.1.7-82563e6-20260909T164729Z/public-verification/report.json`；选人／语言补验：同发布目录 `public-selection-language/2026-09-09T16-49-44-321Z/report.json`。其他证据与校验值索引见版本回执。
- 下一版再试 WebGPU：先验证引擎／C# Web 导出支持、实际性能和旧浏览器回退，继续保持同一游戏项目；本次没有启用 WebGPU，也不承诺更换 API 自动提速。已打开旧版网页需刷新或重新打开以加载新入口。

## alpha-0.1.6 双端发布完成（2026-09-08 UTC）

- Windows 与 Web 均来自干净源码 c5bf1684b88d3b825556919fa7ae9eba6d255059，已推送；服务器通过 git pull --ff-only 同步。后续回执文档提交不改变游戏产物来源。
- Windows：release/TouhouWuxiaSurvivor_alpha-0.1.6.exe，194,427,544 字节，文件版本 0.1.6.0；SHA-256 003B15729C019DAAA3B8EECFE063E3119D29A1114F083BD662E8684F48A093CA。内嵌 PCK 和 .NET，26 项独立单文件检查通过，旧成品和未通过发布门槛的候选均保留。
- Web 构建 20260909-024731-641，活动版本 alpha-0.1.6-c5bf168-20260908T190557Z。复用 ECS/OOP 边界，优化扫掠候选、寻敌、实体回收及批次；正式纳入中英文设置，刷新后保留选择。
- 正式无隔离桌面参考压力：40/200 与 180/600 两档均平均 60 FPS；320/1600 档中文 58.09、英文 58.41 FPS，最低采样 49／52，逻辑 p95 为 9.9／9.2ms。初始另有 400 掉落物；不减少预算，不以模拟 ticks/s 冒充渲染 FPS，不承诺手机锁 60。
- 核心 40/40、语言 860/860、Web 中英文三档门槛、20 次原始加载、四组隔离／压缩组合、双角色普通及异常颜色批次、六类加载场景、图鉴四组、成长与标题各三组均通过。
- 公网普通桌面、触控、移除隔离头的触控均通过，运行错误和失败请求为空；额外真实操作中英文选择并双向刷新保存通过。设置页右上方可选语言，已打开旧页面需要刷新或重新打开以加载新入口。
- 核心计量下载 31,826,997 字节：PCK gzip 19,967,527、WASM gzip 11,859,470；不含页面、脚本及协议开销。既有站点路由未改变，历史更新日志保留。
- 回滚目录：/srv/touhou-survivor/backups/alpha-0.1.6-c5bf168-20260908T190557Z。回执 artifacts/release-receipts/alpha-0.1.6.json；公网证据 artifacts/deployment/alpha-0.1.6-c5bf168-20260908T190557Z/public-verification/report.json 与 public-language/report.json。真实移动设备尚未验收。

## alpha-0.1.5 双端发布完成（2026-09-08）

- Windows 与 Web 由同一干净提交 48f87757f67b7157c04e67d6bf02066cae1c1b1f 构建，源码已推送，服务器通过 git pull --ff-only 同步。使用独立 detached 发布工作树，未混入主工作区尚未提交的中英文切换/文案修改；后续文档回执提交不改变游戏产物来源。
- Windows：release/TouhouWuxiaSurvivor_alpha-0.1.5.exe，194,363,656 字节，文件版本 0.1.5.0；SHA-256 761CC4D47F72BA42EA61C3914D45529DC24E7854CA1AC0BCCAE3A00EC974E591。PCK 与 .NET 运行时内嵌，隔离目录仅一个 EXE，24 项独立检查通过。旧 alpha-0.1.4 及全部历史成品保留。
- 发布灵梦基础御札→能力解锁→兼修行为的成长样板及首轮平衡；采用原作素材 + Aseprite，美化标题、UI、石板路、场景装饰与图标，保留分层源文件、触控和旧操作。
- Web 构建 20260908-232506-259，活动版本 alpha-0.1.5-48f8775-20260908T154824Z，无需跨源隔离/SharedArrayBuffer。20 次原始加载、四组原始/压缩及有/无隔离组合共 20 项游戏流程、两角色普通/异常颜色批次、六类加载故障场景通过；图鉴四种布局各 22 项、成长与标题各三组布局通过。
- 核心 38/38 与原生 UI/设置/图鉴回归通过。公网桌面、触控、移除隔离头的触控三组通过，含设置持久化；gzip 资源计量、跳转和服务器其他路由保持正确。真实移动设备性能尚未验收，公网加载时间受网络与缓存条件影响。
- 回滚目录：/srv/touhou-survivor/backups/alpha-0.1.5-48f8775-20260908T154824Z。发布回执：artifacts/release-receipts/alpha-0.1.5.json；完整构建与验收日志保存在 artifacts/release-worktrees/alpha-0.1.5/artifacts/，未删除历史证据。

## alpha-0.1.4 双端发布完成（2026-09-08）

- 两端游戏产物来自同一干净提交 `99e0baef0d5d85820d8908227ef9fea1f8255f88`，已推送并由服务器 `git pull --ff-only` 同步。发布六类 22 项卡片图鉴、触控小地图避让、原作树冠/鸟居/神社标题；不改变玩法、角色能力或战斗数值。后续文档提交只补充交付记录，不改变游戏产物来源。
- Windows：`release/TouhouWuxiaSurvivor_alpha-0.1.4.exe`，194,332,544 字节，文件版本 0.1.4.0；SHA-256 `9BBA949F4D10A94D27FA72F6276A4AE754FBB8A42DD2BF82251A093B693E501F`。自带依赖、内嵌 PCK，隔离目录前后仅 EXE，24 项独立检查通过（含图鉴列表/详情实际渲染与两角色颜色回归）。
- Web 本地构建 `20260908-182518-023`，实际启用 `alpha-0.1.4-99e0bae-20260908T104638Z`，继续使用无需共享内存的单线程 C# 构建。20 次原始加载、四组隔离/压缩组合共 20 项游戏流程、普通与异常批次颜色、六项加载场景及 DPR 1/3 检查通过。图鉴在四组布局各验证 22 项条目，小地图四组、场景五组、角色特效五组通过。模拟浏览器表现不代表手机真机帧率。
- 源码 29/29 核心、六组固定种子旅程、完整 UI/设置/存档/F3/图鉴/小地图回归、19 项 JavaScript 与七项部署安全测试通过；7/7 特效与 4/4 场景原图来源校验通过，字体覆盖 1204 字符。
- 公网普通桌面、触控、无隔离触控入口通过：首载分别约 37.806 / 44.581 / 10.385 秒。启动观察预算为 600 秒，保留引擎已知等待诊断，运行错误和失败请求为空；保存刷新、多指控制、竖屏暂停及既有站点路由通过。另从公网在桌面与触屏模拟中逐项打开 22 个图鉴详情，验证分类/页码/阅读/暂停，并截图确认小地图不再被暂停按钮盖住；补验通过客户端移除隔离头，不修改服务器响应。
- 核心资源下载总计 31,766,968 字节（约 31.77 MB）：PCK gzip 19,907,498、WASM gzip 11,859,470，不含页面、脚本及协议开销。VPN 条件性提示已进入同源构建，无需额外入口覆盖；不保证网络加速，也未完成手机真机兼容和长局性能验收。
- 备份 `/srv/touhou-survivor/backups/alpha-0.1.4-99e0bae-20260908T104638Z/`，入口 SHA-256 `8752c28627b2e15977c71e087f762ed6f25fa1d37e22cd5006c62d52ee7ea549`。21 个既有 EXE/版本日志逐一比对哈希未变；新增 `release/CHANGELOG_alpha-0.1.4.md` 和 `release/RECEIPT_alpha-0.1.4.json`。
- 公网主报告：`artifacts/deployment/alpha-0.1.4-99e0bae-20260908T104638Z/public-verification/report.json`；图鉴补验位于同目录 `public-journal-verification/2026-09-08T10-52-10-245Z/report.json`。完整本地门禁日志 `artifacts/release-014-web-gates.log`，Windows 报告 `artifacts/alpha-0.1.4-export-validation/report.json`。

## alpha-0.1.3 双端发布完成（2026-09-08）

- Windows 与 Web 游戏产物由同一干净提交 `4d6fb6bfaae1b80a2c420958b5f867cd84501acc` 构建，源码已推送，服务器通过 `git pull --ff-only` 同步。发布原作魔炮、蓄力星环、封魔阵与古印特效，玩法、操作和伤害不变。
- Windows：`release/TouhouWuxiaSurvivor_alpha-0.1.3.exe`，193,260,064 字节，文件版本 0.1.3.0；SHA-256：`F6490F55D10174571FB8F426EC451F0963C61D31D5FE2C851C9760C29C780AA2`。内嵌 .NET 8.0.6，隔离目录前后仅 EXE，22 项独立检查通过；已查看实际魔炮与更新记录截图。
- Web：本地构建 `20260908-153552-526`，实际启用 `alpha-0.1.3-4d6fb6b-20260908T075754Z`；保留单线程 C# Web。20 次原始下载、五个角色美术场景、普通与异常颜色回归、有／无隔离 × 原始／gzip 四组共 20 项游戏流程、六项加载场景及匹配本构建的 DPR 1/3 检查通过。最初 DPR 命令选中了旧指针，显式 `--compatible` 复验后仅采用新构建报告。
- 源码核心 29/29、六组固定种子旅程、UI／设置／存档／F3、1183 字字体覆盖、7/7 原图裁切、18 项 JavaScript 与七项部署安全测试通过。后续公网启动预算新增一项单测，相关七项测试通过，不改变游戏产物。
- 公网正常桌面、触控、无隔离触控最终通过，实际无隔离入口没有 SharedArrayBuffer；保存刷新、多指操作、竖屏暂停及旧路由正常，运行错误与失败请求为空。报告：`artifacts/deployment/alpha-0.1.3-4d6fb6b-20260908T075754Z/public-verification/report.json`。
- 首载速度存在明显波动：前两轮在 180 秒预算内分别于触控 60.8% 和桌面 60.1% 下载阶段超时，原始日志和截图分别保存在同目录的 `public-verification-first-timeout`、`public-verification-second-timeout`。显式使用 600 秒启动观察预算后通过，桌面／触控／无隔离触控首次启动分别为 283.578／10.286／286.122 秒。没有跳过错误断言，不将“最终能进入”写成“加载很快”。
- 核心资源实际下载 30,711,684 字节（约 30.71 MB）：PCK gzip 18,852,214 字节、WASM gzip 11,859,470 字节，不含页面、脚本和协议开销。完整版本日志保存在 `release/CHANGELOG_alpha-0.1.3.md`；全部旧版及回滚备份 `/srv/touhou-survivor/backups/alpha-0.1.3-4d6fb6b-20260908T075754Z/` 保留。
- 用户在验收期间要求增加 VPN 提示。网页入口随后按 `e4191f9853cc79a8b045ff180b0a5a15f8e2afb6` 的 `platform/web/shell.html` 仅追加提示节点与样式；不覆盖不可变资源，不修改原 EXE，不改变游戏版本。active.json 单独记录 `landingPageOverride`，游戏 sourceCommit 仍为原构建提交，不能把入口补丁说成重新构建两端。
- 入口备份：`/srv/touhou-survivor/backups/network-hint-e4191f9-20260908T082946Z/`；入口 SHA-256：`900bc37959f90f5b250530f978a838e78148a0d99d60cb2d9a65f1f4b70063f7`。公网 HTML 哈希、全部脚本未变及电脑／手机横竖屏提示可见性通过，第一次入口截图请求连接中断的记录保留。文案为条件性的“可能提升”，不保证 VPN 加速；此项核验仅覆盖入口，不冒充再次完整游戏回归。
- 收据及核验：`artifacts/deployment/network-hint-receipt.json`、`network-hint-verification.json`、`active-013-with-network-hint.json`。网页文案补充后的日志另存 `release/CHANGELOG_alpha-0.1.3_web-notice.md`，原 EXE 内嵌及成品旁初版日志保留。阵纹密度、程序场景美术、手机真机性能与真实设备发黑触发条件仍待改进或复测。

## alpha-0.1.2 双端发布完成（2026-09-07）

- 两端由同一干净提交 `cf5e5c59a7c8aa166734a271b3404ecd9097dd85` 构建，源码已推送，服务器通过 `git pull --ff-only` 同步。后续文档提交只记录结果，不改变交付产物。
- Windows：`release/TouhouWuxiaSurvivor_alpha-0.1.2.exe`，193,128,784 字节；文件版本 0.1.2.0；SHA-256：`FF7AC4DE1190BB5AC99A08E7E0724D9EDDBBA8874FF86C1F60828B4A7A946BF0`。内嵌 Microsoft.NETCore.App 8.0.6，22 项独立 EXE 检查通过；隔离目录检查前后均只有 EXE。每位角色包含 24 项交错颜色、72 项精灵及 12 处完整战斗像素对照。
- Web：实际启用 `alpha-0.1.2-cf5e5c5-20260907T102610Z`（目录时间为 UTC），本机构建 `20260907-180715-471`。服务器 active.json 已复核版本、源码、threadSupport=false 和配套 Windows 哈希；确认记录 `artifacts/deployment/active-012-confirmed.json`。
- 本地：20 次原始下载、普通与异常默认色注入两组双角色像素回归、有／无隔离 × 原始／gzip 四组共 20 项游戏流程、六项加载场景及 DPR 1/3 检查通过。每组像素回归共 216 项且无超容差差异，异常注入观察到的 3380 次实例绘制均有显式颜色输入。源码 29 项核心、UI、18 项 JavaScript、七项部署安全测试及脚本语法检查通过。
- 公网：正常桌面、触控及无隔离触控三入口通过，后者确实没有 SharedArrayBuffer；多指移动／慢移／闪身、刷新保存与竖屏暂停正常，运行错误和失败请求为空。根站点与既有路由状态保留。核心下载 30,589,176 字节（约 30.59 MB），由 PCK gzip 18,729,706 字节与 WASM gzip 11,859,470 字节组成，不含页面、脚本和协议开销。
- 回滚备份：`/srv/touhou-survivor/backups/alpha-0.1.2-cf5e5c5-20260907T102610Z/`。完整版本日志同时保存在 `release/CHANGELOG_alpha-0.1.2.md`，公网报告在 `artifacts/deployment/alpha-0.1.2-cf5e5c5-20260907T102610Z/public-verification/report.json`。已查看实际 EXE 更新记录、战斗截图及线上版本标题。
- 第一次 Web 编辑器导入出现 0xc0000374 异常，失败目录和日志保留，未作为发布产物；重新隔离构建的最终产物通过完整门禁。之前已验收的候选 EXE 连同报告保留在 `artifacts/release-candidates/alpha-0.1.2-5707ddf/`。所有旧正式版本保留。
- 本版去除了默认顶点色依赖，但异常注入不代表已自然复现用户设备故障；真机发黑触发条件及手机帧率仍待复测，不作彻底修复或全移动设备兼容承诺。

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
