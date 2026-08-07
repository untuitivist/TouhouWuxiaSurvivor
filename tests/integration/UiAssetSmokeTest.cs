using Godot;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证武侠 UI 像素资产已被 Godot 导入，尺寸契约稳定且画面并非空白透明图。
/// </summary>
public partial class UiAssetSmokeTest : Node
{
    private static readonly Dictionary<string, Vector2I> ExpectedAssets = new()
    {
        ["res://assets/ui/wuxia/paper_fiber.png"] = new Vector2I(64, 64),
        ["res://assets/ui/wuxia/scroll_panel.png"] = new Vector2I(24, 24),
        ["res://assets/ui/wuxia/preview_frame.png"] = new Vector2I(24, 24),
        ["res://assets/ui/wuxia/cloud_divider.png"] = new Vector2I(128, 8),
        ["res://assets/ui/wuxia/seal_stamp.png"] = new Vector2I(24, 24),
        ["res://assets/ui/wuxia/ink_mountains.png"] = new Vector2I(320, 180),
        ["res://assets/ui/wuxia/enemy_preview_sheet.png"] = new Vector2I(384, 16),
        ["res://assets/ui/wuxia/daily_actor_sheet.png"] = new Vector2I(128, 16),
    };

    /// <summary>
    /// 逐项读取纹理与源图像，并以明确退出码报告资源生成或导入回归。
    /// </summary>
    public override void _Ready()
    {
        try
        {
            foreach ((string path, Vector2I expectedSize) in ExpectedAssets)
            {
                VerifyAsset(path, expectedSize);
            }

            GD.Print("UI asset smoke test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 检查纹理尺寸、可见像素数量和色彩数量，防止生成器产出空图或单色占位图。
    /// </summary>
    private static void VerifyAsset(string path, Vector2I expectedSize)
    {
        Texture2D? texture = ResourceLoader.Load<Texture2D>(path);
        if (texture is null)
        {
            throw new InvalidOperationException($"UI texture was not imported: {path}");
        }

        Require(texture.GetSize() == expectedSize, $"UI texture size drifted: {path}");

        Image image = texture.GetImage();
        int visiblePixels = 0;
        HashSet<Color> visibleColors = [];
        for (int y = 0; y < image.GetHeight(); y++)
        {
            for (int x = 0; x < image.GetWidth(); x++)
            {
                Color color = image.GetPixel(x, y);
                if (color.A <= 0.01f)
                {
                    continue;
                }

                visiblePixels++;
                visibleColors.Add(color);
            }
        }

        Require(visiblePixels >= 16, $"UI texture is visually empty: {path}");
        Require(visibleColors.Count >= 2, $"UI texture is only a flat placeholder: {path}");
    }

    /// <summary>
    /// 将资源契约失败转换为包含具体路径的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
