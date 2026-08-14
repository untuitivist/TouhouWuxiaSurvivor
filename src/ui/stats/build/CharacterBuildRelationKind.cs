namespace TouhouWuxiaSurvivor.Ui.Stats.Build;

/// <summary>
/// 区分构筑节点间的成长前置、特化分支和互斥关系，供可视化使用不同线型。
/// </summary>
public enum CharacterBuildRelationKind
{
    Requirement,
    Specialization,
    Exclusion,
}
