using System.Numerics;
using Rebirth.Core;
using Rebirth.Diagnostics;
using static Rebirth.Tests.MarisaGrowthTests;

namespace Rebirth.Tests;

internal static class LongRunTests
{
    private static void Check(bool condition, string message) => MarisaGrowthTests.Check(condition, message);

    public static void Identities()
    {
        Check(CharacterCatalog.Get(HeroKind.Reimu).CharacterId == "character_base_00", "Reimu keeps her legacy identity");
        Check(CharacterCatalog.Get(HeroKind.Marisa).CharacterId == "character_th02_soew_02", "Marisa keeps her legacy identity");
        foreach (var hero in Enum.GetValues<HeroKind>())
        {
            var candidates = CharacterCatalog.BossCandidates(hero);
            Check(candidates.Count == 1 && candidates[0].Id != hero, "The player is excluded per run");
            Check(CharacterCatalog.BossCandidates(hero, [hero]).Count == 0 && CharacterCatalog.BossCandidates(hero, []).Count == 0, "An empty enabled pool never falls back to the player");
            var run = new RunState(hero, 42);
            var boss = run.SpawnEnemy(EnemyKind.Boss, new(300, 0));
            Check(boss.Character == candidates[0].Id && boss.Abilities?.Hero == boss.Character, "Boss runtime uses the canonical opposing profile");
            Check(!ReferenceEquals(run.Build, boss.Abilities!.Build) && !ReferenceEquals(run.Marisa, boss.Abilities.Marisa), "Player and Boss state are independent");
            var other = new RunState(hero, 42).SpawnEnemy(EnemyKind.Boss, new(300, 0));
            boss.Abilities.ShotCooldown = 0;
            Check(other.Abilities!.ShotCooldown == 1.5f && boss.MaxHealth == candidates[0].Boss.Health, "No encounter state leaks");
        }
    }

    public static void Stances()
    {
        foreach (var hero in Enum.GetValues<HeroKind>())
        foreach (var chosenIndex in new[] { 0, 1 })
        {
            var build = new BuildState(hero);
            var stances = UpgradeCatalog.All.Where(upgrade => upgrade.Owner == hero && upgrade.Kind == UpgradeKind.Stance).ToArray();
            Check(stances.Length == 2 && stances.All(upgrade => !build.CanChoose(upgrade, 100)), "A fresh run cannot bypass the fourth-growth gate");
            var refine = hero == HeroKind.Reimu ? "reimu.ofuda.refine" : "marisa.stars.refine";
            for (var index = 0; index < 3; index++) Check(build.TryApply(UpgradeCatalog.Get(refine), 100), "Prepare three invested choices");
            var offers = UpgradeOffers.Create(build, 5, new(42));
            Check(offers.Count == 3 && offers.Take(2).SequenceEqual(stances) && offers[2].Kind != UpgradeKind.Stance, "Both stances and one real growth deferral are visible");
            Check(build.TryApply(offers[2], 100) && build.Stance == MainlineStance.None, "Deferral grants ordinary growth without choosing a stance");
            Check(UpgradeOffers.Create(build, 6, new(9)).Take(2).SequenceEqual(stances), "Deferred stances remain reachable");
            var selected = stances[chosenIndex];
            Check(!build.TryApply(selected with { Name = "forged" }, 100), "Forged upgrade objects cannot bypass catalog authority");
            Check(build.TryApply(selected, 100) && !build.TryApply(stances[1 - chosenIndex], 100) && !build.TryApply(selected, 100), "The opposite stance and duplicate choice are rejected");
            for (var seed = 0; seed < 30; seed++)
                Check(UpgradeOffers.Create(build, 100, new(seed)).All(upgrade => upgrade.Kind != UpgradeKind.Stance), "Excluded routes never return in offers");
            var repeat = UpgradeCatalog.Get(hero == HeroKind.Reimu ? MainlineGrowth.OfudaPower : MainlineGrowth.StarPower);
            for (var rank = 0; rank < 1000; rank++) Check(build.TryApply(repeat, 100), "Mainline growth stays effective beyond finite abilities");
            Check(build.Stance == selected.Stance && float.IsFinite(MainlineGrowth.Power(build)), "Extended growth preserves the chosen identity");
        }
        var tempo = new BuildState(HeroKind.Reimu);
        var rhythm = UpgradeCatalog.Get(MainlineGrowth.OfudaTempo);
        for (var rank = 0; rank < MainlineGrowth.TempoLimit; rank++) Check(tempo.TryApply(rhythm, 100), "Finite tempo rank");
        Check(!tempo.TryApply(rhythm, 100), "Tempo cannot erase the charged rhythm through infinite fire rate");
    }

