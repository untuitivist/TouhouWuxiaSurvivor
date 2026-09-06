using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private bool InView(Vector2 position, float margin = 80)
        => Math.Abs(position.X - camera.X) <= 640 + margin && Math.Abs(position.Y - camera.Y) <= 370 + margin;

    private void UpdateCombatBatches()
    {
        if (Run == null) return;
        foreach (var batch in batches.Values) batch.Begin();
        foreach (ref readonly var pickup in Run.Pickups.Active)
        {
            var position = Palette.Vector(pickup.Position);
            if (InView(position)) batches[pickup.Healing ? "healing" : "experience"].Add(position, Vector2.One * (pickup.Healing ? 17 : 12), Colors.White);
        }
        foreach (var enemy in Run.Enemies)
        {
            var position = Palette.Vector(enemy.Position);
            if (!InView(position, 100)) continue;
            var name = enemy.Kind switch { EnemyKind.Kedama => "actors/kedama", EnemyKind.Fairy => "actors/wild_fairy", EnemyKind.Charger => "actors/mountain_spirit", _ => "actors/great_youkai" };
            var scale = enemy.Kind switch { EnemyKind.Kedama => 0.95f, EnemyKind.Elite => 1.7f, EnemyKind.Boss => 2f, _ => 1.15f };
            var (size, frameCount) = spriteFrames[name];
            var frame = (int)(Clock * 9 + enemy.Id * 0.7f) % frameCount;
            batches[name].Add(position - new Vector2(0, size * scale * 0.25f), Vector2.One * size * scale,
                enemy.Flash > 0 ? new Color(1.8f, 1.8f, 1.8f) : Colors.White, frame: frame, frameCount: frameCount);
        }
        foreach (ref readonly var projectile in Run.Projectiles.Active)
        {
            var position = Palette.Vector(projectile.Position);
            if (!InView(position)) continue;
            var name = projectile.Hostile ? projectile.Alternate ? "violet_pellet" : "red_pellet"
                : projectile.DreamOrb ? "dream" : projectile.Art == ArtKind.Ofuda ? "ofuda" : projectile.Art == ArtKind.Stardust ? "stardust" : "star";
            var size = projectile.Hostile ? 15 : projectile.DreamOrb ? projectile.Radius * 2.6f : projectile.Art == ArtKind.Ofuda ? 22 : projectile.Radius * 2.4f;
            var rotation = projectile.Art == ArtKind.Ofuda && !projectile.Hostile ? MathF.Atan2(projectile.Velocity.Y, projectile.Velocity.X) + MathF.PI / 2 : projectile.Hostile ? 0 : Clock * 3 + projectile.Life;
            var tint = projectile.DreamOrb ? DreamColors[projectile.TintIndex % DreamColors.Length] : Colors.White;
            batches[name].Add(position, Vector2.One * size, tint, rotation);
        }
        VisibleBatchInstances = 0;
        foreach (var batch in batches.Values) { batch.Submit(); VisibleBatchInstances += batch.Count; }
    }
}
