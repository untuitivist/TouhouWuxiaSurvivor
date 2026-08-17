using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.World.Official;
using TouhouWuxiaSurvivor.World.Tiles;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>把正式敌人定义投影为图鉴条目，完整展示固定属性、AI、强度档和弹幕档案。</summary>
public static class EnemyCompendiumEntryFactory
{
    /// <summary>按敌人目录顺序创建全部条目，并从内容包身份解析来源与代表地表。</summary>
    public static IReadOnlyList<CompendiumEntry> CreateAll() => EnemyCatalog.All
        .Select(CreateEntry)
        .ToArray();

    /// <summary>创建单个敌人条目；所有数值均读取出生定义，不展示随阶段增长的虚假属性。</summary>
    private static CompendiumEntry CreateEntry(EnemyDefinition enemy)
    {
        ContentPackDefinition source = enemy.RequiredContentPack is null
            ? ContentPackCatalog.Base
            : ContentPackCatalog.All.Single(pack => pack.Id == enemy.RequiredContentPack);
        EnemyBalanceProfile balance = EnemyBalanceProfile.Evaluate(enemy);
        string biomes = enemy.AllowedBiomes.Count == 0
            ? "所有地区"
            : string.Join("、", enemy.AllowedBiomes.Select(BiomeNames.GetChinese));
        string special = enemy.ExplodesOnDeath ? "死亡时爆炸" : "无";
        string rule = "基础生命、速度、触伤与弹丸不随压力档位提升 · " +
            CompendiumVisualProvenanceCatalog.Placeholder;
        int previewVariant = enemy.RequiredContentPack is null
            ? Math.Abs((int)enemy.Archetype) % 4
            : OfficialWorldContentCatalog.GetByPack(source.Id)
                .First(world => world.EnemyName == enemy.DisplayName).RegionIndex;
        return new CompendiumEntry(
            CompendiumCategory.Enemy, enemy.DisplayName, CompendiumSourceText.GetId(source),
            CompendiumSourceText.GetLabel(source),
            $"{GetTierName(enemy.StrengthTier)} · {GetAiName(enemy.AiProfile.Kind)} · " +
            $"生命 {enemy.BaseMaxHealth}",
            [
                new("栖息地区", biomes, true),
                new("强度类型", GetTierName(enemy.StrengthTier)),
                new("威胁等级", $"{balance.ThreatRank}/5 · {balance.ThreatLabel}"),
                new("行动 AI", DescribeAi(enemy.AiProfile)),
                new("敌方弹幕", DescribeProjectile(enemy.ProjectileProfile)),
                new("基础生命", enemy.BaseMaxHealth.ToString()),
                new("移动速度", $"{enemy.MoveSpeed:0.#}"),
                new("接触伤害", enemy.ContactDamage.ToString()),
                new("刷新权重", $"{enemy.SpawnWeight:0.#}"),
                new("目录解锁", $"{enemy.UnlockTime:0} 秒"),
                new("预计击破", $"{balance.BaseTimeToKill:0.0} 秒"),
                new("掉落率", $"{enemy.DropChance:P0}"),
                new("特殊", special),
                new("规则与素材", rule, true),
            ], GetPreviewTile(enemy), previewVariant, enemy);
    }

    /// <summary>返回四档敌群职责名称；档位改变刷新占比，不会改写该敌人的基础属性。</summary>
    private static string GetTierName(EnemyStrengthTier tier) => tier switch
    {
        EnemyStrengthTier.Common => "普通",
        EnemyStrengthTier.Veteran => "强敌",
        EnemyStrengthTier.Elite => "精锐",
        EnemyStrengthTier.Champion => "头目",
        _ => throw new ArgumentOutOfRangeException(nameof(tier)),
    };

    /// <summary>返回适合摘要的 AI 短名。</summary>
    private static string GetAiName(EnemyAiKind kind) => kind switch
    {
        EnemyAiKind.Chase => "追击",
        EnemyAiKind.OrbitShooter => "绕射",
        EnemyAiKind.Charger => "突进",
        EnemyAiKind.BossPhased => "分阶段 Boss",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>组合 AI 类型与真实参数，让同名职责的移动差异仍然可以在图鉴中核对。</summary>
    private static string DescribeAi(EnemyAiProfile profile) => profile.Kind switch
    {
        EnemyAiKind.OrbitShooter => $"绕射 · 距离 {profile.PreferredRange:0}",
        EnemyAiKind.Charger => $"突进 · 间隔 {profile.ChargeInterval:0.##}秒",
        EnemyAiKind.BossPhased => $"Boss 游走 · 距离 {profile.PreferredRange:0}",
        _ => "持续追击",
    };

    /// <summary>把敌人射击档案压缩为一行可比较信息；禁用档案明确显示不发射弹幕。</summary>
    private static string DescribeProjectile(EnemyProjectileProfile profile) => profile.Enabled
        ? $"{profile.ShotCount}发/{profile.FireInterval:0.##}秒 · {profile.ProjectileSpeed:0}速"
        : "无远程弹幕";

    /// <summary>选择首个栖息地区的代表地表；通用敌人使用幻想乡草地。</summary>
    private static TileId GetPreviewTile(EnemyDefinition enemy)
    {
        if (enemy.RequiredContentPack is not null)
        {
            return OfficialWorldContentCatalog.GetByPack(enemy.RequiredContentPack)
                .First(world => world.EnemyName == enemy.DisplayName).BaseTile;
        }

        if (enemy.AllowedBiomes.Count == 0) return TileId.GrassBase;
        return enemy.AllowedBiomes[0] switch
        {
            BiomeId.HakureiShrine => TileId.ShrineGrassBase,
            BiomeId.HumanVillage => TileId.DirtBase,
            BiomeId.MagicForest => TileId.ForestFloorBase,
            BiomeId.YoukaiMountain => TileId.MountainGrassBase,
            _ => TileId.GrassBase,
        };
    }
}
