namespace Rebirth.Core;

public sealed class CombatWorld
{
    public ComponentStore<Enemy> Enemies { get; } = new(RunState.EnemyLimit + 4);
    public ComponentStore<Projectile> Projectiles { get; } = new(RunState.ProjectileLimit);
    public ComponentStore<Pickup> Pickups { get; } = new(RunState.PickupLimit + 1);
    internal EnemyGrid Grid { get; } = new();
}
