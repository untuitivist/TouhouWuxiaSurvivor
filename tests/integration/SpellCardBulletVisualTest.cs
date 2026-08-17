using Godot;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证全部奥义按独立美术语义使用完整原作弹型，而非由空间几何随机猜测或切碎大图。
/// </summary>
public partial class SpellCardBulletVisualTest : Node
{
    /// <summary>遍历完整奥义目录并检查纹理、区域边界、单卡变化与全目录多样性。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyCatalogDiversityAndCuts();
            VerifyBaseAndScarletSemantics();
            VerifyDefaultProjectileChannels();
            VerifyProjectilePosePolicy();
            GD.Print("Spell-card bullet visual test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>遍历完整目录，校验十类弹型、色种轮换、帧尺寸、边界和 32 像素完整性。</summary>
    private static void VerifyCatalogDiversityAndCuts()
    {
        var catalog = new InternalVisualCatalog();
        var assets = new HashSet<string>(StringComparer.Ordinal);
        var styles = new HashSet<SpellBulletStyleKind>();
        var frameSizes = new HashSet<float>();
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
            styles.Add(card.BulletStyleKind);
            var cardRegions = new HashSet<Rect2>();
            Image atlasImage = texture.GetImage();
            for (int variant = 0; variant < 8; variant++)
            {
                SpellBulletVisualSelection selection = SpellBulletAtlasRegionResolver.Resolve(
                    definition, card.BulletStyleKind, variant, texture);
                Rect2 region = selection.Source;
                Require(region.Position.X >= 0.0f && region.Position.Y >= 0.0f &&
                    region.End.X <= texture.GetWidth() && region.End.Y <= texture.GetHeight(),
                    $"Spell-card bullet region escaped its atlas: {card.Id}");
                float expectedSize = ExpectedFrameSize(definition.AssetPath, card.BulletStyleKind);
                Require(region.Size == Vector2.One * expectedSize &&
                    Mathf.IsEqualApprox(region.Position.X % expectedSize, 0.0f),
                    $"Spell-card bullet frame was sliced on the wrong grid: {card.Id}/{region}");
                Require(HasVisiblePixels(atlasImage, region),
                    $"Spell-card bullet region is transparent: {card.Id}/{variant}");
                if (expectedSize == 32.0f)
                {
                    Require(HasVisiblePixelsOutsideFirstQuarter(atlasImage, region),
                        $"Large bullet was reduced to its first 16px quarter: {card.Id}/{variant}");
                }
                Require(selection.DisplaySize is >= 9.0f and <= 13.0f,
                    $"Spell-card display size escaped the shared visual budget: {card.Id}");
                cardRegions.Add(region);
                frameSizes.Add(region.Size.X);
            }

            Require(cardRegions.Count >= 2,
                $"Spell card still renders one repeated bullet cell: {card.Id}");
        }

        Require(SpellCardCatalog.All.Count == 51 && assets.Count >= 10,
            "Installed spell-card atlas coverage regressed.");
        Require(styles.SetEquals(Enum.GetValues<SpellBulletStyleKind>()) &&
            frameSizes.SetEquals(new[] { 16.0f, 32.0f }),
            "Installed spell cards do not cover all semantic styles and frame sizes.");
    }

    /// <summary>锁定本体和红魔乡代表卡的美术语言，使名字、玩法和弹型不会再次错配。</summary>
    private static void VerifyBaseAndScarletSemantics()
    {
        var expected = new Dictionary<string, SpellBulletStyleKind>(StringComparer.Ordinal)
        {
            ["reimu_fantasy_seal"] = SpellBulletStyleKind.Orb,
            ["reimu_evil_sealing_circle"] = SpellBulletStyleKind.Amulet,
            ["reimu_duplex_barrier"] = SpellBulletStyleKind.Amulet,
            ["reimu_omnidirectional_oni_binding_circle"] = SpellBulletStyleKind.Amulet,
            ["marisa_master_spark"] = SpellBulletStyleKind.Laser,
            ["marisa_stardust_reverie"] = SpellBulletStyleKind.Star,
            ["th06_rumia_night_bird"] = SpellBulletStyleKind.Shard,
            ["th06_cirno_perfect_freeze"] = SpellBulletStyleKind.Shard,
            ["th06_patchouli_philosophers_stone"] = SpellBulletStyleKind.LargeOrb,
            ["th06_sakuya_killing_doll"] = SpellBulletStyleKind.Knife,
            ["th06_remilia_scarlet_shoot"] = SpellBulletStyleKind.Needle,
            ["th06_flandre_laevatein"] = SpellBulletStyleKind.Flame,
        };

        foreach ((string id, SpellBulletStyleKind style) in expected)
        {
            SpellCardDefinition card = SpellCardCatalog.FindById(id)
                ?? throw new InvalidOperationException($"Missing semantic test card: {id}");
            Require(card.BulletStyleKind == style,
                $"Spell-card semantic bullet style regressed: {id}/{card.BulletStyleKind}");
        }
    }

