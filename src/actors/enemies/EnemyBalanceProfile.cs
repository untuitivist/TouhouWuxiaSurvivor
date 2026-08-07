namespace TouhouWuxiaSurvivor.Actors.Enemies;

/// <summary>
/// 将敌人原始数值转换为玩家可读的威胁级、战斗定位和预计击破时间。
/// </summary>
public sealed class EnemyBalanceProfile
{
    public const float BaseWeaponInterval = 0.18f;
    public int ThreatRank { get; }
    public float ThreatScore { get; }
    public float BaseTimeToKill { get; }
    public string ThreatLabel { get; }
    public string CombatRole { get; }
    public string ArrivalPhase { get; }

    /// <summary>
    /// 保存由统一评估公式计算出的不可变策划结果。
    /// </summary>
    private EnemyBalanceProfile(
        int threatRank,
        float threatScore,
        float baseTimeToKill,
        string threatLabel,
        string combatRole,
        string arrivalPhase)
    {
        ThreatRank = threatRank;
        ThreatScore = threatScore;
        BaseTimeToKill = baseTimeToKill;
        ThreatLabel = threatLabel;
        CombatRole = combatRole;
        ArrivalPhase = arrivalPhase;
    }

    /// <summary>
    /// 按耐久、速度、体型与特殊死亡效果计算单体威胁，不把稀有度错误算作个体强度。
    /// </summary>
    public static EnemyBalanceProfile Evaluate(EnemyDefinition enemy)
    {
        float durability = MathF.Sqrt(enemy.MaxHealth / 3.0f) * 0.55f;
        float speed = enemy.MoveSpeed / 52.0f * 0.8f;
        float size = enemy.CollisionRadius / 7.0f * 0.25f;
        float special = enemy.ExplodesOnDeath ? 0.7f : 0.0f;
        float score = durability + speed + size + special;
        int rank = score switch
        {
            < 1.55f => 1,
            < 1.90f => 2,
            < 2.20f => 3,
            < 2.60f => 4,
            _ => 5,
        };
        string[] labels = ["寻常", "警戒", "凶险", "强敌", "灾厄"];
        return new EnemyBalanceProfile(
            rank,
            score,
            enemy.MaxHealth * BaseWeaponInterval,
            labels[rank - 1],
            GetCombatRole(enemy),
            GetArrivalPhase(enemy.UnlockTime));
    }

    /// <summary>
    /// 根据最显著的实战参数确定敌人定位，供图鉴和后续刷怪编队共同使用。
    /// </summary>
    private static string GetCombatRole(EnemyDefinition enemy)
    {
        if (enemy.ExplodesOnDeath)
        {
            return "爆破型";
        }

        if (enemy.MaxHealth >= 20)
        {
            return "重装型";
        }

        if (enemy.MoveSpeed >= 58.0f)
        {
            return "突进型";
        }

        if (enemy.CollisionRadius >= 9.0f)
        {
            return "压迫型";
        }

        return "游击型";
    }

    /// <summary>
    /// 将解锁秒数归入开局、成势、围攻和决战四个生存阶段。
    /// </summary>
    private static string GetArrivalPhase(float unlockTime) => unlockTime switch
    {
        < 45.0f => "开局",
        < 120.0f => "成势",
        < 240.0f => "围攻",
        _ => "决战",
    };
}
