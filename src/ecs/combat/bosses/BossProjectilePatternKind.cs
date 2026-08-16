namespace TouhouWuxiaSurvivor.Ecs.Combat.Bosses;

/// <summary>
/// 定义 Boss 弹幕系统可执行的五种空间语法；内容层负责把作品符卡几何映射到这些稳定运行类型。
/// </summary>
public enum BossProjectilePatternKind
{
    Orbit,
    Fan,
    Line,
    Ring,
    Backstab,
}
