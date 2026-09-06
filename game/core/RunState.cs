using System.Numerics;

namespace Rebirth.Core;

public sealed partial class RunState
{
    public const float StepSeconds = 1f / 60;
    public const float BossArrival = 240;
    public const float ArenaHalfWidth = 1500;
    public const float ArenaHalfHeight = 1100;
    public const int EnemyLimit = 320;
    public const int ProjectileLimit = 1600;
    public const int PickupLimit = 700;
    public readonly List<Enemy> Enemies = [];
    public readonly List<Projectile> Projectiles = [];
    public readonly List<Pickup> Pickups = [];
    public readonly List<CombatEvent> Events = [];
    public readonly List<ArtKind> Choices = [];
    public readonly int[] Ranks = new int[9];
    public readonly Seal[] Seals =
    [
        new() { Name = "天之印", Position = new(-680, -430) },
        new() { Name = "地之印", Position = new(680, -330) },
        new() { Name = "人之印", Position = new(100, 650) }
    ];
    public HeroKind Hero { get; }
    public int Seed { get; }
    public RunPhase Phase { get; private set; } = RunPhase.Playing;
    public Vector2 PlayerPosition { get; private set; }
    public Vector2 PlayerVelocity { get; private set; }
    public Vector2 Facing { get; private set; } = Vector2.UnitY;
    public int Ticks { get; private set; }
    public float Time => Ticks * StepSeconds;
    public float Health { get; private set; }
    public float MaxHealth => (Hero == HeroKind.Reimu ? 110 : 85) + Ranks[(int)ArtKind.Vitality] * 25;
    public float Power => (Hero == HeroKind.Marisa ? 1.16f : 1) * (1 + Ranks[(int)ArtKind.Power] * 0.18f);
    public float CastSpeed => 1 + Ranks[(int)ArtKind.Haste] * 0.14f;
    public float MoveSpeed => (Hero == HeroKind.Marisa ? 220 : 205) * (1 + Ranks[(int)ArtKind.Flow] * 0.06f);
    public float PickupRadius => 78 + Ranks[(int)ArtKind.Flow] * 35;
    public float DashDuration { get; private set; }
    public float DashCooldown { get; private set; }
    public float DashInterval => 2.8f - Ranks[(int)ArtKind.Flow] * 0.35f;
    public float Invulnerability { get; private set; }
    public float Qi { get; private set; }
    public float BurstGlow { get; private set; }
    public int Kills { get; private set; }
    public int Grazes { get; private set; }
    public int Bursts { get; private set; }
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int NextLevelExperience => 7 + Level * 4;
    public int PendingChoices { get; private set; }
    public int PurifiedSeals => Seals.Count(seal => seal.Complete);
    public Enemy? Boss => Enemies.Find(enemy => enemy.Kind == EnemyKind.Boss && enemy.Health > 0);
    public bool BossSpawned { get; private set; }
    public float OrbitAngle => Time * 2.9f;
    public float OrbitRadius => Ranks[(int)ArtKind.Orbit] >= 5 ? 115 : 85;
    private readonly Random random;
    private readonly EnemyGrid grid = new();
    private int nextEnemyId;
    private float spawnTimer = 0.4f;
    private float nextElite = 55;
    private float swordTimer;
    private float orbitTimer;
    private float talismanTimer;
    private float lightningTimer;
    private Vector2 dashDirection;

    public RunState(HeroKind hero, int seed)
    {
        Hero = hero;
        Seed = seed;
        random = new Random(seed);
        Health = MaxHealth;
        Ranks[(int)ArtKind.Sword] = 1;
        if (hero == HeroKind.Reimu) Ranks[(int)ArtKind.Orbit] = 1;
        else Ranks[(int)ArtKind.Lightning] = 1;
    }

    public void Step(FrameInput input)
    {
        Events.Clear();
        if (Phase != RunPhase.Playing) return;
        Ticks++;
        Invulnerability = Math.Max(0, Invulnerability - StepSeconds);
        DashDuration = Math.Max(0, DashDuration - StepSeconds);
        DashCooldown = Math.Max(0, DashCooldown - StepSeconds);
        BurstGlow = Math.Max(0, BurstGlow - StepSeconds);
        MovePlayer(input);
        UpdateEncounters();
        UpdateEnemies();
        grid.Rebuild(Enemies);
        UpdateWeapons();
        UpdateProjectiles();
        if (Phase == RunPhase.Won) Projectiles.RemoveAll(projectile => projectile.Hostile);
        if (Phase == RunPhase.Playing) UpdatePickupsAndSeals();
        Enemies.RemoveAll(enemy => enemy.Health <= 0);
        if (Qi >= 100 && Phase == RunPhase.Playing) ReleaseBurst();
        if (PendingChoices > 0 && Phase == RunPhase.Playing) OfferChoices();
    }

    private void MovePlayer(FrameInput input)
    {
        var movement = input.Move;
        if (movement.LengthSquared() > 1) movement = Vector2.Normalize(movement);
        if (movement.LengthSquared() > 0.01f) Facing = Geometry.Direction(movement);
        if (input.Dash && DashCooldown <= 0)
        {
            dashDirection = Facing;
            DashDuration = 0.2f;
            DashCooldown = DashInterval;
            Invulnerability = Math.Max(Invulnerability, 0.28f);
            Emit(EffectKind.Dash, PlayerPosition);
        }
        PlayerVelocity = DashDuration > 0 ? dashDirection * 860 : movement * MoveSpeed * (input.Focus ? 0.48f : 1);
        PlayerPosition = ClampToArena(PlayerPosition + PlayerVelocity * StepSeconds);
    }

    public void TogglePause()
    {
        if (Phase == RunPhase.Playing) Phase = RunPhase.Paused;
        else if (Phase == RunPhase.Paused) Phase = RunPhase.Playing;
    }

    private void Hurt(float damage)
    {
        if (Invulnerability > 0 || Phase != RunPhase.Playing) return;
        Health = Math.Max(0, Health - damage);
        Invulnerability = 0.95f;
        Emit(EffectKind.Hurt, PlayerPosition, damage);
        if (Health <= 0) Phase = RunPhase.Lost;
    }

    private void Heal(float amount) => Health = Math.Min(MaxHealth, Health + amount);
    private void Emit(EffectKind kind, Vector2 position, float value = 0) => Events.Add(new(kind, position, position, value));
    private float RandomFloat() => (float)random.NextDouble();
    private static Vector2 ClampToArena(Vector2 position) => Vector2.Clamp(position, new(-ArenaHalfWidth + 28, -ArenaHalfHeight + 28), new(ArenaHalfWidth - 28, ArenaHalfHeight - 28));
}
