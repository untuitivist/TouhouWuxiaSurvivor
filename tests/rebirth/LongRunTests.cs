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

    public static void FreeChoices()
    {
        foreach (var hero in Enum.GetValues<HeroKind>())
        {
            var build = new BuildState(hero);
            var abilities = ArtCatalog.Abilities(hero).ToArray();
            foreach (var unlock in UpgradeCatalog.All.Where(upgrade => upgrade.Owner == hero && upgrade.Kind == UpgradeKind.Unlock))
                Check(build.TryApply(unlock, 100), "Every auxiliary ability can be learned");
            var original = UpgradeCatalog.All.Where(upgrade => upgrade.Owner == hero && build.CanChoose(upgrade, 100)).ToArray();
            var repeat = UpgradeCatalog.Get(MainlineGrowth.PowerId(MainlineGrowth.Primary(hero)));
            Check(!build.TryApply(repeat with { Name = "forged" }, 100), "Only canonical catalog upgrades can be applied");
            for (var rank = 0; rank < 1000; rank++) Check(build.TryApply(repeat, 100), "Focused growth remains effective without committing to a route");
            foreach (var other in original.Where(upgrade => upgrade.Ability != repeat.Ability))
                Check(build.CanChoose(other, 100), "Deep investment does not exclude other abilities or techniques");
            foreach (var art in abilities.Where(art => art.Id != repeat.Ability))
                for (var rank = 0; rank < 1000; rank++) Check(build.TryApply(UpgradeCatalog.Get(MainlineGrowth.PowerId(art.Id)), 100), "Auxiliary abilities can also keep growing");
            Check(abilities.All(art => float.IsFinite(MainlineGrowth.Stats(build, art.Id).Damage)), "Every trained ability remains finite");
            var legal = UpgradeCatalog.All.Where(upgrade => upgrade.Kind != UpgradeKind.Recovery && build.CanChoose(upgrade, 100)).ToArray();
            var seen = new HashSet<string>();
            for (var seed = 0; seed < 512; seed++)
            {
                var offers = UpgradeOffers.Create(build, 100, new(seed));
                Check(offers.Count == 3 && offers.Distinct().Count() == 3 && offers.All(upgrade => build.CanChoose(upgrade, 100)), "Three distinct legal offers");
                Check(offers.Take(2).Select(upgrade => upgrade.Ability).Distinct().Count() == 2, "Different character directions remain visible without forcing the primary ability");
                foreach (var offer in offers) seen.Add(offer.Id);
            }
            Check(legal.All(upgrade => seen.Contains(upgrade.Id)), "Every still-legal direction can return in offers");
            Check(UpgradeCatalog.All.All(upgrade => !upgrade.Id.Contains(".stance.", StringComparison.Ordinal)), "No renamed or hidden exclusive stance remains");
        }
        var tempo = new BuildState(HeroKind.Reimu);
        var rhythm = UpgradeCatalog.Get(MainlineGrowth.OfudaTempo);
        for (var rank = 0; rank < MainlineGrowth.TempoLimit; rank++) Check(tempo.TryApply(rhythm, 100), "Finite casting efficiency rank");
        Check(!tempo.TryApply(rhythm, 100) && tempo.CanChoose(UpgradeCatalog.Get(MainlineGrowth.OfudaPower), 100), "A physical rate limit never closes other growth");
    }

    public static void CombinedGrowth()
    {
        var orders = new[] {
            new[] { MainlineGrowth.OfudaPower, MainlineGrowth.OfudaTempo, UpgradeCatalog.Homing, UpgradeCatalog.Blast },
            new[] { UpgradeCatalog.Blast, UpgradeCatalog.Homing, MainlineGrowth.OfudaTempo, MainlineGrowth.OfudaPower }
        };
        var first = new RunState(HeroKind.Reimu, 42);
        var second = new RunState(HeroKind.Reimu, 42);
        Learn(first, orders[0]);
        Learn(second, orders[1]);
        Check(first.Build.Traits == second.Build.Traits && MainlineGrowth.OfudaStats(first.Build) == MainlineGrowth.OfudaStats(second.Build), "Order does not overwrite power, tempo, homing or blast");
        var baseline = AbilityTuning.Get(ArtKind.Ofuda, 1);
        var combined = MainlineGrowth.OfudaStats(first.Build);
        Check(combined.Damage > baseline.Damage && combined.Interval < baseline.Interval, "Damage and tempo are simultaneously effective");
        Target(first, new(300, 0));
        first.Step(default);
        Check(first.Projectiles.Count > 0 && first.Projectiles.All(projectile => projectile.Pierce == 0), "Ordinary ofuda is not silently converted into the removed charged stance");
        var run = new RunState(HeroKind.Marisa, 42);
        MarisaProjectileSystem.Cast(run, 1);
        var original = run.Stars[0];
        Learn(run, UpgradeCatalog.StarMass, UpgradeCatalog.StarSpread, UpgradeCatalog.StarLifetime, MainlineGrowth.StarPower);
        MarisaProjectileSystem.Cast(run, 1);
        Check(run.Stars[0].Mass == original.Mass && run.Stars[0].Life == original.Life && run.Stars[0].DamageRate == original.DamageRate, "Training does not resample, renew or overwrite an existing star");
        Check(run.Stars[1].Duration > original.Duration && run.Stars[1].DamageRate / run.Stars[1].Mass > original.DamageRate / original.Mass, "New stars combine lifetime, independent distribution and damage growth");
    }

    public static void InvestmentBudget()
    {
        foreach (var hero in Enum.GetValues<HeroKind>())
        {
            var focused = new BuildState(hero);
            var balanced = new BuildState(hero);
            var primary = MainlineGrowth.Primary(hero);
            var primaryTraining = UpgradeCatalog.Get(MainlineGrowth.PowerId(primary));
            for (var point = 0; point < 24; point++) Check(focused.TryApply(primaryTraining, 100), "Allocate focused budget");
            foreach (var unlock in UpgradeCatalog.All.Where(upgrade => upgrade.Owner == hero && upgrade.Kind == UpgradeKind.Unlock))
                Check(balanced.TryApply(unlock, 100), "Allocate breadth from the same budget");
            var abilities = ArtCatalog.Abilities(hero).ToArray();
            for (var point = 0; point < 22; point++)
                Check(balanced.TryApply(UpgradeCatalog.Get(MainlineGrowth.PowerId(abilities[point % abilities.Length].Id)), 100), "Allocate balanced budget");
            Check(focused.AllocatedPoints == balanced.AllocatedPoints && balanced.AllocatedPoints == 24, "Budgets are equal, not constrained by route quotas");
            Check(MainlineGrowth.Power(focused) > MainlineGrowth.Power(balanced), "Breadth trades primary depth instead of closing a route");
            Check(abilities.All(art => balanced.Investment(art.Id) > 0 && MainlineGrowth.Stats(balanced, art.Id).Damage > AbilityTuning.Get(art.Id, balanced.Ranks[(int)art.Id]).Damage), "Every balanced investment has an actual effect");
            foreach (var unlock in UpgradeCatalog.All.Where(upgrade => upgrade.Owner == hero && upgrade.Kind == UpgradeKind.Unlock))
                Check(focused.TryApply(unlock, 100), "Focused players can freely add another ability later");
            Check(focused.TrainingRank(primaryTraining.Id) == 24, "Changing direction never resets prior investment");
            var finiteCost = UpgradeCatalog.All.Where(upgrade => (upgrade.Owner == null || upgrade.Owner == hero) && upgrade.MaxRank != int.MaxValue).Sum(upgrade => upgrade.MaxRank);
            Check(finiteCost > 26, "The reference standard-run budget cannot finish every finite upgrade; sustained training adds further depth");
        }
    }

    public static void AuxiliaryTraining()
    {
        var reimu = new RunState(HeroKind.Reimu, 42);
        Learn(reimu, UpgradeCatalog.BoundaryUnlock, UpgradeCatalog.YinYangUnlock, MainlineGrowth.BoundaryPower, MainlineGrowth.YinYangPower, UpgradeCatalog.Cluster, UpgradeCatalog.Bind, UpgradeCatalog.Clear, UpgradeCatalog.Launch);
        Target(reimu, new(150, 0));
        reimu.Step(default);
        Check(reimu.Field != null && reimu.Field.Damage > AbilityTuning.Get(ArtKind.Boundary, 1).Damage * reimu.Power, "Boundary training reaches the actual field damage");
        Check(MainlineGrowth.Stats(reimu.Build, ArtKind.YinYang).Damage > AbilityTuning.Get(ArtKind.YinYang, 1).Damage, "Orbit and launched orbs share their own potency");
        var marisa = new RunState(HeroKind.Marisa, 42);
        Learn(marisa, UpgradeCatalog.HerbsUnlock, UpgradeCatalog.MasterSparkUnlock, MainlineGrowth.HerbPotency, MainlineGrowth.SparkPower, UpgradeCatalog.HerbBrew, UpgradeCatalog.HerbReserve, UpgradeCatalog.SparkSteer, UpgradeCatalog.SparkWide, UpgradeCatalog.SparkResonance, UpgradeCatalog.SparkClear, UpgradeCatalog.FinalSpark);
        MarisaHerbSystem.Step(marisa);
        var medicine = marisa.Pickups.Single(pickup => pickup.Herbal);
        Check(medicine.Value == AbilityTuning.Get(ArtKind.Herbs, 1).Damage + 1, "Medicine training provides one real healing point");
        Learn(marisa, MainlineGrowth.HerbPotency);
        Check(marisa.Pickups.Single(pickup => pickup.Herbal).Value == medicine.Value, "Existing medicine is not retroactively refreshed");
        MarisaBeamSystem.Start(marisa, false);
        Check(marisa.Beam!.Damage > AbilityTuning.Get(ArtKind.MasterSpark, 1).Damage * marisa.Power && marisa.Beam.Steering && marisa.Beam.ClearsBullets, "Beam potency combines with steering, width, resonance and clearing");
        var before = marisa.Beam.Damage;
        MarisaBeamSystem.Start(marisa, true);
        Check(marisa.Beam!.Damage > before, "Signature beam retains its own trained potency");
        Check(marisa.Build.CanChoose(UpgradeCatalog.Get(UpgradeCatalog.StarMass), 100), "Training medicine and beam never excludes star distribution growth");
    }

    public static void PilotStarAvoidance()
    {
        var run = new RunState(HeroKind.Reimu, 42);
        run.Stars.Add(new() { Position = new(65, 0), Mass = 100, Life = 8, Hostile = true });
        run.Stars.Add(new() { Position = new(-65, 0), Mass = 100, Life = 8, Hostile = true });
        var health = run.Health;
        var result = RunPilot.AvoidStarFields(run, Vector2.UnitX);
        Check(Math.Abs(result.Y) > 0.7f, "Opposing star repulsions must not cancel into a path through either damage field");
        var destination = result * run.MoveSpeed * 0.35f;
        Check(run.Stars.All(star => Geometry.DistanceSquared(destination, star.Position) > MathF.Pow(MarisaTuning.DamageRadius(star.Mass), 2)), "Lookahead exits both real star damage radii");
        Check(run.Health == health && run.Stars.Count == 2 && run.Ticks == 0, "Pilot only supplies input, never heals, clears or advances simulation");
        foreach (ref var star in run.Stars.Active) star.Hostile = false;
        Check(RunPilot.AvoidStarFields(run, Vector2.UnitX) == Vector2.UnitX, "Friendly stars do not divert the driver");
        foreach (ref var star in run.Stars.Active) { star.Hostile = true; star.Life = 0; }
        Check(RunPilot.AvoidStarFields(run, Vector2.UnitX) == Vector2.UnitX, "Dead stars do not divert the driver");
    }

    public static void MixedBulletClearing()
    {
        var run = new RunState(HeroKind.Reimu, 42);
        run.Projectiles.Add(new() { Position = Vector2.Zero, Hostile = true, Radius = 5, Life = 8 });
        run.Projectiles.Add(new() { Position = Vector2.Zero, Radius = 5, Life = 8 });
        for (var index = 0; index < 3; index++) run.Stars.Add(new() { Position = Vector2.Zero, Hostile = true, OwnerId = 77, Mass = 2, Life = 8, Duration = 8 });
        run.Stars.Add(new() { Position = Vector2.Zero, Mass = 3, Life = 8, Duration = 8 });
        var budget = 3;
        ReimuAbilitySystem.ClearProjectiles(run, Vector2.Zero, Vector2.Zero, ref budget);
        Check(budget == 0 && run.Projectiles.Count(projectile => projectile.Hostile && projectile.Life <= 0) + run.Stars.Count(star => star.Hostile && star.Life <= 0) == 3, "Projectiles and hostile stars share one unchanged budget");
        Check(run.Stars.Count == 4 && run.Stars.Single(star => !star.Hostile).Life == 8 && run.Projectiles.Single(projectile => !projectile.Hostile).Life == 8, "Clearing marks safely and preserves friendly entities");
        budget = 3;
        CharacterAttackRules.ClearSegment(run, Vector2.Zero, Vector2.Zero, 24, ref budget, false);
        Check(budget == 1 && run.Stars.Single(star => !star.Hostile).Life == 0 && run.Stars.Count(star => star.Hostile && star.Life > 0) == 1, "Boss orb clearing targets player bullets and stars without friendly fire");
        var signature = new RunState(HeroKind.Reimu, 42);
        Learn(signature, UpgradeCatalog.DreamSeal);
        signature.Stars.Add(new() { Position = new(600, 0), Hostile = true, OwnerId = 77, Mass = 2, Life = 8, Duration = 8 });
        signature.Stars.Add(new() { Position = new(-600, 0), Mass = 2, Life = 8, Duration = 8 });
        for (var index = 0; index < 20; index++)
            signature.Projectiles.Add(new() { Position = Geometry.Angle(index * MathF.Tau / 20) * 25, Hostile = true, Radius = 5, Life = 2 });
        signature.Step(default);
        Check(signature.SpellsCast == 1 && signature.Stars.All(star => !star.Hostile || star.Life <= 0) && signature.Stars.Any(star => !star.Hostile && star.Life > 0), "Signature clears hostile stars globally, never the player's own field");
        var beam = new RunState(HeroKind.Marisa, 42);
        Learn(beam, UpgradeCatalog.MasterSparkUnlock, UpgradeCatalog.SparkClear);
        MarisaBeamSystem.Start(beam, false);
        beam.Beam!.Warmup = 0;
        for (var index = 0; index < MarisaTuning.BeamClearLimit + 2; index++)
            beam.Stars.Add(new() { Position = beam.Beam.Direction * 150, Hostile = true, OwnerId = 77, Mass = 2, Life = 8, Duration = 8 });
        beam.Stars.Add(new() { Position = beam.Beam.Direction * 150, Mass = 2, Life = 8, Duration = 8 });
        MarisaBeamSystem.Step(beam);
        Check(beam.Stars.Count(star => star.Hostile && star.Life <= 0) == MarisaTuning.BeamClearLimit && beam.Stars.Single(star => !star.Hostile).Life == 8, "Beam clearing includes enemy stars under the existing per-pulse budget");
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
