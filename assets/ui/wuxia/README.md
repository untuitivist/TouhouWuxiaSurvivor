# 武侠 UI 像素资产

本目录保存游戏运行时直接引用的像素 UI 资产。所有图片由项目内 RGB 像素绘制器确定性生成，
不依赖外部图片服务；相同代码与参数会得到相同 PNG，便于后续统一调色和批量迭代。

## 资产清单

- `paper_fiber.png`：宣纸纤维底纹
- `scroll_panel.png`：九宫格卷轴面板边框
- `preview_frame.png`：图鉴动态窗九宫格边框
- `cloud_divider.png`：祥云分隔纹
- `seal_stamp.png`：朱砂印章
- `ink_mountains.png`：主菜单墨山、月轮与鸟居背景
- `enemy_preview_sheet.png`：十二种敌人原型的双帧移动图
- `daily_actor_sheet.png`：四种场景日常人物的双帧行走图

## 重新生成

```bat
dotnet run --project tools\ui_asset_generator\ui_asset_generator.csproj --configuration Release -- assets\ui\wuxia
```

生成后首次打开工程时，Godot 会自动导入 PNG。命令行环境可执行：

```bat
D:\_soft\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe --headless --editor --path . --quit
```

所有纹理继承项目的最近邻过滤和禁用 mipmap 规则，不应在单独节点改为线性过滤。
