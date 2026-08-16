using Godot;

namespace TouhouWuxiaSurvivor.Combat.Weapons;

/// <summary>保存单颗普通弹或中心弹幕的通道、出生位置与飞行方向。</summary>
public readonly record struct ProjectileLaunchPlan(
    Vector2 Position,
    Vector2 Direction,
    PlayerProjectileChannel Channel);
