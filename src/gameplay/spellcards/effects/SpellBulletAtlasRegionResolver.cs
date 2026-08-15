using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 从原作 16 像素弹幕图集选择稳定形状和颜色，使奥义身份不再退化为同一行圆弹。
/// </summary>
public static class SpellBulletAtlasRegionResolver
{
    private const int CellSize = 16;
    private static readonly int[] ShapeRows = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13];
    private static readonly Dictionary<string, bool[]> OccupiedCells = new(StringComparer.Ordinal);

    /// <summary>
    /// 以几何决定基础弹型、映射变体区分同类卡片、弹丸序号轮换颜色和次级弹型。
    /// </summary>
    public static Rect2 Resolve(
        InternalVisualDefinition definition,
        SpellCardGeometryKind geometryKind,
        int projectileVariant,
        Texture2D texture)
    {
        Vector2 textureSize = texture.GetSize();
        int columns = Math.Max(1, (int)textureSize.X / CellSize);
        int rows = Math.Max(1, (int)textureSize.Y / CellSize);
        int normalizedVariant = Math.Max(0, projectileVariant);
        int geometryIndex = geometryKind switch
        {
            SpellCardGeometryKind.Orbit => 0,
            SpellCardGeometryKind.Fan => 2,
            SpellCardGeometryKind.Line => 3,
            SpellCardGeometryKind.Ring => 5,
            SpellCardGeometryKind.Backstab => 6,
            _ => 0,
        };
        int shapeIndex = PositiveModulo(
            geometryIndex + definition.Variant * 2 + normalizedVariant / 4,
            ShapeRows.Length);
        int row = Math.Min(rows - 1, ShapeRows[shapeIndex]);
        int colorCount = Math.Max(1, Math.Min(14, columns - 1));
        int column = columns <= 1
            ? 0
            : 1 + PositiveModulo(
                definition.Variant * 3 + normalizedVariant, colorCount);
        bool[] occupied = GetOccupiedCells(definition.AssetPath, texture, columns, rows);
        (column, row) = FindVisibleCell(
            occupied, columns, rows, column, row, colorCount, shapeIndex);
        return new Rect2(column * CellSize, row * CellSize, CellSize, CellSize);
    }

    /// <summary>首次使用图集时扫描 Alpha 并缓存每个十六像素格是否含可见弹丸。</summary>
    private static bool[] GetOccupiedCells(
        string assetPath,
        Texture2D texture,
        int columns,
        int rows)
    {
        if (OccupiedCells.TryGetValue(assetPath, out bool[]? cached))
        {
            return cached;
        }

        Image image = texture.GetImage();
        image.Convert(Image.Format.Rgba8);
        byte[] pixels = image.GetData();
        int width = image.GetWidth();
        var occupied = new bool[columns * rows];
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                occupied[row * columns + column] = CellHasAlpha(
                    pixels, width, column * CellSize, row * CellSize);
            }
        }

        OccupiedCells.Add(assetPath, occupied);
        return occupied;
    }

    /// <summary>优先保留同一弹型的其他颜色，再按候选形状和全图可见格确定性回退。</summary>
    private static (int Column, int Row) FindVisibleCell(
        bool[] occupied,
        int columns,
        int rows,
        int preferredColumn,
        int preferredRow,
        int colorCount,
        int shapeIndex)
    {
        if (IsOccupied(occupied, columns, preferredColumn, preferredRow))
        {
            return (preferredColumn, preferredRow);
        }

        for (int offset = 1; offset < colorCount; offset++)
        {
            int column = 1 + PositiveModulo(preferredColumn - 1 + offset, colorCount);
            if (IsOccupied(occupied, columns, column, preferredRow)) return (column, preferredRow);
        }

        for (int rowOffset = 1; rowOffset < ShapeRows.Length; rowOffset++)
        {
            int row = Math.Min(rows - 1, ShapeRows[(shapeIndex + rowOffset) % ShapeRows.Length]);
            for (int columnOffset = 0; columnOffset < colorCount; columnOffset++)
            {
                int column = 1 + PositiveModulo(preferredColumn - 1 + columnOffset, colorCount);
                if (IsOccupied(occupied, columns, column, row)) return (column, row);
            }
        }

        for (int index = 0; index < occupied.Length; index++)
        {
            if (occupied[index]) return (index % columns, index / columns);
        }

        return (preferredColumn, preferredRow);
    }

    /// <summary>读取缓存格状态，并把越界候选视为透明。</summary>
    private static bool IsOccupied(bool[] occupied, int columns, int column, int row) =>
        column >= 0 && row >= 0 && row * columns + column < occupied.Length &&
        occupied[row * columns + column];

    /// <summary>直接扫描 RGBA8 字节中的 Alpha，避免逐像素跨 Godot 绑定调用。</summary>
    private static bool CellHasAlpha(byte[] pixels, int width, int startX, int startY)
    {
        for (int y = startY; y < startY + CellSize; y++)
        {
            for (int x = startX; x < startX + CellSize; x++)
            {
                if (pixels[(y * width + x) * 4 + 3] > 2) return true;
            }
        }

        return false;
    }

    /// <summary>返回不会因负映射变体产生负图集坐标的数学模。</summary>
    private static int PositiveModulo(int value, int divisor)
    {
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
}
