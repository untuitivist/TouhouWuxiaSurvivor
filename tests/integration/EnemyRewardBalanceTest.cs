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
/// 验证敌人接触伤害、Boss 缩放、灵息奖励与遭遇恢复期使用同一条无尽时间轴且没有早期跳变。
/// </summary>
public partial class EnemyRewardBalanceTest : Node
{
    /// <summary>依次执行纯公式和 ECS 遭遇断言，任一回归都以非零退出码报告明确原因。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyContactDamageRounding();
            VerifyBossScalingOwnership();
            VerifySpiritRewards();
            VerifyScaledHealthDoesNotPayTwice();
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

    /// <summary>确认十秒的一点基础伤害仍为一点，并在长局保持非递减且最终跨过整数阈值。</summary>
    private static void VerifyContactDamageRounding()
    {
        var enemy = new EnemyDefinition(EnemyArchetype.Fairy, "测试妖精", 40, 30.0f,
            7.0f, 1.0f, 0.0f, 0.0f, [], contactDamage: 1);
        int opening = EnemyDifficultyScaler.Scale(enemy, 0).ContactDamage;
        int tenSeconds = EnemyDifficultyScaler.Scale(enemy,
            EnemyDifficultyScaler.GetTier(10.0)).ContactDamage;
        Require(opening == 1 && tenSeconds == 1,
            "One contact damage jumped at the ten-second tier.");
        int previous = opening;
        for (int minute = 1; minute <= 240; minute++)
        {
            int current = EnemyDifficultyScaler.Scale(enemy,
                EnemyDifficultyScaler.GetTier(minute * 60.0)).ContactDamage;
            Require(current >= previous, $"Contact damage decreased at minute {minute}.");
            previous = current;
        }
        Require(previous > opening, "Contact damage never crossed an endless integer tier.");
    }

    /// <summary>确认 Boss 工厂只缩放生命与伤害，移速只由移动系统消费一次共享倍率。</summary>
    private static void VerifyBossScalingOwnership()
    {
        CharacterDefinition character = CharacterCatalog.All.First(character =>
            character.SourcePackId != ContentPackCatalog.Base.Id);
        const double elapsedSeconds = 600.0;
        EnemyDefinition boss = BossDefinitionFactory.Create(character, elapsedSeconds);
        Require(Mathf.IsEqualApprox(boss.MoveSpeed, character.BossProfile.MoveSpeed),
            "Boss factory multiplied movement speed before the movement system.");
        int expectedDamage = EnemyDifficultyScaler.ScaleContactDamage(
            character.BossProfile.ContactDamage,
            EndlessDifficultyCurve.EvaluateSeconds(elapsedSeconds, int.MaxValue)
                .EnemyDamageMultiplier);
        Require(boss.ContactDamage == expectedDamage,
            "Boss contact damage diverged from the shared enemy formula.");
        VerifySingleMovementMultiplier(boss, elapsedSeconds);
    }

    /// <summary>推进一秒 Boss 移动，确认位移等于基础速度乘一次共享速度倍率。</summary>
    private static void VerifySingleMovementMultiplier(EnemyDefinition boss, double elapsedSeconds)
    {
        var pool = new EnemyPool();
        pool.Add(new Vector2(-300.0f, 0.0f), boss);
        new EnemyMovementSystem().Step(pool, Vector2.Zero, 1.0f, elapsedSeconds, _ => { });
        float displacement = pool.Get(0).Position.DistanceTo(new Vector2(-300.0f, 0.0f));
        float expected = boss.MoveSpeed * (float)
            EndlessDifficultyCurve.EvaluateSeconds(elapsedSeconds, int.MaxValue)
                .EnemySpeedMultiplier;
        Require(Mathf.IsEqualApprox(displacement, expected),
            "Boss movement applied the endless speed multiplier more than once.");
    }

    /// <summary>确认 Boss 奖励高于普通怪八点上限，且普通怪和 Boss 的时间奖励均在长局单调增长。</summary>
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
        VerifyRewardMonotonic(normal);
        VerifyRewardMonotonic(boss);
    }

    /// <summary>在代表性长局时刻比较奖励，验证共享奖励倍率被实际消费且不会倒退。</summary>
    private static void VerifyRewardMonotonic(EnemyDefinition enemy)
    {
        int previous = 0;
        foreach (double minutes in new[] { 0.0, 2.0, 10.0, 60.0, 240.0 })
        {
            int current = SpiritValueCalculator.CalculateForElapsedTime(enemy, minutes * 60.0);
            Require(current >= previous,
                $"Spirit reward decreased at {minutes} minutes for {enemy.DisplayName}.");
            previous = current;
        }
        Require(previous > SpiritValueCalculator.CalculateForElapsedTime(enemy, 0.0),
            $"Spirit reward did not grow endlessly for {enemy.DisplayName}.");
    }

    /// <summary>
    /// 比较目录敌人与正式出生缩放敌人，确认相同击破时刻只消费一次时间奖励倍率，生命缩放不再暗加奖励。
    /// </summary>
    private static void VerifyScaledHealthDoesNotPayTwice()
    {
        EnemyDefinition authored = EnemyCatalog.All.First(enemy => !enemy.IsBoss);
        const double elapsedSeconds = 3600.0;
        EnemyDefinition scaled = EnemyDifficultyScaler.Scale(authored,
            EnemyDifficultyScaler.GetTier(elapsedSeconds));
        Require(scaled.MaxHealth > authored.MaxHealth &&
            scaled.BaseMaxHealth == authored.MaxHealth,
            "Runtime enemy lost its authored durability while scaling health.");
        Require(SpiritValueCalculator.CalculateForElapsedTime(scaled, elapsedSeconds) ==
            SpiritValueCalculator.CalculateForElapsedTime(authored, elapsedSeconds),
            "Scaled health applied the endless reward curve a second time.");
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
