using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Gameplay.Difficulty;
using TouhouWuxiaSurvivor.Gameplay.Encounters;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证普通怪与 Boss 属性只由种类档案决定，并锁定固定灵息奖励与遭遇恢复期。
/// </summary>
public partial class EnemyRewardBalanceTest : Node
{
    /// <summary>依次执行纯公式和 ECS 遭遇断言，任一回归都以非零退出码报告明确原因。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyFixedMonsterStats();
            VerifyBossProfileOwnership();
            VerifySpiritRewards();
            VerifyEncounterRecovery();
            GD.Print("Enemy reward balance test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认任意历史时间档都返回同一怪物定义，不改变生命、速度、伤害或奖励。</summary>
    private static void VerifyFixedMonsterStats()
    {
        var enemy = new EnemyDefinition(EnemyArchetype.Fairy, "测试妖精", 40, 30.0f,
            7.0f, 1.0f, 0.0f, 0.0f, [], contactDamage: 1);
        int opening = EnemyDifficultyScaler.Scale(enemy, 0).ContactDamage;
        EnemyDefinition late = EnemyDifficultyScaler.Scale(enemy,
            EnemyDifficultyScaler.GetTier(240.0 * 60.0));
        Require(ReferenceEquals(enemy, late) && opening == 1 &&
            late.MaxHealth == 40 && Mathf.IsEqualApprox(late.MoveSpeed, 30.0f) &&
            late.ContactDamage == 1 && SpiritValueCalculator.Calculate(late) ==
                SpiritValueCalculator.Calculate(enemy),
            "A global stage changed fixed monster attributes or rewards.");
    }

    /// <summary>确认 Boss 工厂完整使用角色档案，生命阶段只切换招式而不会放大全局属性。</summary>
    private static void VerifyBossProfileOwnership()
    {
        CharacterDefinition character = CharacterCatalog.All.First(character =>
            character.SourcePackId != ContentPackCatalog.Base.Id);
        EnemyDefinition boss = BossDefinitionFactory.Create(character);
        Require(boss.MaxHealth == Mathf.CeilToInt(character.BossProfile.MaxHealth) &&
            Mathf.IsEqualApprox(boss.MoveSpeed, character.BossProfile.MoveSpeed) &&
            boss.ContactDamage == EnemyDifficultyScaler.NormalizeContactDamage(
                character.BossProfile.ContactDamage),
            "Boss factory changed the authored character profile.");
        VerifyFixedMovement(boss);
    }

    /// <summary>推进一秒 Boss 移动，确认位移只消费一次档案速度而不读取生存时间。</summary>
    private static void VerifyFixedMovement(EnemyDefinition boss)
    {
        var pool = new EnemyPool();
        pool.Add(new Vector2(-300.0f, 0.0f), boss);
        new EnemyMovementSystem().Step(pool, Vector2.Zero, 1.0f, _ => { });
        float displacement = pool.Get(0).Position.DistanceTo(new Vector2(-300.0f, 0.0f));
        Require(Mathf.IsEqualApprox(displacement, boss.MoveSpeed),
            "Boss movement changed its authored speed through a global stage multiplier.");
    }

    /// <summary>确认 Boss 奖励高于普通怪八点上限，且同种怪物的奖励不会随阶段变化。</summary>
    private static void VerifySpiritRewards()
    {
        var normal = new EnemyDefinition(EnemyArchetype.Fairy, "普通怪", 64, 20.0f,
            6.0f, 1.0f, 0.0f, 0.0f, []);
        var boss = new EnemyDefinition(EnemyArchetype.CharacterBoss, "角色 Boss", 900,
            30.0f, 18.0f, 0.0f, 0.0f, 1.0f, [], isBoss: true,
            characterId: "character_reward_test");
        Require(SpiritValueCalculator.Calculate(normal) == 8 &&
            SpiritValueCalculator.Calculate(boss) > 8,
            "Boss spirit reward still shares the ordinary eight-point cap.");
        int normalReward = SpiritValueCalculator.Calculate(normal);
        int bossReward = SpiritValueCalculator.Calculate(boss);
        Require(normalReward == 8 && bossReward > normalReward,
            "Fixed monster rewards changed between repeated evaluations.");
    }

    /// <summary>模拟生成与结束，确认下一场只会从结束时刻开始计算完整间隔且重复结束无效。</summary>
    private static void VerifyEncounterRecovery()
    {
        CharacterDefinition selected = CharacterCatalog.Default;
        ContentPackDefinition pack = ContentPackCatalog.All.First();
        var content = new ContentPackSelection([pack.Id]);
        var context = new RunContentContext(content, new CharacterSelection(selected));
        var world = new EcsCombatWorld();
        var director = new BossEncounterDirector { EncounterIntervalSeconds = 180.0 };
        director.Configure(world, context, () => Vector2.Zero);
        Require(director.TrySpawn(new Vector2(300.0f, 0.0f), 120.0, 0),
            "Boss encounter could not start for recovery test.");
        Require(double.IsPositiveInfinity(director.NextEncounterSeconds),
            "Boss encounter scheduled its successor at spawn time.");
        Require(director.ResolveActiveEncounter(500.0) &&
            Math.Abs(director.NextEncounterSeconds - 680.0) < 0.001 &&
            !director.ResolveActiveEncounter(600.0),
            "Boss recovery interval was not measured once from encounter resolution.");
        director._ExitTree();
        director.Free();
        world.Free();
    }

    /// <summary>将任一敌人奖励策划违约转换为包含具体原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
