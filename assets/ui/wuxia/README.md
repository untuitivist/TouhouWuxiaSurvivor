# 武侠 UI 像素资产

本目录保存游戏运行时直接引用的东方武侠像素 UI 资产。所有图片由项目内 RGB 像素绘制器
确定性生成，不依赖外部图片服务；相同代码与参数会得到相同 PNG，便于统一调色和批量迭代。

## 资产清单

- `paper_fiber.png`：宣纸纤维底纹
- `scroll_panel.png`：八像素切片的漆木外层面板
- `danger_panel.png`：八像素切片的朱砂危险与失败面板
- `inset_panel.png`：六像素切片的正文、列表和详情内嵌面
- `preview_frame.png`：十像素切片的图鉴动态陈列框
- `hud_panel.png`：六像素切片的紧凑状态栏外框
- `map_frame.png`：十像素切片、透明中心的行旅地图包边
- `button_*.png`：按钮普通、悬停、按下、禁用与焦点五态
- `field_*.png`：输入框普通与聚焦两态
- `tab_*.png`：页签普通、悬停与选中三态
- `list_*.png`：条目列表底面与当前选中签
- `check_*.png`：启用、禁用条件下的勾选与未勾选四态
- `option_arrow.png`、`slider_grabber*.png`：选项箭头与滑杆结绳抓手
- `separator_*.png`：不含可拖动语义的横竖编织分隔纹
- `scroll_*.png`：竹节式滚动槽与抓手
- `progress_*.png`：生命、经验、阶段与亲和的语义进度纹样
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
