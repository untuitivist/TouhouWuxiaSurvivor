using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 投射物 ECS 的 Godot 桥接节点；只负责时间步、绘制和对外生成接口，不为单颗投射物创建节点。
/// </summary>
public partial class ProjectileEcsRuntime : Node2D
{
    private readonly ProjectilePool _pool = new();
    private readonly ProjectileMovementSystem _movement = new();
    private readonly ProjectileCollisionSystem _collision = new();
    private readonly InternalVisualCatalog _visuals = new();
    private readonly SpellCardProjectileVisualResolver _projectileVisuals = new();
    private Node2D? _enemyContainer;
    private int _visualSourceId;

    /// <summary>获取当前活跃的 ECS 投射物数量，供 HUD 和性能测试读取。</summary>
    public int ActiveCount => _pool.Count;

    /// <summary>获取从本局开始累计生成的投射物数量。</summary>
    public int TotalSpawned { get; private set; }

    /// <summary>
    /// 绑定敌人容器和玩家内容包来源；素材缺失时绘制纯色点而不借用其他内容包。
    /// </summary>
    public void Configure(Node2D enemyContainer, string visualSourcePackId)
    {
        _enemyContainer = enemyContainer;
        _visualSourceId = ProjectileVisualSourceBindingCatalog.GetBindingId(
            visualSourcePackId);
        _projectileVisuals.Configure(_visuals);
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        QueueRedraw();
    }

    /// <summary>在 ECS 连续池中生成一颗投射物，并返回轻量实体句柄。</summary>
    public void Spawn(
        Vector2 position,
        Vector2 direction,
        float speed,
        int damage,
        int maximumHits = 1,
        int secondaryHitDamage = -1,
        int visualVariant = 0)
    {
        if (_pool.TryAdd(position, direction, speed, damage,
                ProjectileFaction.Player, ProjectileKinematicsPolicy.PlayerLifetimeSeconds,
                4.0f, visualVariant, out _, maximumHits,
                secondaryHitDamage, visualSourceId: _visualSourceId))
        {
            TotalSpawned++;
        }
        QueueRedraw();
    }

    /// <summary>
    /// 让原点重定位同步修改数据坐标；运行时节点本身保持在零点，避免坐标系分裂。
    /// </summary>
    public void Rebase(Vector2 offset)
    {
        for (int index = 0; index < _pool.Count; index++)
        {
            ProjectileComponent projectile = _pool.Get(index);
            projectile.Position -= offset;
            _pool.Set(index, projectile);
        }
    }

    /// <summary>先运行纯数据系统，再请求一次批量重绘。</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (_enemyContainer is null)
        {
            return;
        }

        _movement.Step(_pool, (float)delta);
        _collision.Resolve(_pool, _enemyContainer);
        QueueRedraw();
    }

    /// <summary>用一张共享纹理绘制所有投射物，避免每颗子弹产生 Sprite2D 和 Area2D。</summary>
    public override void _Draw()
    {
        _pool.ForEach(projectile =>
        {
            SpellBulletStyleKind style = ProjectileBulletStylePolicy.Resolve(
                projectile.Faction, projectile.VisualVariant);
            if (_projectileVisuals.TryResolveSource(projectile.VisualSourceId, style,
                    projectile.VisualVariant, out Texture2D texture,
                    out SpellBulletVisualSelection selection, out _))
            {
                ProjectileVisualDrawHelper.Draw(this, texture, selection,
                    projectile.Position, projectile.Velocity);
            }
            else
            {
                Rect2 destination = new(projectile.Position - new Vector2(4.0f, 4.0f), new Vector2(8.0f, 8.0f));
                DrawRect(destination, new Color(0.95f, 0.86f, 0.48f), true);
            }
        });
    }
}
