using TouhouWuxiaSurvivor.World.Map;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.Ui.Map;

/// <summary>
/// 从已探索范围和结构选址器生成当前地图视口需要固定显示的结构名称。
/// </summary>
public sealed class MapLabelProvider
{
    private readonly ExploredMapStore _exploredMap;
    private readonly StructureLocator _structures;

    /// <summary>
    /// 创建只读取共享世界数据的标签提供器，不重新生成或修改任何区块。
    /// </summary>
    public MapLabelProvider(
        ExploredMapStore exploredMap,
        StructureLocator structures)
    {
        _exploredMap = exploredMap;
        _structures = structures;
    }

    /// <summary>
    /// 为可见绝对 Tile 矩形生成已经探索的结构标签。
    /// </summary>
    public IReadOnlyList<MapLabel> Build(
        long left,
        long top,
        int width,
        int height)
    {
        long right = left + width - 1;
        long bottom = top + height - 1;
        var labels = new List<MapLabel>();
        AddStructureLabels(labels, left, top, right, bottom);
        return labels;
    }

    /// <summary>
    /// 查询视口内的确定性结构锚点，并过滤尚未探索的结构。
    /// </summary>
    private void AddStructureLabels(
        List<MapLabel> labels,
        long left,
        long top,
        long right,
        long bottom)
    {
        foreach (StructurePlacement placement in _structures.FindInBounds(left, top, right, bottom))
        {
            if (!_exploredMap.TryGetTile(placement.X, placement.Y, out _))
            {
                continue;
            }

            labels.Add(new MapLabel(
                MapLabelKind.Structure,
                $"地标 · {StructureNames.GetChinese(placement.Id)}",
                placement.X,
                placement.Y));
        }
    }

}
