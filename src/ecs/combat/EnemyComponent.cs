using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Core;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 保存高数量敌人的运行时数据；定义对象只作为不可变平衡数据被引用。
/// </summary>
public struct EnemyComponent
{
    /// <summary>创建一份可直接加入连续敌人池的数据。</summary>
    public EnemyComponent(EcsEntity entity, Vector2 position, EnemyDefinition definition)
    {
        Entity = entity;
        _position = new InterpolatedPosition2D(position);
        Velocity = Vector2.Zero;
        Definition = definition;
        Health = definition.MaxHealth;
        DeathTime = 0.0f;
        HurtTime = 0.0f;
        TouchCooldown = 0.0f;
        AiTimer = definition.AiProfile.ChargeInterval;
        ChargeTimeLeft = 0.0f;
        FireCooldown = 0.25f + entity.Value % 7 * 0.08f;
        OrbitDirection = (entity.Value & 1) == 0 ? 1.0f : -1.0f;
        BossPhase = BossBulletPhase.AimedFan;
        PatternAngle = entity.Value % 12 * Mathf.Pi / 6.0f;
        PatternDirection = (entity.Value & 1) == 0 ? 1.0f : -1.0f;
        ActiveSpellName = string.Empty;
        SpellAnnouncementTime = 0.0f;
        Alive = true;
    }

    /// <summary>获取实体句柄。</summary>
    public EcsEntity Entity;

    private InterpolatedPosition2D _position;

    /// <summary>获取或设置碰撞与 AI 使用的权威局部世界位置。</summary>
    public Vector2 Position
    {
        get => _position.Current;
        set => _position.Current = value;
    }

    /// <summary>在移动系统写入新位置前保存上一物理位置。</summary>
    public void BeginPhysicsStep() => _position.BeginPhysicsStep();

    /// <summary>同步平移前后物理位置，防止世界重定位被渲染成高速移动。</summary>
    public void Translate(Vector2 offset) => _position.Translate(offset);

    /// <summary>按渲染时物理分数读取平滑位置，不改变碰撞使用的权威位置。</summary>
    public readonly Vector2 GetRenderPosition(float fraction) => _position.Sample(fraction);

    /// <summary>获取或设置追踪、突进和绕行系统共享的当前速度。</summary>
    public Vector2 Velocity;

    /// <summary>获取敌人平衡定义。</summary>
    public EnemyDefinition Definition;

    /// <summary>获取或设置当前生命值。</summary>
    public int Health;

    /// <summary>获取或设置受击反馈剩余时间。</summary>
    public float HurtTime;

    /// <summary>获取或设置死亡文字反馈剩余时间。</summary>
    public float DeathTime;

    /// <summary>获取或设置接触伤害冷却。</summary>
    public float TouchCooldown;

    /// <summary>获取或设置移动 AI 的蓄势或状态剩余时间。</summary>
    public float AiTimer;

    /// <summary>获取或设置突进动作尚需保持锁定方向的时间。</summary>
    public float ChargeTimeLeft;

    /// <summary>获取或设置下一次普通射击或 Boss 弹幕允许发射前的时间。</summary>
    public float FireCooldown;

    /// <summary>获取实体稳定分配的顺逆时针方向，避免同类敌人完全重叠。</summary>
    public float OrbitDirection;

    /// <summary>获取或设置角色 Boss 当前生效的血量弹幕阶段。</summary>
    public BossBulletPhase BossPhase;

    /// <summary>获取或设置旋转弹幕的累计角度，使低血量阶段跨帧连续。</summary>
    public float PatternAngle;

    /// <summary>获取或设置交错旋转方向，每次发射后翻转形成正反双螺旋。</summary>
    public float PatternDirection;

    /// <summary>保存当前血量阶段正在演出的角色符卡简称；通用弹幕保持空字符串。</summary>
    public string ActiveSpellName;

    /// <summary>获取或设置符卡名演出剩余时间，归零后不再占用世界画面。</summary>
    public float SpellAnnouncementTime;

    /// <summary>获取是否仍然可以移动、受伤和被索敌。</summary>
    public bool Alive;
}
