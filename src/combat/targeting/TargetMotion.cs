using Godot;

namespace TouhouWuxiaSurvivor.Combat.Targeting;

/// <summary>
/// 保存自动武器在一次射击决策中使用的目标位置与权威速度，使节点和 ECS 目标共用同一预判输入。
/// </summary>
public readonly record struct TargetMotion(Vector2 Position, Vector2 Velocity);
