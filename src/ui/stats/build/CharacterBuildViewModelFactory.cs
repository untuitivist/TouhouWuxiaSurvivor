using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Gameplay.Progression.Definitions;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 从本局构筑、内容快照与境界生成可视化模型，是 E 键构筑页唯一的数据组装入口。
/// </summary>
public static class CharacterBuildViewModelFactory
{
    /// <summary>
    /// 按目录稳定顺序投影亲和、升级、特化和关系边，不修改运行状态也不读取场景节点。
    /// </summary>
    public static CharacterBuildViewModel Create(
        RunBuildState build,
        ContentPackSelection content,
        int runLevel,
        CharacterCombatRole combatRole = CharacterCombatRole.Balanced)
    {
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(content);
        var nodes = new List<CharacterBuildNodeView>();
        var relations = new List<CharacterBuildRelationView>();
        int specializationOrder = 0;
        for (int index = 0; index < RunUpgradeCatalog.All.Count; index++)
        {
            RunUpgradeDefinition definition = RunUpgradeCatalog.All[index];
            nodes.Add(CharacterBuildNodeFactory.CreateUpgrade(
                definition, build, content, index));
            AddUpgradeRelations(definition, build, relations);
            foreach (RunUpgradeSpecialization specialization in definition.Specializations)
            {
                nodes.Add(CharacterBuildNodeFactory.CreateSpecialization(
                    definition, specialization, build, content,
                    runLevel, specializationOrder++));
                AddSpecializationRelations(definition, specialization, build, relations);
            }
        }

        return new CharacterBuildViewModel(
            runLevel, build.TotalRanks, combatRole,
            CreateAffinities(build), nodes, relations);
    }

    /// <summary>
    /// 计算五类亲和的绝对点数与全局占比；空构筑没有虚假的主亲和。
    /// </summary>
    private static IReadOnlyList<CharacterBuildAffinityView> CreateAffinities(
        RunBuildState build)
    {
        RunUpgradeAffinity[] kinds = Enum.GetValues<RunUpgradeAffinity>();
        int[] values = kinds.Select(build.GetAffinity).ToArray();
        int total = values.Sum();
        int maximum = values.DefaultIfEmpty().Max();
        return kinds.Select((affinity, index) => new CharacterBuildAffinityView(
            affinity, RunUpgradeAffinityFormatter.Format(affinity), values[index],
            total == 0 ? 0.0f : values[index] / (float)total,
            maximum > 0 && values[index] == maximum, index)).ToArray();
    }

    /// <summary>
    /// 将升级的全部前置和互斥声明转换为关系边，并根据当前重数标记是否满足。
    /// </summary>
    private static void AddUpgradeRelations(
        RunUpgradeDefinition definition,
        RunBuildState build,
        ICollection<CharacterBuildRelationView> relations)
    {
        foreach (RunUpgradeRequirement requirement in definition.Requirements)
        {
            relations.Add(new CharacterBuildRelationView(
                requirement.RequiredUpgradeId, definition.Id,
                CharacterBuildRelationKind.Requirement, requirement.MinimumRank,
                build.GetRank(requirement.RequiredUpgradeId) >= requirement.MinimumRank,
                $"{requirement.MinimumRank}重"));
        }

        foreach (string excludedId in definition.ExcludedUpgradeIds)
        {
            relations.Add(new CharacterBuildRelationView(
                excludedId, definition.Id, CharacterBuildRelationKind.Exclusion,
                0, build.GetRank(excludedId) == 0 && build.GetRank(definition.Id) == 0,
                "互斥"));
        }
    }

    /// <summary>
    /// 建立基础修行到特化的解锁边，并把特化间显式互斥声明保留给关系图。
    /// </summary>
    private static void AddSpecializationRelations(
        RunUpgradeDefinition definition,
        RunUpgradeSpecialization specialization,
        RunBuildState build,
        ICollection<CharacterBuildRelationView> relations)
    {
        relations.Add(new CharacterBuildRelationView(
            definition.Id, specialization.Id,
            CharacterBuildRelationKind.Specialization, specialization.RequiredRank,
            build.GetRank(definition.Id) >= specialization.RequiredRank,
            $"{specialization.RequiredRank}重后特化"));
        foreach (string excludedId in specialization.ExcludedSpecializationIds)
        {
            relations.Add(new CharacterBuildRelationView(
                excludedId, specialization.Id, CharacterBuildRelationKind.Exclusion,
                0, !build.HasSpecialization(excludedId) &&
                    !build.HasSpecialization(specialization.Id), "互斥"));
        }
    }
}
