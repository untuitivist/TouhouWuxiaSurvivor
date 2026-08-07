using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Structures;

namespace TouhouWuxiaSurvivor.Tools.WorldVerifier;

/// <summary>
/// 验证尚未启用的正作地区和结构不会混入本体，同时对应内容选择确实能够解锁它们。
/// </summary>
internal static class OfficialContentLeakVerifier
{
    /// <summary>
    /// 大范围采样 TH08 边界，确认迷途竹林和竹林古道只由东方永夜抄内容选择产生。
    /// </summary>
    public static void Verify(ulong seed)
    {
        var baseBiomes = new BiomeSelector(seed, ContentPackSelection.BaseOnly);
        var inSelection = new ContentPackSelection([ContentPackIds.ImperishableNight]);
        var inBiomes = new BiomeSelector(seed, inSelection);
        bool foundBamboo = false;
        for (long y = -3000; y <= 3000; y += 67)
        {
            for (long x = -3000; x <= 3000; x += 67)
            {
                Require(baseBiomes.Select(x, y) != BiomeId.BambooForest,
                    "Base world leaked the TH08 Bamboo Forest biome.");
                foundBamboo |= inBiomes.Select(x, y) == BiomeId.BambooForest;
            }
        }

        Require(foundBamboo, "TH08 selection did not unlock Bamboo Forest.");
        var baseStructures = new StructureLocator(seed, baseBiomes);
        var inStructures = new StructureLocator(seed, inBiomes);
        Require(!baseStructures.FindInBounds(-6000, -6000, 6000, 6000)
            .Any(item => item.Id == StructureId.BambooTrail),
            "Base world leaked the TH08 Bamboo Trail structure.");
        Require(inStructures.FindInBounds(-6000, -6000, 6000, 6000)
            .Any(item => item.Id == StructureId.BambooTrail),
            "TH08 selection did not unlock Bamboo Trail.");
    }

    /// <summary>
    /// 将正作隔离失败转换为带有明确原因的验证异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