    public static void Cadence()
    {
        var stats = new List<AbilityStats>();
        foreach (var stance in new[] { MainlineGrowth.RapidOfuda, MainlineGrowth.ChargedOfuda })
        {
            var build = new BuildState(HeroKind.Reimu);
            for (var index = 0; index < 3; index++) build.TryApply(UpgradeCatalog.Get("reimu.ofuda.refine"), 100);
            build.TryApply(UpgradeCatalog.Get(stance), 100);
            stats.Add(MainlineGrowth.OfudaStats(build));
        }
        Check(stats[1].Interval / stats[0].Interval > 3 && stats[1].Damage / stats[0].Damage > 3, "Ofuda stances have distinctly different windows");
        var ratio = stats[1].Damage / stats[1].Interval / (stats[0].Damage / stats[0].Interval);
        Check(ratio is > 0.95f and < 1.05f, "Cadence choice does not gift a superior nominal DPS route");
        var charged = new RunState(HeroKind.Reimu, 42);
        Learn(charged, "reimu.ofuda.refine", "reimu.ofuda.refine", "reimu.ofuda.refine", MainlineGrowth.ChargedOfuda);
        Target(charged, new(300, 0));
        charged.Step(default);
        Check(charged.Projectiles.Count > 0 && charged.Projectiles.All(projectile => projectile.Pierce == 1), "Charged ofuda penetrates one extra target instead of inflating single-target DPS or adding entities");
        Check(MainlineGrowth.StarAgeMultiplier(MainlineStance.YoungStars, 8, 7) > MainlineGrowth.StarAgeMultiplier(MainlineStance.MatureStars, 8, 7), "Young stars win early");
        Check(MainlineGrowth.StarAgeMultiplier(MainlineStance.YoungStars, 8, 2) < MainlineGrowth.StarAgeMultiplier(MainlineStance.MatureStars, 8, 2), "Mature stars win late");
        var run = new RunState(HeroKind.Marisa, 42);
        MarisaProjectileSystem.Cast(run, 1);
        var original = run.Stars[0];
        Learn(run, UpgradeCatalog.StarMass, UpgradeCatalog.StarLifetime, MainlineGrowth.StarPower, MainlineGrowth.MatureStars);
        MarisaProjectileSystem.Cast(run, 1);
        Check(run.Stars[0].Mass == original.Mass && run.Stars[0].Life == original.Life && run.Stars[0].Stance == MainlineStance.None, "Old stars are never resampled, renewed or retroactively restanced");
        Check(run.Stars[1].Stance == MainlineStance.MatureStars && run.Stars[1].Duration > original.Duration, "Only new stars use the new growth snapshot");
    }

    public static void Factions()
    {
        var run = new RunState(HeroKind.Reimu, 42);
        var boss = run.SpawnEnemy(EnemyKind.Boss, new(300, 0));
        var state = boss.Abilities!;
        var target = Target(run, new(0, 0));
        var before = run.Health;
        run.Stars.Add(new() { OwnerId = boss.Id, Hostile = true, Position = Vector2.Zero, Mass = 2, Life = 8, Duration = 8, DamageRate = 10 });
        run.World.Grid.Rebuild(run.Enemies);
        MarisaProjectileSystem.Step(run);
        Check(run.Health < before && run.Invulnerability == 0 && target.Health == target.MaxHealth && boss.Health == boss.MaxHealth, "Enemy tearing damages only the player without gifting contact invulnerability");
        run.Stars[0].Position = new(80, 0);
        run.Marisa.GravityTick = 0;
        StarGravitySystem.Step(run);
        Check(run.PlayerGravityAcceleration.X > 0 && run.PlayerMass > 0, "Enemy stars exert mass-based gravity on the player");
        run.Step(new(Vector2.UnitY, false, true));
        Check(run.PlayerGravityVelocity == Vector2.Zero && run.DashDuration > 0, "Dash escapes gravitational motion");
        var remainingHealth = run.Health;
        run.HurtContinuous(10);
        Check(run.Health == remainingHealth, "Dash invulnerability also protects against tearing");
        run.Stars.Clear();
        for (var index = 0; index < 100; index++) MarisaProjectileSystem.CastFrom(run, boss.Position, state.Build, state.Marisa, 2, 1, boss.Id);
        Check(run.Stars.Count == BossAbilitySystem.StarLimit && state.Marisa.BlockedEmissions == 100 - BossAbilitySystem.StarLimit, "Boss star capacity is separately enforced without queued burst emissions");
        run.World.Grid.Rebuild(run.Enemies);
        run.Stars.Add(new() { Position = boss.Position, Mass = 2, Life = 8, Duration = 8, DamageRate = 1e9f });
        MarisaProjectileSystem.Step(run);
        Check(run.Phase == RunPhase.Won && boss.Abilities == null && run.Stars.All(star => !star.Hostile), "A star can kill its owner enemy during a span traversal with safe deferred cleanup");
    }

