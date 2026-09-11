using System.Diagnostics;
using System.Numerics;
using Rebirth.Core;
using Rebirth.Diagnostics;
using Rebirth.Tests;

if (args.Contains("--performance")) return PerformanceBenchmarks.Run(args);
if (args.Contains("--growth-balance")) return GrowthBalance.Run();
if (args.Contains("--marisa-balance")) return MarisaBalance.Run();

var tests = new (string Name, Action Body)[]
{
    ("dense component lifecycle preserves order and state", EcsTests.Storage),
    ("inline hit history retains overflow and duplicate safety", EcsTests.History),
    ("spatial identity lookup and allocation-free queries", EcsTests.Queries),
    ("scalar hot geometry matches vector reference", EcsTests.GeometryEquivalence),
    ("dense grid and overflow preserve reference query order", EcsTests.GridEquivalence),
    ("hero identities and initial weapons", HeroIdentity),
    ("Marisa unlocks, prerequisites and compatible choices", MarisaGrowthTests.Offers),
    ("Marisa starts with stars and learns her signature", MarisaGrowthTests.Starter),
    ("Marisa samples a trainable distribution without a guaranteed heavy star", MarisaGrowthTests.Mass),
    ("Marisa cosmetic colors do not alter mass, damage or lifetime", MarisaGrowthTests.CosmeticIsolation),
    ("Marisa emits individual stars frequently in every direction", MarisaGrowthTests.Emission),
    ("Marisa tears continuously and retargets without renewed life", MarisaGrowthTests.Sustain),
    ("Marisa stars stay within storage capacity and freeze with choices", MarisaGrowthTests.Lifecycle),
    ("Marisa many-body gravity is reciprocal and sums all nearby masses", MarisaGrowthTests.Targeting),
    ("Marisa tearing reaches all nearby targets", MarisaGrowthTests.CrowdedTarget),
    ("Marisa high-mass stars can attract bosses without immunity", MarisaGrowthTests.Planet),
    ("Marisa remedy storage, collection and expiry", MarisaHerbTests.Lifecycle),
    ("Marisa healing rate, queue and pickup budgets", MarisaHerbTests.Budget),
    ("Marisa beam steering, width and capped pulse clear", MarisaBeamTests.Branches),
    ("Marisa beam auto-aim tracks the nearest enemy independently of movement", MarisaBeamTests.Targeting),
    ("Marisa beam resonance has a bounded damage multiplier", MarisaBeamTests.Resonance),
    ("Marisa descriptions translate in both languages", MarisaGrowthTests.Language),
    ("both diagnostic hero builds continue through upgrades", MarisaBeamTests.Stress),
    ("Marisa equal-point routes have bounded distinct niches", MarisaBalance.Guardrails),
    ("growth prerequisites, uniqueness and mixed offers", GrowthTests.Offers),
    ("compatible branches commute and reject repeats", GrowthTests.Combinations),
    ("basic ofuda stays straight and signature stays locked", GrowthTests.Basic),
    ("all straight ofuda ranks aim a real shot at distant targets", GrowthTests.StraightAim),
    ("equal-point route balance retains distinct niches", GrowthBalance.Guardrails),
    ("homing blast combines without recursive explosions", GrowthTests.Blast),
    ("clustered boundary binds while boss attacks continue", GrowthTests.Boundary),
    ("yin yang charge, launch and pause share orbit state", GrowthTests.Orbit),
    ("bounded bullet clear is safe regardless of storage order", GrowthTests.Clear),
    ("upgrade descriptions match shared ability tuning", HeroTests.UpgradeDescriptions),
    ("character-owned upgrade pools and validation", HeroTests.Ownership),
    ("ofuda tracks and reacquires targets", HeroTests.Homing),
    ("star focus changes spread not damage", HeroTests.StarFocus),
    ("sealing field remains at its cast location", HeroTests.StationaryBoundary),
    ("signature spells have distinct mechanics", HeroTests.SignatureIdentity),
    ("pause and choices freeze persistent abilities", HeroTests.AbilityPause),
    ("focus movement preserves precise control", FocusMovement),
    ("normalized movement and finite arena", Movement),
    ("pause freezes the complete simulation", Pause),
    ("queued upgrades freeze and resolve once", Choices),
    ("upgrade ranks cap and recover safely", UpgradeCaps),
    ("dash invulnerability and cooldown", Dash),
    ("each bullet grants graze only once", Graze),
    ("full spell charge clears bullets and attracts experience", QiBurst),
    ("swept collision catches fast projectiles", SweptCollision),
    ("seal retains progress and rewards once", SealReward),
    ("enemy death rewards exactly once", DeathReward),
    ("piercing projectile cannot repeatedly hit one target", Piercing),
    ("sustained beam respects warmup geometry and pulse rate", HeroTests.BeamGeometry),
    ("boss defeat yields victory and freezes time", Victory),
    ("player death cannot turn into victory", Defeat),
    ("same seed and input reproduce state", Determinism),
    ("fresh run has no previous session state", Restart),
    ("full timeline reaches boss with bounded entities", FullTimeline)
};
var failures = 0;
var watch = Stopwatch.StartNew();
foreach (var test in tests)
{
    try { test.Body(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception error) { failures++; Console.WriteLine($"FAIL {test.Name}: {error.Message}"); }
}
Console.WriteLine($"REBIRTH_CORE: {tests.Length - failures}/{tests.Length} passed in {watch.Elapsed.TotalSeconds:0.00}s");
if (args.Contains("--balance")) BalanceReport();
return failures == 0 ? 0 : 1;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Near(float actual, float expected, float tolerance = 0.05f) => Check(Math.Abs(actual - expected) <= tolerance, $"Expected {expected}, actual {actual}");
static RunState NewRun(HeroKind hero = HeroKind.Reimu) => new(hero, 260906);
static void Resolve(RunState run)
{
    RunPilot.ResolveChoices(run);
}


static void HeroIdentity()
{
    var reimu = NewRun();
    var marisa = NewRun(HeroKind.Marisa);
    Check(reimu.MaxHealth > marisa.MaxHealth && marisa.MoveSpeed > reimu.MoveSpeed, "Different strengths");
    Check(reimu.Ranks[(int)ArtKind.Ofuda] == 1 && reimu.Ranks[(int)ArtKind.YinYang] == 0 && marisa.Ranks[(int)ArtKind.Stars] == 1 && marisa.Ranks[(int)ArtKind.MasterSpark] == 0 && marisa.Ranks[(int)ArtKind.Ofuda] == 0, "Different starter arts");
}

static void Movement()
{
    var run = NewRun();
    run.Step(new(new(1, 1)));
    Near(run.PlayerPosition.Length(), run.MoveSpeed * RunState.StepSeconds);
    for (var index = 0; index < 1600; index++) { Resolve(run); run.Enemies.Clear(); run.Step(new(new(1, 1))); }
    Check(run.PlayerPosition.X < RunState.ArenaHalfWidth && run.PlayerPosition.Y < RunState.ArenaHalfHeight, "Arena clamp");
}

static void FocusMovement()
{
    var run = NewRun();
    run.Step(new(Vector2.UnitX, true));
    Near(run.PlayerPosition.X, run.MoveSpeed * 0.48f * RunState.StepSeconds);
}

static void QiBurst()
{
    var run = NewRun();
    run.Build.TryApply(UpgradeCatalog.Get(UpgradeCatalog.DreamSeal), 5);
    for (var index = 0; index < 20; index++)
        run.Projectiles.Add(new() { Position = Geometry.Angle(index * MathF.Tau / 20) * 25, Hostile = true, Radius = 5, Life = 2 });
    run.Pickups.Add(new() { Position = new(500, 0), Value = 4 });
    var enemy = run.SpawnEnemy(EnemyKind.Elite, new(350, 0));
    var health = enemy.Health;
    run.Step(default);
    Check(run.SpellsCast == 1 && run.SpellCharge == 0 && run.Invulnerability > 0, "Burst state");
    Check(run.Projectiles.All(projectile => !projectile.Hostile) && run.Pickups.All(pickup => pickup.Attracted), "Bullet clear and magnet");
    Check(run.Projectiles.Count(projectile => projectile.DreamOrb) == 7, "Signature creates real projectiles");
    for (var index = 0; index < 60; index++) run.Step(default);
    Check(enemy.Health < health, "Dream orbs reach enemies after travel");
}

static void Piercing()
{
    var run = NewRun(HeroKind.Marisa);
    Array.Clear(run.Ranks);
    var enemy = run.SpawnEnemy(EnemyKind.Elite, new(120, 0));
    var health = enemy.Health;
    run.Projectiles.Add(new() { Position = enemy.Position, Damage = 7, Radius = 40, Pierce = 5, Life = 0.15f });
    for (var index = 0; index < 8; index++) run.Step(default);
    Near(enemy.Health, health - 7);
}


static void Pause()
{
    var run = NewRun();
    run.Step(new(Vector2.UnitX));
    var previous = run.PlayerPosition;
    run.TogglePause();
    for (var index = 0; index < 60; index++) run.Step(new(Vector2.UnitY, false, true));
    Check(run.Ticks == 1 && run.PlayerPosition == previous && run.Enemies.Count == 0, "Frozen simulation");
    run.TogglePause();
    run.Step(default);
    Check(run.Ticks == 2, "Resume");
}

static void Choices()
{
    var run = NewRun();
    run.AddExperience(60);
    run.Step(default);
    var pending = run.PendingChoices;
    var tick = run.Ticks;
    Check(pending >= 2 && run.Phase == RunPhase.Choosing, "Queued levels");
    Check(run.Choices.Distinct().Count() == run.Choices.Count, "Unique options");
    run.Step(new(Vector2.One));
    Check(run.Ticks == tick && !run.Choose(-1) && !run.Choose(9), "Freeze and bounds");
    var previousRanks = run.Build.AllocatedPoints;
    Check(run.Choose(0) && run.PendingChoices == pending - 1 && run.Build.AllocatedPoints == previousRanks + 1, "One application");
    Resolve(run);
    Check(!run.Choose(0) && run.Phase == RunPhase.Playing, "Cannot choose stale card");
}

static void UpgradeCaps()
{
    var run = NewRun();
    foreach (var art in ArtCatalog.All.Where(art => art.Id != ArtKind.Recovery)) run.Ranks[(int)art.Id] = art.MaxRank;
    foreach (var upgrade in UpgradeCatalog.All.Where(upgrade => upgrade.Kind == UpgradeKind.Behavior)) run.Build.TryApply(upgrade, 100);
    run.AddExperience(50);
    run.Step(default);
    Check(run.Choices.SequenceEqual(new[] { UpgradeCatalog.Get(UpgradeCatalog.Recovery) }), "Fallback only");
    Resolve(run);
    foreach (var art in ArtCatalog.All.Where(art => art.Id != ArtKind.Recovery)) Check(run.Ranks[(int)art.Id] == art.MaxRank, "Rank limit");
}

static void Dash()
{
    var run = NewRun();
    run.Step(new(Vector2.UnitX, false, true));
    var cooldown = run.DashCooldown;
    Check(run.Invulnerability > 0 && run.DashDuration > 0 && run.PlayerPosition.X > 10, "Dash activation");
    run.Step(new(Vector2.UnitX, false, true));
    Check(run.DashCooldown < cooldown, "No retrigger during cooldown");
    for (var index = 0; index < 180; index++) { run.Enemies.Clear(); run.Step(default); }
    run.Step(new(Vector2.UnitX, false, true));
    Check(run.DashCooldown > 2, "Dash recharges");
}

static void Graze()
{
    var run = NewRun();
    run.Projectiles.Add(new() { Position = new(24, 0), Hostile = true, Radius = 5, Damage = 10, Life = 5 });
    for (var index = 0; index < 12; index++) run.Step(default);
    Check(run.Grazes == 1, "One graze per bullet");
    Near(run.SpellCharge, 5);
    Near(run.Health, run.MaxHealth);
}

static void SweptCollision()
{
    var run = NewRun();
    run.Projectiles.Add(new() { Position = new(-100, 0), Velocity = new(12000, 0), Hostile = true, Radius = 5, Damage = 17, Life = 2 });
    run.Step(default);
    Near(run.Health, run.MaxHealth - 17);
    Check(run.Grazes == 0, "Hit is not graze");
}

static void SealReward()
{
    var run = NewRun();
    var seal = run.Seals[0];
    seal.Position = Vector2.Zero;
    for (var index = 0; index < 180; index++) { run.Enemies.Clear(); run.Step(default); }
    var charge = seal.Charge;
    seal.Position = new(1000, 0);
    for (var index = 0; index < 60; index++) { run.Enemies.Clear(); run.Step(default); }
    Near(seal.Charge, charge);
    seal.Position = Vector2.Zero;
    for (var index = 0; index < 250; index++) { run.Enemies.Clear(); run.Step(default); }
    Check(seal.Complete && run.PendingChoices == 1, "Seal reward");
    Resolve(run);
    for (var index = 0; index < 120; index++) { run.Enemies.Clear(); run.Step(default); }
    Check(run.PendingChoices == 0 && run.PurifiedSeals == 1, "No duplicate reward");
}

static void DeathReward()
{
    var run = NewRun();
    var enemy = run.SpawnEnemy(EnemyKind.Kedama, new(80, 0));
    enemy.Health = 1;
    run.Projectiles.Add(new() { Position = enemy.Position, Damage = 100, Radius = 10, Life = 1, Pierce = 4 });
    run.Projectiles.Add(new() { Position = enemy.Position, Damage = 100, Radius = 10, Life = 1, Pierce = 4 });
    run.Step(default);
    Check(run.Kills == 1 && run.Events.Count(entry => entry.Kind == EffectKind.Defeat) == 1, "No double drop");
}

static void Victory()
{
    var run = NewRun();
    var boss = run.SpawnEnemy(EnemyKind.Boss, new(90, 0));
    boss.Health = 1;
    run.Projectiles.Add(new() { Position = new(300, 0), Hostile = true, Life = 2 });
    run.Projectiles.Add(new() { Position = boss.Position, Damage = 100, Radius = 30, Life = 1 });
    run.Step(default);
    Check(run.Phase == RunPhase.Won && run.Projectiles.All(projectile => !projectile.Hostile), "Victory clears danger");
    var tick = run.Ticks;
    run.Step(new(Vector2.One));
    Check(run.Ticks == tick, "Victory freezes time");
}

static void Defeat()
{
    var run = NewRun();
    run.Projectiles.Add(new() { Position = Vector2.Zero, Hostile = true, Damage = 1000, Radius = 5, Life = 2 });
    run.Step(default);
    Check(run.Phase == RunPhase.Lost && run.Health == 0, "Death state");
    run.AddExperience(1000);
    run.Step(default);
    Check(run.Phase == RunPhase.Lost && run.PendingChoices == 0, "No postmortem progression");
}

static void Determinism()
{
    var first = NewRun();
    var second = NewRun();
    for (var index = 0; index < 5400; index++)
    {
        Resolve(first); Resolve(second);
        var input = new FrameInput(new(MathF.Cos(index * 0.004f), MathF.Sin(index * 0.004f)), index % 400 < 60, index % 220 == 0);
        first.Step(input); second.Step(input);
    }
    Check(first.Phase == second.Phase && first.PlayerPosition == second.PlayerPosition && first.Health == second.Health && first.Kills == second.Kills && first.Grazes == second.Grazes, "Matching state");
    Check(first.Enemies.Select(enemy => enemy.Position).SequenceEqual(second.Enemies.Select(enemy => enemy.Position)), "Matching spawns");
}

static void Restart()
{
    var old = NewRun();
    old.AddExperience(900);
    old.Step(default);
    Resolve(old);
    var fresh = NewRun();
    Check(fresh.Ticks == 0 && fresh.Level == 1 && fresh.Enemies.Count == 0 && fresh.Ranks.Sum() == HeroCatalog.Get(fresh.Hero).InitialAbilities.Count && fresh.SpellCharge == 0, "Independent state");
}

static void FullTimeline()
{
    var run = NewRun();
    var maximumEnemies = 0;
    var maximumProjectiles = 0;
    for (var index = 0; index < 19000 && run.Phase is not (RunPhase.Won or RunPhase.Lost); index++)
    {
        Resolve(run);
        if (index % 30 == 0) run.Pickups.Add(new() { Position = run.PlayerPosition, Healing = true, Value = 100 });
        run.Step(BotInput(run, index));
        maximumEnemies = Math.Max(maximumEnemies, run.Enemies.Count);
        maximumProjectiles = Math.Max(maximumProjectiles, run.Projectiles.Count);
        Check(run.Enemies.Count <= RunState.EnemyLimit + 4 && run.Projectiles.Count <= RunState.ProjectileLimit && run.Pickups.Count <= RunState.PickupLimit + 1, "Bounded entities");
        Check(float.IsFinite(run.PlayerPosition.X) && float.IsFinite(run.Health), "Finite simulation");
    }
    Check(run.BossSpawned, $"Timeline reaches boss: {run.Phase} at {run.Time}");
    Console.WriteLine($"  SOAK (healing-assisted, not balance proof): {run.Phase} time={run.Time:0.0} kills={run.Kills} level={run.Level} maxEnemies={maximumEnemies} maxBullets={maximumProjectiles}");
}

static FrameInput BotInput(RunState run, int tick)
{
    return RunPilot.Input(run, tick);
}

static void BalanceReport()
{
    foreach (var hero in Enum.GetValues<HeroKind>())
    foreach (var seed in new[] { 42, 260906, 781 })
    {
        var run = new RunState(hero, seed);
        for (var tick = 0; tick < 25200 && run.Phase is not (RunPhase.Won or RunPhase.Lost); tick++)
        {
            Resolve(run);
            run.Step(BotInput(run, tick));
        }
        Console.WriteLine($"BALANCE hero={hero} seed={seed} result={run.Phase} time={run.Time:0.0} kills={run.Kills} level={run.Level} seals={run.PurifiedSeals} health={run.Health:0.0} grazes={run.Grazes} bossHp={run.Boss?.Health:0}");
    }
}
