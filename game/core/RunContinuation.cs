namespace Rebirth.Core;

public sealed partial class RunState
{
    public bool HasStandardVictory { get; private set; }
    public float StandardVictoryTime { get; private set; }
    public bool IsEndless { get; private set; }
    public int EndlessRounds { get; private set; }
    public bool ChallengeFinished { get; private set; }
    public float NextBossTime { get; private set; } = RunPacing.StandardBossTime;
    public float EndlessTime => HasStandardVictory ? Math.Max(0, Time - StandardVictoryTime) : 0;
    public bool CanContinue => Phase == RunPhase.Won && !ChallengeFinished;
    public string StageName => IsEndless ? "续战修行" : RunPacing.At(Time).Name;
    public int PlayerStarCount => MarisaProjectileSystem.CountOwned(this, 0);

    public bool ContinueChallenge()
    {
        if (!CanContinue || !HasStandardVictory) return false;
        IsEndless = true;
        NextBossTime = Time + RunPacing.ContinuationInterval;
        BossSpawned = false;
        nextElite = Time + 90;
        spawnTimer = 3;
        Enemies.Clear();
        Projectiles.RemoveWhere(static (in Projectile projectile) => projectile.Hostile);
        Stars.RemoveWhere(static (in StarBody star) => star.Hostile || star.Life <= 0);
        PlayerGravityAcceleration = default;
        PlayerGravityVelocity = default;
        PlayerBoundRemaining = 0;
        Heal(MaxHealth * 0.25f);
        Invulnerability = Math.Max(Invulnerability, 2);
        Phase = RunPhase.Playing;
        if (PendingChoices > 0) OfferChoices();
        return true;
    }

    public bool FinishChallenge()
    {
        if (!HasStandardVictory || Phase == RunPhase.Lost || ChallengeFinished) return false;
        ChallengeFinished = true;
        Phase = RunPhase.Won;
        return true;
    }

    private void CompleteBossEncounter(Enemy enemy)
    {
        if (Phase == RunPhase.Lost) return;
        if (!HasStandardVictory)
        {
            HasStandardVictory = true;
            StandardVictoryTime = Time;
        }
        else EndlessRounds++;
        Phase = RunPhase.Won;
        Emit(EffectKind.Victory, enemy.Position);
    }
}
