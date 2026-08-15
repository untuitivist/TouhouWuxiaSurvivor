using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证全部奥义使用自身作品图集，并按几何、映射变体和弹丸序号选择多种有效弹型区域。
/// </summary>
public partial class SpellCardBulletVisualTest : Node
{
    /// <summary>遍历完整奥义目录并检查纹理、区域边界、单卡变化与全目录多样性。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyCatalogDiversity();
            GD.Print("Spell-card bullet visual test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>每张卡至少轮换两种区域，完整目录必须同时使用多份图集和多行弹型。</summary>
    private static void VerifyCatalogDiversity()
    {
        var catalog = new InternalVisualCatalog();
        var assets = new HashSet<string>(StringComparer.Ordinal);
        var shapes = new HashSet<string>(StringComparer.Ordinal);
        foreach (SpellCardDefinition card in SpellCardCatalog.All)
        {
            InternalVisualDefinition definition = null!;
            Texture2D texture = null!;
            Require(catalog.TryGet(card.SourcePackId, InternalVisualCategory.SpellCard,
                    card.FullName, out definition) &&
                definition.Kind == InternalVisualKind.BulletAtlas &&
                catalog.TryGetTexture(definition, out texture),
                $"Spell-card bullet atlas is unavailable: {card.Id}");
            assets.Add(definition.AssetPath);
            var cardRegions = new HashSet<Rect2>();
            Image atlasImage = texture.GetImage();
            for (int variant = 0; variant < 8; variant++)
            {
                Rect2 region = SpellBulletAtlasRegionResolver.Resolve(
                    definition, card.GeometryKind, variant, texture);
                Require(region.Position.X >= 0.0f && region.Position.Y >= 0.0f &&
                    region.End.X <= texture.GetWidth() && region.End.Y <= texture.GetHeight(),
                    $"Spell-card bullet region escaped its atlas: {card.Id}");
                Require(HasVisiblePixels(atlasImage, region),
                    $"Spell-card bullet region is transparent: {card.Id}/{variant}");
                cardRegions.Add(region);
                shapes.Add($"{definition.AssetPath}:{region.Position.Y}");
            }

            Require(cardRegions.Count >= 2,
                $"Spell card still renders one repeated bullet cell: {card.Id}");
        }

        Require(assets.Count >= 10 && shapes.Count >= 24,
            "Installed spell cards do not expose enough original atlas or shape diversity.");
    }

    /// <summary>扫描一个图集格的 Alpha，拒绝把透明留白计作可用原作弹型。</summary>
    private static bool HasVisiblePixels(Image image, Rect2 region)
    {
        int startX = (int)region.Position.X;
        int startY = (int)region.Position.Y;
        int endX = (int)region.End.X;
        int endY = (int)region.End.Y;
        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                if (image.GetPixel(x, y).A > 0.01f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>把任一素材契约失败转换为带明确奥义身份的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
