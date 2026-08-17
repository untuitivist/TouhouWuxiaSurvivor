using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在真实游戏画布绘制本体与红魔乡的弹型联系表，验收语义、色种和大小帧切片。
/// </summary>
public partial class SpellBulletVisualAcceptanceTest : Node2D
{
    private const string ScarletPackId = "th06_eosd";
    private readonly InternalVisualCatalog _visuals = new();
    private Font? _font;
    private SpellCardDefinition[] _cards = [];

    /// <summary>加载十三张当前正式内容卡，稳定绘制后保存最近邻截图。</summary>
    public override async void _Ready()
    {
        int exitCode = 0;
        try
        {
            GetWindow().Size = new Vector2I(1280, 720);
            _font = ThemeDB.FallbackFont;
            _cards = SpellCardCatalog.All.Where(card =>
                card.SourcePackId is "base" or ScarletPackId).ToArray();
            Require(_cards.Length == 13, $"Expected 13 visual cards, got {_cards.Length}.");
            QueueRedraw();
            await WaitFrames(3);
            SaveScreenshot();
            GD.Print("Spell bullet visual acceptance test passed.");
        }
        catch (Exception exception)
        {
            exitCode = 1;
            GD.PushError(exception.ToString());
        }

        GetTree().Quit(exitCode);
    }

    /// <summary>绘制两列卡片、四个色种和普通/中心通道图例，不对素材做平滑缩放。</summary>
    public override void _Draw()
    {
        if (_font is null || _cards.Length == 0) return;
        DrawRect(new Rect2(0.0f, 0.0f, 640.0f, 360.0f), new Color("08120d"));
        DrawString(_font, new Vector2(14.0f, 24.0f), "奥义弹型语义验收",
            HorizontalAlignment.Left, 240.0f, 16, new Color("f2e7c5"));
        DrawString(_font, new Vector2(326.0f, 22.0f), "普通弹：针    中心弹幕：星",
            HorizontalAlignment.Left, 220.0f, 11, new Color("bacbb8"));
        DrawChannelSamples();

        for (int index = 0; index < _cards.Length; index++)
        {
            int column = index / 7;
            int row = index % 7;
            DrawCard(_cards[index], 14.0f + column * 312.0f, 39.0f + row * 44.0f);
        }
    }

    /// <summary>绘制一张卡片的短名、中文弹型与四个色种，完整帧统一到局内显示尺寸。</summary>
    private void DrawCard(SpellCardDefinition card, float x, float y)
    {
        DrawRect(new Rect2(x, y, 302.0f, 39.0f), new Color("101d15"));
        DrawString(_font, new Vector2(x + 5.0f, y + 14.0f), card.ShortName,
            HorizontalAlignment.Left, 148.0f, 10, new Color("e9e2cb"));
        DrawString(_font, new Vector2(x + 5.0f, y + 29.0f),
            $"{StyleText(card.BulletStyleKind)} · {card.SourcePackId}",
            HorizontalAlignment.Left, 148.0f, 9, new Color("a8bca6"));

        Require(TryLoad(card, out InternalVisualDefinition definition, out Texture2D texture),
            $"Visual acceptance cannot load {card.Id}.");
        for (int variant = 0; variant < 4; variant++)
        {
            SpellBulletVisualSelection selection = SpellBulletAtlasRegionResolver.Resolve(
                definition, card.BulletStyleKind, variant, texture);
            Vector2 center = new(x + 171.0f + variant * 31.0f, y + 19.0f);
            Rect2 destination = selection.CreateDestination(center);
            DrawTextureRectRegion(texture, destination, selection.Source);
            DrawRect(destination.Grow(2.0f), new Color("536a55"), false, 1.0f);
        }
    }

    /// <summary>在标题区用同一本体图集展示两个基础射击通道的不同轮廓。</summary>
    private void DrawChannelSamples()
    {
        SpellCardDefinition card = SpellCardCatalog.FindById("reimu_fantasy_seal")!;
        Require(TryLoad(card, out InternalVisualDefinition definition, out Texture2D texture),
            "Default channel atlas is unavailable.");
        SpellBulletVisualSelection needle = SpellBulletAtlasRegionResolver.Resolve(
            definition, SpellBulletStyleKind.Needle, 0, texture);
        SpellBulletVisualSelection star = SpellBulletAtlasRegionResolver.Resolve(
            definition, SpellBulletStyleKind.Star, 1, texture);
        DrawTextureRectRegion(texture, needle.CreateDestination(new Vector2(556.0f, 17.0f)),
            needle.Source);
        DrawTextureRectRegion(texture, star.CreateDestination(new Vector2(612.0f, 17.0f)),
            star.Source);
    }

    /// <summary>按稳定内容身份取得图集映射与纹理，失败时不做跨作品静默回退。</summary>
    private bool TryLoad(
        SpellCardDefinition card,
        out InternalVisualDefinition definition,
        out Texture2D texture)
    {
        definition = null!;
        texture = null!;
        return _visuals.TryGet(card.SourcePackId, InternalVisualCategory.SpellCard,
                card.FullName, out definition)
            && definition.Kind == InternalVisualKind.BulletAtlas
            && _visuals.TryGetTexture(definition, out texture);
    }

    /// <summary>把内部枚举转成策划可读的中文弹型名，便于直接检查卡名与视觉是否相符。</summary>
    private static string StyleText(SpellBulletStyleKind style) => style switch
    {
        SpellBulletStyleKind.Orb => "灵玉",
        SpellBulletStyleKind.Amulet => "灵札",
        SpellBulletStyleKind.Needle => "飞针",
        SpellBulletStyleKind.Knife => "飞刀",
        SpellBulletStyleKind.Star => "星弹",
        SpellBulletStyleKind.Flame => "焰弹",
        SpellBulletStyleKind.Butterfly => "蝶弹",
        SpellBulletStyleKind.Laser => "光束",
        SpellBulletStyleKind.Shard => "碎晶",
        SpellBulletStyleKind.LargeOrb => "大玉",
        _ => style.ToString(),
    };

    /// <summary>等待固定帧数，让视口尺寸、字体和 Canvas 指令稳定后再截图。</summary>
    private async Task WaitFrames(int count)
    {
        for (int index = 0; index < count; index++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>保存 1280×720 最近邻截图；无头回归只执行渲染契约并跳过文件输出。</summary>
    private void SaveScreenshot()
    {
        if (DisplayServer.GetName() == "headless") return;
        Image image = GetViewport().GetTexture().GetImage();
        if (image.GetWidth() != 1280 || image.GetHeight() != 720)
        {
            image.Resize(1280, 720, Image.Interpolation.Nearest);
        }
        string path = ProjectSettings.GlobalizePath(
            "user://visual-spell-bullet-semantics-1280x720.png");
        Require(image.SavePng(path) == Error.Ok, $"Could not save screenshot: {path}.");
        GD.Print($"Spell bullet visual screenshot: {path}");
    }

    /// <summary>把任何视觉契约失败转换为带明确原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
