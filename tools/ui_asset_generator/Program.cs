using TouhouWuxiaSurvivor.Tools.TileGenerator;
using TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: ui_asset_generator <output-directory>");
    return 2;
}

string outputRoot = Path.GetFullPath(args[0]);
var chrome = new UiChromePainter();
var backdrop = new UiBackdropPainter();
var frames = new UiFramePainter();
var controls = new UiControlPainter();
var icons = new UiIconPainter();
var progress = new UiProgressPainter();
var sprites = new PreviewSpritePainter();
var assets = new Dictionary<string, PixelCanvas>
{
    ["paper_fiber.png"] = chrome.PaintPaperFiber(),
    ["scroll_panel.png"] = frames.PaintLacquerPanel(),
    ["danger_panel.png"] = frames.PaintDangerPanel(),
    ["inset_panel.png"] = frames.PaintInsetPanel(),
    ["preview_frame.png"] = frames.PaintPreviewFrame(),
    ["hud_panel.png"] = frames.PaintHudPanel(),
    ["map_frame.png"] = frames.PaintMapFrame(),
    ["button_normal.png"] = controls.PaintButton(UiControlState.Normal),
    ["button_hover.png"] = controls.PaintButton(UiControlState.Hover),
    ["button_pressed.png"] = controls.PaintButton(UiControlState.Pressed),
    ["button_disabled.png"] = controls.PaintButton(UiControlState.Disabled),
    ["button_focus.png"] = controls.PaintButtonFocus(),
    ["field_normal.png"] = controls.PaintField(false),
    ["field_focus.png"] = controls.PaintField(true),
    ["tab_normal.png"] = controls.PaintTab(UiControlState.Normal),
    ["tab_hover.png"] = controls.PaintTab(UiControlState.Hover),
    ["tab_selected.png"] = controls.PaintTab(UiControlState.Pressed),
    ["list_panel.png"] = controls.PaintListPanel(),
    ["list_selected.png"] = controls.PaintListSelection(),
    ["check_off.png"] = controls.PaintCheck(false, false),
    ["check_on.png"] = controls.PaintCheck(true, false),
    ["check_off_disabled.png"] = controls.PaintCheck(false, true),
    ["check_on_disabled.png"] = controls.PaintCheck(true, true),
    ["option_arrow.png"] = icons.PaintOptionArrow(),
    ["slider_grabber.png"] = icons.PaintSliderGrabber(false),
    ["slider_grabber_hover.png"] = icons.PaintSliderGrabber(true),
    ["separator_horizontal.png"] = controls.PaintHorizontalSeparator(),
    ["separator_vertical.png"] = controls.PaintVerticalSeparator(),
    ["scroll_track.png"] = controls.PaintScrollTrack(),
    ["scroll_grabber.png"] = controls.PaintScrollGrabber(false),
    ["scroll_grabber_hover.png"] = controls.PaintScrollGrabber(true),
    ["progress_track.png"] = progress.PaintTrack(),
    ["progress_health.png"] = progress.PaintFill(UiProgressKind.Health),
    ["progress_experience.png"] = progress.PaintFill(UiProgressKind.Experience),
    ["progress_pacing.png"] = progress.PaintFill(UiProgressKind.Pacing),
    ["progress_affinity.png"] = progress.PaintFill(UiProgressKind.Affinity),
    ["cloud_divider.png"] = chrome.PaintCloudDivider(),
    ["seal_stamp.png"] = chrome.PaintSealStamp(),
    ["ink_mountains.png"] = backdrop.PaintInkMountains(),
    ["enemy_preview_sheet.png"] = sprites.PaintEnemySheet(),
    ["daily_actor_sheet.png"] = sprites.PaintDailyActorSheet(),
};

foreach ((string name, PixelCanvas canvas) in assets)
{
    PngWriter.Write(canvas, Path.Combine(outputRoot, name));
}

Console.WriteLine($"Generated {assets.Count} UI assets at {outputRoot}");
return 0;
