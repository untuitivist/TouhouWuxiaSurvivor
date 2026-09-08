# Moonlit Shrine Title

## Art Direction

用户于 2026-09-08 对照 artifacts/scenery-art/title-comparison.png，指定采用 BEFORE 的彩色像素夜景方向，而不是黑白原作插画。

- 月亮、朱红鸟居、樱树、常绿树、暖灯与石阶延续旧构图；左侧给原菜单留空，右侧人物位置不变。
- 新画布为 640×360，10 个独立图层。PNG 全不透明，最近邻显示；不在运行时重新绘制背景，也不额外染成蓝灰色。
- 标题源图是本作新绘制内容，放在 art/title，而不是标为原作素材。旧的 title_shrine.png 与局内原作场景均保留。

## Authoring

本机工具：D:/thesteam/steamapps/common/Aseprite/Aseprite.exe，实际版本 1.3.18.3-x64。

初稿通过 Aseprite 内置 Lua 的 Sprite、Image 和 cel 接口直接绘制并保存，不使用其他绘图工具、AI 生图或外部栅格绘制器。脚本定义画笔图形和构图，再由 Aseprite 自身创建全部像素与分层；不是手工鼠标逐像素绘制。

- 可编辑主文件：art/title/moonlit_shrine.aseprite
- 游戏导出文件：assets/ui/title/moonlit_shrine.png
- 初稿绘制脚本：tools/aseprite/draw_title.lua 与 pixel_tools.lua

日常修改请直接在 Aseprite 编辑主文件，保存后运行 tools/aseprite/export_title.cmd。可通过 ASEPRITE_EXE 指定其他安装位置。该命令只导出现有源文件，不会重新绘制或覆盖图层结构。

不要把 draw_title.lua 当作日常导出工具：它会重建初稿，覆盖同名输出。若要试新构图，必须用 --script-param source=... 和 --script-param texture=... 指向新的文件，保留人工修订过的版本。

## Validation

2026-09-08 验收：Aseprite 重新导出与运行 PNG 逐像素相同，10 层、640×360、104 个最终颜色；原生构建零警告，38 项核心与完整 UI/设置烟测通过。Web 修复旧 UI 排除规则后，桌面 1280×720、小屏 640×360、触屏模拟 844×390 DPR3 的背景和开始导航均通过，已查看实际截图；没有进行手机真机或正式 EXE 发布验收。

本地证据：artifacts/aseprite-title-verification.json、artifacts/aseprite-title-native.png、artifacts/aseprite-title-web.log，以及 artifacts/web-builds/20260908-202434-824/aseprite-title-verification。此 Web 开发快照包含工作区中并行的语言改动，不是干净源码的正式发布包。

tools/aseprite/verify_title.cjs 使用本机 Aseprite 重新导出副本，检查格式、画布、10 层结构、全不透明像素和 PNG 逐像素一致性。Node 在此只读取和比较图片，不绘制美术。

tools/platform/verify_title.cjs 检查无跨源隔离、无共享内存 Web 的桌面、小屏和触屏模拟标题布局，以及开始按钮到角色选择页面的导航。截图人工审阅不等于所有真机验收。

Windows 与 Web 均加载 assets/ui/title/moonlit_shrine.png。Web 暂存流程显式复制该资源目录；三个导出预设只排除旧 assets/ui/wuxia，而不排除整个 assets/ui；.aseprite 源文件和初稿绘制脚本不进入 Web 游戏包。