    public static void BossTelegraphs()
    {
        var run = new RunState(HeroKind.Reimu, 42);
        var boss = run.SpawnEnemy(EnemyKind.Boss, new(300, 0));
        boss.Health = boss.MaxHealth * 0.6f;
        var state = boss.Abilities!;
        state.SpecialCooldown = 0;
        run.Step(default);
        Check(state.Phase == 1 && state.Beam is { Warmup: > 1 }, "Boss Spark gives a visible windup");
        Check(!CharacterAttackRules.BeamContains(state.Beam!, boss.Position, run.PlayerPosition, 5), "Telegraph is not a damaging beam");
        var oldDirection = state.Beam!.Direction;
        run.TogglePause();
        var elapsed = state.Elapsed;
        run.Step(new(Vector2.One));
        Check(state.Elapsed == elapsed && state.Beam.Direction == oldDirection, "Pause freezes all Boss timers and effects");
        run.TogglePause();
        for (var tick = 0; tick < 120; tick++)
        {
            oldDirection = state.Beam?.Direction ?? Vector2.Zero;
            run.Step(new(Vector2.UnitY));
            if (state.Beam is { } beam && oldDirection != Vector2.Zero)
                Check(Math.Abs(Geometry.AngleDelta(MathF.Atan2(oldDirection.Y, oldDirection.X), MathF.Atan2(beam.Direction.Y, beam.Direction.X))) <= BossAbilitySystem.BeamTurnRate * RunState.StepSeconds + 0.0001f, "Beam turn rate remains escapable");
        }
        state.Beam = null;
        state.SpecialCooldown = 10000;
        state.ShotCooldown = 10000;
        boss.Health = boss.MaxHealth * 0.5f;
        state.RecoveryCooldown = 0;
        for (var tick = 0; tick < 2700; tick++) BossAbilitySystem.Step(run, boss, -Vector2.UnitX, 300);
        Check(state.RemediesUsed == 2 && state.RecoveryRemaining <= 0, "Boss cannot heal infinitely");
        var reimuRun = new RunState(HeroKind.Marisa, 42);
        var reimu = reimuRun.SpawnEnemy(EnemyKind.Boss, new(300, 0));
        reimu.Health = reimu.MaxHealth * 0.2f;
        reimu.Abilities!.FieldCooldown = 0;
        var orb = reimu.Abilities.OrbitPosition(reimu.Position, 0);
        for (var index = 0; index < 20; index++) reimuRun.Projectiles.Add(new() { Position = orb, Life = 8 });
        BossAbilitySystem.Step(reimuRun, reimu, -Vector2.UnitX, 300);
        Check(reimu.Abilities.Field is { Warmup: > 1 } && reimuRun.Projectiles.Count(projectile => projectile.Life <= 0) == 3, "Boundary is telegraphed and Yin-Yang clear has a shared finite budget");
    }

