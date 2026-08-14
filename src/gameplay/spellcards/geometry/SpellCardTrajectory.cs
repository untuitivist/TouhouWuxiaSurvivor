using Godot;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Geometry;

/// <summary>
/// 保存一枚符卡投射物的起点、目标和初始弯曲量，供正式节点与测试共享同一规划结果。
/// </summary>
public readonly record struct SpellCardTrajectory(
    Vector2 SpawnPosition,
    Vector2 TargetPosition,
    float Curvature);
