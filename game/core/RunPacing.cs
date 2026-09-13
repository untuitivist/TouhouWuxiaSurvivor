namespace Rebirth.Core;

public readonly record struct EncounterStage(string Name, float SpawnRate, float FairyChance, float ChargerChance, float HealthScale);

public static class RunPacing
{
    public const float StandardBossTime = 4 * 60;
    public const float ContinuationInterval = 5 * 60;
    public const float WaveSeconds = 60;
    public const float RecoveryWindowStart = 50;
    public const int RecoveryAmount = 20;
    public static EncounterStage At(float seconds) => seconds switch
    {
        < 60 => new("初入夜境", 2.8f, 0.14f, 0.1f, 1),
        < 120 => new("夜巡交锋", 3.8f, 0.22f, 0.18f, 1.6f),
        < 180 => new("妖雾合围", 4.8f, 0.3f, 0.22f, 2.4f),
        _ => new("破晓决战", 5.8f, 0.27f, 0.3f, 3.5f)
    };
    public static int ExperienceFor(int level)
    {
        var value = 10.0 + 7.0 * Math.Max(1, level);
        return (int)Math.Min(int.MaxValue, Math.Ceiling(value));
    }
    public static float SpawnRate(float seconds, bool bossActive, int continuationRounds)
    {
        var recovery = seconds % WaveSeconds >= RecoveryWindowStart ? 0.65f : 1;
        return Math.Min(9, At(seconds).SpawnRate * recovery * (bossActive ? 0.18f : 1) * (1 + continuationRounds * 0.06f));
    }
}
