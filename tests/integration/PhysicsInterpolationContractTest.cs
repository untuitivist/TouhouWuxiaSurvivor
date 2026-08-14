using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Ecs.Core;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证节点和 ECS 运动共享固定物理步、渲染插值及无限世界重定位的连续性契约。
/// </summary>
public partial class PhysicsInterpolationContractTest : Node
{
    /// <summary>顺序执行纯数据插值与三类正式移动系统，失败时以非零退出码结束。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyProjectInterpolationEnabled();
            VerifyPositionSamplingAndTranslation();
            VerifyEnemyMovementSnapshot();
            VerifyProjectileMovementSnapshot();
            VerifySpiritMovementSnapshot();
            GD.Print("Physics interpolation contract test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认所有 Node2D 默认启用 Godot 固定步物理插值。</summary>
    private static void VerifyProjectInterpolationEnabled()
    {
        bool enabled = ProjectSettings.GetSetting(
            "physics/common/physics_interpolation", false).AsBool();
        Require(enabled, "Project physics interpolation is not enabled.");
    }

    /// <summary>确认中点取样正确、比例被钳制，且重定位同时平移轨迹两端。</summary>
    private static void VerifyPositionSamplingAndTranslation()
    {
        var position = new InterpolatedPosition2D(new Vector2(10.0f, 20.0f));
        position.BeginPhysicsStep();
        position.Current = new Vector2(30.0f, 60.0f);
        Require(position.Sample(0.0f).IsEqualApprox(new Vector2(10.0f, 20.0f)),
            "Interpolation did not retain the previous fixed-step position.");
        Require(position.Sample(0.5f).IsEqualApprox(new Vector2(20.0f, 40.0f)),
            "Interpolation midpoint is incorrect.");
        Require(position.Sample(2.0f).IsEqualApprox(position.Current),
            "Interpolation fraction was not clamped before sampling.");

        Vector2 before = position.Sample(0.35f);
        var offset = new Vector2(-8192.0f, 4096.0f);
        position.Translate(offset);
        Require(position.Sample(0.35f).IsEqualApprox(before + offset),
            "World rebasing changed the shape of the interpolation path.");
    }

    /// <summary>确认正式敌人 AI 在移动前保存位置，而碰撞仍读取移动后的权威位置。</summary>
    private static void VerifyEnemyMovementSnapshot()
    {
        var pool = new EnemyPool();
        var definition = new EnemyDefinition(
            EnemyArchetype.Fairy, "插值敌人", 10, 60.0f, 6.0f,
            0.0f, 0.0f, 0.0f, []);
        Vector2 start = new(120.0f, 0.0f);
        pool.Add(start, definition);
        new EnemyMovementSystem().Step(pool, Vector2.Zero, 0.25f, _ => { });
        EnemyComponent enemy = pool.Get(0);
        VerifyMovedComponent(start, enemy.Position,
            enemy.GetRenderPosition(0.0f), enemy.GetRenderPosition(0.5f), "enemy");
    }

    /// <summary>确认高频投射物移动系统保存上一位置，且半帧取样落在物理轨迹中间。</summary>
    private static void VerifyProjectileMovementSnapshot()
    {
        var pool = new ProjectilePool();
        Vector2 start = new(8.0f, 12.0f);
        Require(pool.TryAdd(start, Vector2.Right, 80.0f, 1,
            ProjectileFaction.Player, 2.0f, 4.0f, 0, out _),
            "Projectile fixture could not be created.");
        new ProjectileMovementSystem().Step(pool, 0.25f);
        ProjectileComponent projectile = pool.Get(0);
        VerifyMovedComponent(start, projectile.Position,
            projectile.GetRenderPosition(0.0f), projectile.GetRenderPosition(0.5f),
            "projectile");
    }

    /// <summary>确认吸附灵息同样参与渲染插值，而经验交付仍使用当前物理位置。</summary>
    private static void VerifySpiritMovementSnapshot()
    {
        Vector2 start = new(100.0f, 0.0f);
        var spirits = new List<SpiritComponent>
        {
            new(new EcsEntity(1), start, 1),
        };
        new SpiritSystem().Step(spirits, Vector2.Zero, 200.0f, 0.1f, _ => { });
        SpiritComponent spirit = spirits[0];
        VerifyMovedComponent(start, spirit.Position,
            spirit.GetRenderPosition(0.0f), spirit.GetRenderPosition(0.5f), "spirit");
    }

    /// <summary>比较任意移动组件的起点、终点和半帧取样，统一错误诊断文本。</summary>
    private static void VerifyMovedComponent(
        Vector2 start,
        Vector2 current,
        Vector2 previousSample,
        Vector2 halfSample,
        string label)
    {
        Require(!current.IsEqualApprox(start), $"{label} did not move during its physics step.");
        Require(previousSample.IsEqualApprox(start),
            $"{label} did not retain its previous physics position.");
        Require(halfSample.IsEqualApprox(start.Lerp(current, 0.5f)),
            $"{label} render position is not the fixed-step midpoint.");
    }

    /// <summary>将契约失败转换为包含具体原因的异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
