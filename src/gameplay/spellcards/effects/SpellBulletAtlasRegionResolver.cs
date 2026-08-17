using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 按原作图集家族与弹型语义挑选完整帧，避免把 32 像素大型弹切成四块。
/// </summary>
public static class SpellBulletAtlasRegionResolver
{
    private static readonly int[] ColorColumns = [1, 3, 5, 7, 9, 11, 13, 14];

    /// <summary>
    /// 以声明式弹型决定基础帧，映射变体和弹丸序号只负责轮换色种。
    /// </summary>
    public static SpellBulletVisualSelection Resolve(
        InternalVisualDefinition definition,
        SpellBulletStyleKind style,
        int projectileVariant,
        Texture2D texture) => Resolve(
            definition.AssetPath,
            definition.Variant,
            style,
            projectileVariant,
            texture);

    /// <summary>
    /// 允许图鉴等只读消费者使用同一切片规则，而无需伪造正式视觉目录定义。
    /// </summary>
    public static SpellBulletVisualSelection Resolve(
        string assetPath,
        int mappingVariant,
        SpellBulletStyleKind style,
        int projectileVariant,
        Texture2D texture)
    {
        var (row, frameSize, paletteSize) = ResolveFrame(assetPath, style);
        int paletteIndex = PositiveModulo(mappingVariant + projectileVariant, paletteSize);
        int column = frameSize == 16 ? ColorColumns[paletteIndex] : paletteIndex;
        var source = new Rect2(column * frameSize, row, frameSize, frameSize);
        ValidateBounds(texture, assetPath, source);
        return new SpellBulletVisualSelection(source, ResolveDisplaySize(style), style);
    }

    /// <summary>按图集路径识别 TH06、旧 Windows 作和现代作的三套真实排布。</summary>
    private static (int Row, int FrameSize, int PaletteSize) ResolveFrame(
        string assetPath,
        SpellBulletStyleKind style)
    {
        if (assetPath.Contains("/th06/", StringComparison.Ordinal))
        {
            return style switch
            {
                SpellBulletStyleKind.Orb => (32, 16, 8),
                SpellBulletStyleKind.Amulet or SpellBulletStyleKind.Shard => (64, 16, 8),
                SpellBulletStyleKind.Needle => (80, 16, 8),
                SpellBulletStyleKind.Butterfly => (96, 16, 8),
                SpellBulletStyleKind.Knife or SpellBulletStyleKind.Laser => (160, 32, 8),
                SpellBulletStyleKind.Star or SpellBulletStyleKind.Flame => (192, 32, 4),
                SpellBulletStyleKind.LargeOrb => (128, 32, 8),
                _ => throw new ArgumentOutOfRangeException(nameof(style)),
            };
        }

        if (IsOldWindowsAtlas(assetPath))
        {
            return style switch
            {
                SpellBulletStyleKind.Orb => (32, 16, 8),
                SpellBulletStyleKind.Amulet => (16, 16, 8),
                SpellBulletStyleKind.Shard => (64, 16, 8),
                SpellBulletStyleKind.Needle or SpellBulletStyleKind.Star => (80, 16, 8),
                SpellBulletStyleKind.LargeOrb => (112, 32, 8),
                SpellBulletStyleKind.Knife or SpellBulletStyleKind.Laser => (144, 32, 8),
                SpellBulletStyleKind.Butterfly => (176, 32, 8),
                SpellBulletStyleKind.Flame => (208, 32, 8),
                _ => throw new ArgumentOutOfRangeException(nameof(style)),
            };
        }

        return style switch
        {
            SpellBulletStyleKind.Orb => (32, 16, 8),
            SpellBulletStyleKind.Shard => (64, 16, 8),
            SpellBulletStyleKind.Needle or SpellBulletStyleKind.Knife => (80, 16, 8),
            SpellBulletStyleKind.Amulet => (112, 16, 8),
            SpellBulletStyleKind.Laser => (128, 16, 8),
            SpellBulletStyleKind.Butterfly => (144, 16, 8),
            SpellBulletStyleKind.Star => (160, 16, 8),
            SpellBulletStyleKind.Flame => (176, 16, 8),
            SpellBulletStyleKind.LargeOrb => (208, 32, 8),
            _ => throw new ArgumentOutOfRangeException(nameof(style)),
        };
    }

    /// <summary>判断素材是否采用 TH07 至 TH09 的旧式 32 像素大弹排布。</summary>
    private static bool IsOldWindowsAtlas(string assetPath)
    {
        return assetPath.Contains("/th07/", StringComparison.Ordinal)
            || assetPath.Contains("/th08/", StringComparison.Ordinal)
            || assetPath.Contains("/th09/", StringComparison.Ordinal);
    }

    /// <summary>为不同轮廓给出克制但可辨认的统一局内尺寸。</summary>
    private static float ResolveDisplaySize(SpellBulletStyleKind style)
    {
        return style switch
        {
            SpellBulletStyleKind.LargeOrb => 13f,
            SpellBulletStyleKind.Laser or SpellBulletStyleKind.Flame => 12f,
            SpellBulletStyleKind.Knife or SpellBulletStyleKind.Butterfly => 11f,
            SpellBulletStyleKind.Needle or SpellBulletStyleKind.Star => 10f,
            _ => 9f,
        };
    }

    /// <summary>对声明式切片执行边界校验，使错误图集在开发期直接暴露。</summary>
    private static void ValidateBounds(Texture2D texture, string assetPath, Rect2 source)
    {
        Vector2 atlasSize = texture.GetSize();
        if (source.Position.X < 0f || source.Position.Y < 0f
            || source.End.X > atlasSize.X || source.End.Y > atlasSize.Y)
        {
            throw new InvalidOperationException(
                $"Bullet region {source} exceeds atlas '{assetPath}'.");
        }
    }

    /// <summary>返回不会因负映射变体产生负图集坐标的数学模。</summary>
    private static int PositiveModulo(int value, int divisor)
    {
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
}
