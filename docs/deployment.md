# 域名部署与双端发布

## 已确认的部署目标

- 入口：`https://allinagent.top/TouhouSurvivor/`。
- 服务器：`ubuntu@170.106.119.27`，SSH 私钥由用户在本机提供，不入库、不上传。
- Web 服务：现有 Caddy；根站点、`/tusharedata/` 和现有拒绝路由必须保持原样。
- GitHub：`https://github.com/untuitivist/TouhouWuxiaSurvivor.git`，分支 `main`。
- 服务器源码目录：`/home/ubuntu/touhou-survivor`；首次 clone，后续 `git pull --ff-only origin main`。检测到工作区修改、错误远端、错误分支或提交不一致时停止，不强制覆盖。

## 为什么 Git 与游戏产物分开

同一仓库维护 C# 源码、资源和部署脚本。Windows 构建机完成实验 Web 编译及验证，服务器通过 Git 获取同一提交的代码，通过 SCP 接收已验证的静态产物。服务器不安装另一套 Godot/.NET 构建环境，也不把近 100 MB 的生成文件或 SDK 缓存写入 Git 历史。

`git pull` 负责源码和部署脚本更新，并不把 C# 自动变成浏览器能运行的文件。一次完整网页更新仍需要“构建验证 → 提交与推送 → 服务器拉取 → 上传并激活产物 → 公网验证”。

## 执行更新

在已配置的本机仓库根目录执行：

```bat
build_web.cmd
tools\platform\verify_web.cmd
git push origin main
deploy_web.cmd -KeyPath "<private-key-path>"
tools\platform\verify_deployment.cmd
```

游戏源文件必须先完成日常提交。若在构建后修改玩法、资源、导出配置或游戏更新日志，必须重新构建验证；部署工具校验源文件与产物 SHA-256，不会悄悄部署未经测试的内容。部署脚本本身也必须提交并推送，让服务器运行精确的同一提交。

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

## 今后的“发布”定义

正式发布必须同时交付同一版本、同一游戏源码修订的：

1. 自带依赖的 Windows EXE，保留旧文件，遵循既有版本命名及更新日志规则。
2. 更新后的网页部署，并给出公网检查结果及回滚信息。

仅 EXE 成功或仅网页成功都属于部分完成。本次是首次网页试部署，不自动提升版本号，也不重新导出覆盖已有 `alpha-0.0.9` EXE。用户已授权本轮 GitHub 推送和服务器 clone/pull；日常开发提交不自动等于发布。
