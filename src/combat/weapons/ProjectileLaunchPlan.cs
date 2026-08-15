using Godot;

namespace TouhouWuxiaSurvivor.Combat.Weapons;

/// <summary>保存单颗普通攻击弹的明确出生位置和有效飞行方向。</summary>
public readonly record struct ProjectileLaunchPlan(Vector2 Position, Vector2 Direction);