    public static void Continuation()
    {
        var run = new RunState(HeroKind.Marisa, 42);
        run.Step(default);
        run.Seals[0].Complete = true;
        var boss = run.SpawnEnemy(EnemyKind.Boss, new(300, 0));
        run.DamageEnemy(boss, boss.Health, Vector2.Zero);
        var standardTime = run.Time;
        Check(run.HasStandardVictory && run.Phase == RunPhase.Won && run.CanContinue, "First Boss victory freezes and offers optional continuation");
        run.Step(new(Vector2.One));
        Check(run.Time == standardTime && run.EndlessRounds == 0, "Standard win does not silently start endless mode");
        var stars = run.Stars.ToArray();
        Check(run.ContinueChallenge() && !run.ContinueChallenge(), "Continuation starts once only");
        Check(run.NextBossTime == standardTime + RunPacing.ContinuationInterval && run.Seals[0].Complete && run.Stars.SequenceEqual(stars), "Continuation keeps seals, build and existing player stars; countdown starts after the previous duel");
        boss = run.SpawnEnemy(EnemyKind.Boss, new(300, 0));
        run.DamageEnemy(boss, boss.Health, Vector2.Zero);
        Check(run.EndlessRounds == 1 && run.StandardVictoryTime == standardTime, "Later victories never overwrite standard completion");
        Check(run.ContinueChallenge(), "Another voluntary round can begin");
        for (var tick = 0; tick < 122; tick++) { RunPilot.ResolveChoices(run); run.Step(new(Vector2.UnitX)); }
        run.HurtContinuous(run.MaxHealth * 2);
        Check(run.Phase == RunPhase.Lost && run.HasStandardVictory && !run.CanContinue && !run.FinishChallenge(), "Endless failure cannot erase or convert the standard victory");
        var finish = new RunState(HeroKind.Reimu, 7);
        var last = finish.SpawnEnemy(EnemyKind.Boss, new(300, 0));
        finish.DamageEnemy(last, last.Health, Vector2.Zero);
        Check(finish.FinishChallenge() && !finish.FinishChallenge() && !finish.ContinueChallenge(), "Finishing seals the challenge against accidental continuation");
    }

    public static void LongSoak()
    {
        foreach (var hero in Enum.GetValues<HeroKind>())
        {
            var run = new RunState(hero, 781);
            var endTicks = 3600 / RunState.StepSeconds;
            for (var tick = 0; tick < endTicks; tick++)
            {
                if (run.Phase == RunPhase.Won) Check(run.ContinueChallenge(), "Soak resumes after each won encounter");
                RunPilot.ResolveChoices(run);
                if (tick % 30 == 0) run.Heal(run.MaxHealth);
                run.Step(RunPilot.Input(run, tick));
                Check(run.Phase != RunPhase.Lost, "Healing-assisted stability soak remains active");
                Check(run.Enemies.Count <= RunState.EnemyLimit + 4 && run.Projectiles.Count <= RunState.ProjectileLimit && run.Stars.Count <= MarisaTuning.StarLimit + BossAbilitySystem.StarLimit, "Sixty-minute storage stays bounded");
                Check(float.IsFinite(run.Health) && float.IsFinite(run.PlayerPosition.X) && run.Stars.All(star => float.IsFinite(star.Mass) && float.IsFinite(star.Position.X)), "Sixty-minute values stay finite");
            }
            Check(run.HasStandardVictory && run.IsEndless && run.EndlessRounds >= 3 && run.Build.AllocatedPoints > 26, "Soak includes standard victory, several continuation cycles and later growth");
            Console.WriteLine($"LONG_SOAK healingAssisted=true hero={hero} seconds={run.Time:0.0} choices={run.Build.AllocatedPoints} rounds={run.EndlessRounds} stars={run.Stars.Count}");
        }
    }

    public static void RecoveryWindows()
    {
        var run = new RunState(HeroKind.Reimu, 42);
        run.Hurt(10);
        var health = run.Health;
        for (var tick = 0; tick < RunPacing.RecoveryWindowStart / RunState.StepSeconds + 1; tick++)
        {
            run.Enemies.Clear();
            run.Step(default);
        }
        Check(run.Pickups.Count == 1 && run.Pickups[0].Healing && run.Pickups[0].Value == RunPacing.RecoveryAmount && run.Health == health, "Recovery window creates one nearby collectible, not free healing");
        Check(RunPacing.SpawnRate(77, false, 0) < RunPacing.SpawnRate(75, false, 0), "Recovery window reduces pressure");
        var count = run.Pickups.Count;
        run.TogglePause();
        for (var tick = 0; tick < 600; tick++) run.Step(default);
        Check(run.Pickups.Count == count, "Pausing cannot farm recovery windows");
    }
}
