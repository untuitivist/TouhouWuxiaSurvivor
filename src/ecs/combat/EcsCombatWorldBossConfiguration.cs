using TouhouWuxiaSurvivor.Ecs.Combat.Bosses;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 提供角色 Boss 的低频内容配置入口，使战斗世界核心文件不依赖内容目录实现。
/// </summary>
public partial class EcsCombatWorld
{
    /// <summary>注入内容层预先解析的 Boss 符卡攻击；ECS 只消费纯数据档案。</summary>
    public void ConfigureBossAttacks(IBossAttackResolver? resolver) =>
        _enemyProjectiles.ConfigureBossAttacks(resolver);
}
