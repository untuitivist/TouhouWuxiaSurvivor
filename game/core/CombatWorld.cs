namespace Rebirth.Core;

public sealed class CombatWorld
{
    public ComponentStore<Enemy> Enemies { get; } = new(RunState.EnemyLimit + 4);
    public ComponentStore<Projectile> Projectiles { get; } = new(RunState.ProjectileLimit);
    public ComponentStore<Pickup> Pickups { get; } = new(RunState.PickupLimit + 1);
    public ComponentStore<StarBody> Stars { get; } = new(MarisaTuning.StarLimit + BossAbilitySystem.StarLimit);
    internal EnemyGrid Grid { get; } = new();
}
