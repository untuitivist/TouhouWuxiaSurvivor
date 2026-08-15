using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证符卡投射物以稳定实体身份追踪移动目标，并在目标失效或不存在时保持安全落点语义。
/// </summary>
public partial class SpellCardTrackingTest : Node
{
    private static readonly PackedScene OrbScene = GD.Load<PackedScene>(
        "res://src/gameplay/spellcards/effects/FantasySealOrb.tscn");

    /// <summary>依次覆盖正式 ECS、目标失效、固定点回退、池交换和旧节点兼容追踪。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyMovingEcsTarget();
            VerifyLostTargetDoesNotRetarget();
            VerifyFixedPointFallback();
            VerifyPoolSwapKeepsIdentity();
            VerifyMovingLegacyTarget();
            GD.Print("Spell card tracking test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>移动 ECS 世界中的原目标，确认灵玉更新坐标并命中而不是停在施放瞬间位置。</summary>
    private void VerifyMovingEcsTarget()
    {
        var world = new EcsCombatWorld();
        var enemies = new Node2D();
        AddChild(world);
        AddChild(enemies);
        world.SpawnEnemy(new Vector2(100, 0), CreateEnemy(5));
        var backend = new SpellCardCombatBackend(enemies, world);
        SpellCardTargetReference target = backend.SelectCandidateTargets(
            Vector2.Zero, 300.0f).Single();
        FantasySealOrb orb = CreateOrb(backend, target.InitialPosition, 5, target);
        world.Rebase(new Vector2(-80, 0));
        AdvanceToImpact(orb, 1.0, 0.8, 0.01);
        Require(world.AliveEnemyCount == 0,
            "ECS orb did not follow the same target after its position changed.");
        world.Free();
        enemies.Free();
    }

    /// <summary>原目标死亡后在旧落点放入替身，确认灵玉保留末位置但不会把身份伤害转嫁给替身。</summary>
    private void VerifyLostTargetDoesNotRetarget()
    {
        var world = new EcsCombatWorld();
        var enemies = new Node2D();
        AddChild(world);
        AddChild(enemies);
        var backend = new SpellCardCombatBackend(enemies, world);
        var original = world.SpawnEnemy(new Vector2(100, 0), CreateEnemy(1));
        SpellCardTargetReference target = backend.SelectCandidateTargets(
            Vector2.Zero, 200.0f).Single();
        FantasySealOrb orb = CreateOrb(backend, target.InitialPosition, 5, target);
        Require(world.DamageEnemy(original, 1), "Could not invalidate the tracked enemy.");
        var replacement = world.SpawnEnemy(new Vector2(100, 0), CreateEnemy(5));
        AdvanceToImpact(orb, 1.0, 0.01);
        Require(world.DamageEnemy(replacement, 4) &&
            world.TryGetEnemyPosition(replacement, out _),
            "Lost tracking target redirected damage to a replacement enemy.");
        world.Free();
        enemies.Free();
    }

    /// <summary>没有绑定敌人身份时仍按固定几何落点飞行并安全回收，不要求临时重新选敌。</summary>
    private void VerifyFixedPointFallback()
    {
        var world = new EcsCombatWorld();
        var enemies = new Node2D();
        AddChild(world);
        AddChild(enemies);
        var backend = new SpellCardCombatBackend(enemies, world);
        Require(backend.SelectCandidateTargets(Vector2.Zero, 100.0f).Count == 0,
            "Empty fallback fixture unexpectedly found an enemy.");
        FantasySealOrb orb = CreateOrb(backend, new Vector2(100, 0), 5, null);
        AdvanceToImpact(orb, 1.0, 0.01);
        Require(orb.IsQueuedForDeletion(),
            "Fixed-point orb did not safely recycle when no enemy existed.");
        world.Free();
        enemies.Free();
    }

    /// <summary>尾部交换后按句柄读取原尾敌人，证明追踪不依赖会随紧凑化改变的数组索引。</summary>
    private static void VerifyPoolSwapKeepsIdentity()
    {
        var pool = new EnemyPool();
        pool.Add(Vector2.Zero, CreateEnemy(5));
        var tracked = pool.Add(new Vector2(80, 0), CreateEnemy(5));
        pool.RemoveSwap(0);
        pool.TrimLast();
        Require(pool.TryGetAlive(tracked, out int index, out EnemyComponent enemy) &&
            index == 0 && enemy.Position.IsEqualApprox(new Vector2(80, 0)),
            "Enemy handle did not survive compact-pool swap removal.");
    }

    /// <summary>移动旧版 EnemyActor 节点，确认兼容后端同样按节点身份读取新位置并造成伤害。</summary>
    private void VerifyMovingLegacyTarget()
    {
        var enemies = new Node2D();
        var idleTarget = new Node2D();
        AddChild(enemies);
        AddChild(idleTarget);
        var actor = GD.Load<PackedScene>("res://src/actors/enemies/EnemyActor.tscn")
            .Instantiate<EnemyActor>();
        actor.Configure(CreateEnemy(5), idleTarget);
        enemies.AddChild(actor);
        actor.GlobalPosition = new Vector2(100, 0);
        var backend = new SpellCardCombatBackend(enemies, null);
        SpellCardTargetReference target = backend.SelectCandidateTargets(
            Vector2.Zero, 300.0f).Single();
        FantasySealOrb orb = CreateOrb(backend, target.InitialPosition, 5, target);
        actor.GlobalPosition = new Vector2(180, 0);
        AdvanceToImpact(orb, 1.0, 0.8, 0.01);
        Require(!actor.IsAlive,
            "Legacy orb did not retain and follow the original EnemyActor identity.");
        enemies.Free();
        idleTarget.Free();
    }

    /// <summary>建立关闭自动物理更新的真实灵玉，使测试可用确定步长驱动跨帧追踪。</summary>
    private FantasySealOrb CreateOrb(
        SpellCardCombatBackend backend,
        Vector2 targetPosition,
        int damage,
        SpellCardTargetReference? target)
    {
        FantasySealOrb orb = OrbScene.Instantiate<FantasySealOrb>();
        orb.ConfigureImpact(backend, targetPosition, damage, 100.0f, 5.0f,
            5.0f, 0, "base", "追踪测试",
            SpellCardGeometryKind.Orbit, 0.0f, target);
        AddChild(orb);
        orb.SetPhysicsProcess(false);
        orb.GlobalPosition = Vector2.Zero;
        return orb;
    }

    /// <summary>用显式物理步长推进灵玉，避免测试结果依赖机器帧率和真实经过时间。</summary>
    private static void AdvanceToImpact(FantasySealOrb orb, params double[] deltas)
    {
        foreach (double delta in deltas)
        {
            orb._PhysicsProcess(delta);
        }
    }

    /// <summary>建立静止、无掉落且耐久可控的测试敌人，隔离移动 AI 与内容目录。</summary>
    private static EnemyDefinition CreateEnemy(int health) => new(
        EnemyArchetype.Fairy, "追踪测试靶", health, 0.0f, 5.0f,
        0.0f, 0.0f, 0.0f, []);

    /// <summary>把追踪契约失败转换为包含具体语义的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