    /// <summary>确认普通自瞄弹、中心弹幕和四种敌弹不再退化为同一行圆玉换色。</summary>
    private static void VerifyDefaultProjectileChannels()
    {
        Require(ProjectileBulletStylePolicy.Resolve(ProjectileFaction.Player, 0)
                == SpellBulletStyleKind.Needle
            && ProjectileBulletStylePolicy.Resolve(ProjectileFaction.Player, 1)
                == SpellBulletStyleKind.Star,
            "Player ordinary and centered barrage channels share one bullet shape.");
        var enemyStyles = Enumerable.Range(0, 4)
            .Select(variant => ProjectileBulletStylePolicy.Resolve(
                ProjectileFaction.Enemy, variant)).ToHashSet();
        Require(enemyStyles.Count == 4,
            "Enemy fallback projectiles do not expose four distinct bullet shapes.");
    }

    /// <summary>锁定方向型弹丸随速度转身、对称弹不被运动方向强制旋转的姿态契约。</summary>
    private static void VerifyProjectilePosePolicy()
    {
        SpellBulletStyleKind[] directionalStyles =
        [
            SpellBulletStyleKind.Amulet,
            SpellBulletStyleKind.Needle,
            SpellBulletStyleKind.Knife,
            SpellBulletStyleKind.Flame,
            SpellBulletStyleKind.Butterfly,
            SpellBulletStyleKind.Laser,
            SpellBulletStyleKind.Shard,
        ];
        Require(directionalStyles.All(ProjectileVisualPosePolicy.IsDirectional)
            && !ProjectileVisualPosePolicy.IsDirectional(SpellBulletStyleKind.Orb)
            && !ProjectileVisualPosePolicy.IsDirectional(SpellBulletStyleKind.Star)
            && !ProjectileVisualPosePolicy.IsDirectional(SpellBulletStyleKind.LargeOrb),
            "Directional and symmetric bullet semantics were mixed.");
        Require(Mathf.IsZeroApprox(ProjectileVisualPosePolicy.ResolveRotation(
                SpellBulletStyleKind.Needle, Vector2.Down))
            && Mathf.IsEqualApprox(ProjectileVisualPosePolicy.ResolveRotation(
                SpellBulletStyleKind.Needle, Vector2.Right), -Mathf.Pi * 0.5f)
            && Mathf.IsEqualApprox(ProjectileVisualPosePolicy.ResolveRotation(
                SpellBulletStyleKind.Knife, Vector2.Left), Mathf.Pi * 0.5f)
            && Mathf.IsEqualApprox(Mathf.Abs(ProjectileVisualPosePolicy.ResolveRotation(
                SpellBulletStyleKind.Laser, Vector2.Up)), Mathf.Pi),
            "Directional bullet pose no longer follows its velocity vector.");
        Require(Mathf.IsZeroApprox(ProjectileVisualPosePolicy.ResolveRotation(
                SpellBulletStyleKind.Orb, Vector2.Right))
            && Mathf.IsZeroApprox(ProjectileVisualPosePolicy.ResolveRotation(
                SpellBulletStyleKind.Needle, Vector2.Zero)),
            "Symmetric or stationary bullets received an arbitrary pose.");
    }

    /// <summary>返回各原作图集家族中该语义弹型的真实单帧宽度。</summary>
    private static float ExpectedFrameSize(string assetPath, SpellBulletStyleKind style)
    {
        bool th06 = assetPath.Contains("/th06/", StringComparison.Ordinal);
        bool oldWindows = assetPath.Contains("/th07/", StringComparison.Ordinal)
            || assetPath.Contains("/th08/", StringComparison.Ordinal)
            || assetPath.Contains("/th09/", StringComparison.Ordinal);
        if (th06)
        {
            return style is SpellBulletStyleKind.Knife or SpellBulletStyleKind.Laser
                or SpellBulletStyleKind.Star or SpellBulletStyleKind.Flame
                or SpellBulletStyleKind.LargeOrb ? 32.0f : 16.0f;
        }

        if (oldWindows)
        {
            return style is SpellBulletStyleKind.Knife or SpellBulletStyleKind.Laser
                or SpellBulletStyleKind.Butterfly or SpellBulletStyleKind.Flame
                or SpellBulletStyleKind.LargeOrb ? 32.0f : 16.0f;
        }

        return style == SpellBulletStyleKind.LargeOrb ? 32.0f : 16.0f;
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

    /// <summary>确认大型帧的可见内容越过左上十六像素，直接拦截旧版四分之一切片。</summary>
    private static bool HasVisiblePixelsOutsideFirstQuarter(Image image, Rect2 region)
    {
        int startX = (int)region.Position.X;
        int startY = (int)region.Position.Y;
        for (int y = startY; y < (int)region.End.Y; y++)
        {
            for (int x = startX; x < (int)region.End.X; x++)
            {
                bool inFirstQuarter = x < startX + 16 && y < startY + 16;
                if (!inFirstQuarter && image.GetPixel(x, y).A > 0.01f) return true;
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
