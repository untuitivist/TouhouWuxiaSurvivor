namespace Rebirth.Core;

public readonly record struct EncounterStage(string Name, float SpawnRate, float FairyChance, float ChargerChance, float HealthScale);

public static class RunPacing
{
    public const float StandardBossTime = 18 * 60;
    public const float ContinuationInterval = 5 * 60;
    public const float WaveSeconds = 90;
    public const float RecoveryWindowStart = 76;
    public const int RecoveryAmount = 20;
    public static EncounterStage At(float seconds) => seconds switch
    {
        < 180 => new("初入夜境", 2.3f, 0.14f, 0.1f, 1),
        < 420 => new("夜巡交锋", 3.5f, 0.22f, 0.18f, 1.6f),
        < 720 => new("妖雾合围", 4.8f, 0.3f, 0.22f, 2.4f),
        _ => new("破晓决战", 6, 0.27f, 0.3f, 3.5f)
    };
    public static int ExperienceFor(int level)
    {
        var value = 18.0 + 11.0 * level + 0.7 * level * level;
        return (int)Math.Min(int.MaxValue, Math.Ceiling(value));
    }
    public static float SpawnRate(float seconds, bool bossActive, int continuationRounds)
    {
        var recovery = seconds % WaveSeconds >= RecoveryWindowStart ? 0.65f : 1;
        return Math.Min(9, At(seconds).SpawnRate * recovery * (bossActive ? 0.45f : 1) * (1 + continuationRounds * 0.06f));
    }
}
