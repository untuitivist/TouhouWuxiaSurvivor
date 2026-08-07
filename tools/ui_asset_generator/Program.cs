using TouhouWuxiaSurvivor.Tools.TileGenerator;
using TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: ui_asset_generator <output-directory>");
    return 2;
}

string outputRoot = Path.GetFullPath(args[0]);
var chrome = new UiChromePainter();
var sprites = new PreviewSpritePainter();
var assets = new Dictionary<string, PixelCanvas>
{
    ["paper_fiber.png"] = chrome.PaintPaperFiber(),
    ["scroll_panel.png"] = chrome.PaintScrollPanel(),
    ["preview_frame.png"] = chrome.PaintPreviewFrame(),
    ["cloud_divider.png"] = chrome.PaintCloudDivider(),
    ["seal_stamp.png"] = chrome.PaintSealStamp(),
    ["ink_mountains.png"] = chrome.PaintInkMountains(),
    ["enemy_preview_sheet.png"] = sprites.PaintEnemySheet(),
    ["daily_actor_sheet.png"] = sprites.PaintDailyActorSheet(),
};

foreach ((string name, PixelCanvas canvas) in assets)
{
    PngWriter.Write(canvas, Path.Combine(outputRoot, name));
}

Console.WriteLine($"Generated {assets.Count} UI assets at {outputRoot}");
return 0;
