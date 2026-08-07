using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Official;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.World.Rendering;

/// <summary>
/// 把世界枚举解析为内部素材清单使用的内容包 ID，集中维护本体与正作的归属规则。
/// </summary>
public static class InternalContentSourceResolver
{
    private const string BaseSourceId = "base";

    /// <summary>
    /// 返回地区所属正作的内容包 ID，本体地区统一归入 base。
    /// </summary>
    public static string GetSourceId(BiomeId biome) =>
        OfficialWorldContentCatalog.TryGet(biome, out OfficialWorldContentDefinition definition)
            ? definition.PackId
            : BaseSourceId;

    /// <summary>
    /// 返回结构所属正作的内容包 ID，本体结构统一归入 base。
    /// </summary>
    public static string GetSourceId(StructureId structure) =>
        OfficialWorldContentCatalog.TryGet(structure, out OfficialWorldContentDefinition definition)
            ? definition.PackId
            : BaseSourceId;
}
