using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Combat.Targeting;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证自动射击按目标速度、实际弹速、出生偏移和寿命求得拦截方向，并为无解目标保留直瞄回退。
/// </summary>
public partial class PredictiveAimTest : Node
{
    /// <summary>依次执行横移、远离、不可追及、超寿命及 ECS 速度读取契约，任一偏差都以测试失败退出。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyPerpendicularInterception();
            VerifySpawnOffsetInterception();
            VerifyFallbackConditions();
            VerifyEcsMotionSnapshot();
            GD.Print("Predictive aim test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认横向移动目标会得到明显提前量，并在解析时刻与弹丸位置精确相交。</summary>
    private static void VerifyPerpendicularInterception()
    {
        Vector2 origin = Vector2.Zero;
        var target = new TargetMotion(new Vector2(100.0f, 0.0f), new Vector2(0.0f, 30.0f));
        Require(InterceptAimSolver.TrySolve(origin, target, 100.0f, 0.0f,
            ProjectileKinematicsPolicy.PlayerLifetimeSeconds,
            out Vector2 direction, out float time),
            "A reachable perpendicular target did not produce an intercept solution.");
        Vector2 projectileAtHit = origin + direction * 100.0f * time;
        Vector2 targetAtHit = target.Position + target.Velocity * time;
        Require(direction.Y > 0.25f && projectileAtHit.DistanceTo(targetAtHit) < 0.01f,
            "Perpendicular lead did not meet the moving target at the solved time.");
    }

    /// <summary>确认枪口出生偏移进入同一解析方程，远离目标不会因忽略十八像素前移而过度预判。</summary>
    private static void VerifySpawnOffsetInterception()
    {
        var target = new TargetMotion(new Vector2(100.0f, 0.0f), new Vector2(20.0f, 0.0f));
        Require(InterceptAimSolver.TrySolve(Vector2.Zero, target, 100.0f, 18.0f,
            ProjectileKinematicsPolicy.PlayerLifetimeSeconds,
            out Vector2 direction, out float time),
            "A reachable receding target did not produce an intercept solution.");
        float projectileDistance = 18.0f + 100.0f * time;
        Vector2 targetAtHit = target.Position + target.Velocity * time;
        Require(Math.Abs(time - 1.025f) < 0.001f &&
            (direction * projectileDistance).DistanceTo(targetAtHit) < 0.01f,
            "Spawn offset was not included in the receding-target intercept equation.");
    }

    /// <summary>确认速度更快的远离目标与寿命外目标不会产生伪解，并统一回退为当前直瞄方向。</summary>
    private static void VerifyFallbackConditions()
    {
        var escaping = new TargetMotion(new Vector2(100.0f, 0.0f), new Vector2(120.0f, 0.0f));
        Require(!InterceptAimSolver.TrySolve(Vector2.Zero, escaping, 100.0f, 18.0f,
            ProjectileKinematicsPolicy.PlayerLifetimeSeconds, out _, out _),
            "An escaping target faster than the projectile produced a false intercept.");
        Require(InterceptAimSolver.ResolveDirection(Vector2.Zero, escaping, 100.0f, 18.0f,
            ProjectileKinematicsPolicy.PlayerLifetimeSeconds).IsEqualApprox(Vector2.Right),
            "An unsolved target did not fall back to direct aim.");

        var tooFar = new TargetMotion(new Vector2(1000.0f, 0.0f), Vector2.Zero);
        Require(!InterceptAimSolver.TrySolve(Vector2.Zero, tooFar, 100.0f, 18.0f,
            ProjectileKinematicsPolicy.PlayerLifetimeSeconds, out _, out _),
            "A target beyond the projectile lifetime produced an unreachable intercept.");
    }

    /// <summary>确认 ECS 最近目标入口返回移动系统的权威速度，而不是只交付会导致落后的当前坐标。</summary>
    private static void VerifyEcsMotionSnapshot()
    {
        var enemies = new EnemyPool();
        enemies.Add(new Vector2(96.0f, 24.0f), CreateEnemy());
        EnemyComponent enemy = enemies.Get(0);
        enemy.Velocity = new Vector2(-12.0f, 48.0f);
        enemies.Set(0, enemy);
        var access = new EnemyTargetAccess();
        Require(access.TryFindNearestMotion(enemies, Vector2.Zero, 200.0f,
            out TargetMotion motion), "ECS motion query did not find its only living target.");
        Require(motion.Position.IsEqualApprox(enemy.Position) &&
            motion.Velocity.IsEqualApprox(enemy.Velocity),
            "ECS motion query discarded or changed the authoritative target velocity.");
    }

    /// <summary>建立不会移动或攻击的测试敌人，速度由测试显式写入以隔离查询语义。</summary>
    private static EnemyDefinition CreateEnemy() => new(
        EnemyArchetype.Fairy, "预判试靶", 100, 0.0f, 6.0f,
        1.0f, 0.0f, 0.0f, []);

    /// <summary>把预判契约偏差转换为包含明确原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
