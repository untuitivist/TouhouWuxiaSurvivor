using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private SpriteBatch redPellets = null!;
    private SpriteBatch violetPellets = null!;
    private SpriteBatch experienceBatch = null!;
    private SpriteBatch healingBatch = null!;
    private SpriteBatch ofudaBatch = null!;

    private void CacheCombatStyles()
    {
        redPellets = batches["red_pellet"];
        violetPellets = batches["violet_pellet"];
        experienceBatch = batches["experience"];
        healingBatch = batches["healing"];
        ofudaBatch = batches["ofuda"];
    }

    private bool InView(Vector2 position, float margin = 80)
        => Math.Abs(position.X - camera.X) <= 640 + margin && Math.Abs(position.Y - camera.Y) * WorldProjection.DepthScale <= 370 + margin;

    private void UpdateCombatBatches()
    {
        if (Run == null) return;
        foreach (var batch in batches.Values) batch.Begin();
        foreach (ref readonly var pickup in Run.Pickups.Active)
        {
            var position = Palette.Vector(pickup.Position);
            if (InView(position)) (pickup.Healing ? healingBatch : experienceBatch).AddPosition(position.X, position.Y);
        }
        foreach (ref readonly var projectile in Run.Projectiles.Active)
        {
            var position = Palette.Vector(projectile.Position);
            if (!InView(position)) continue;
            if (projectile.Hostile)
            {
                (projectile.Alternate ? violetPellets : redPellets).AddPosition(position.X, position.Y);
                continue;
            }
            if (!projectile.DreamOrb && projectile.Art == ArtKind.Ofuda)
            {
                ofudaBatch.AddDirected(position, new(22, 22), Colors.White, Palette.Vector(projectile.Velocity));
                continue;
            }
            var name = projectile.DreamOrb ? "dream" : projectile.Art == ArtKind.YinYang ? "actors/yin_yang_orb" : projectile.Art == ArtKind.Stardust ? "stardust" : "star";
            var size = projectile.Hostile ? 15 : projectile.DreamOrb ? projectile.Radius * 2.6f : projectile.Art == ArtKind.Ofuda ? 22 : projectile.Radius * 2.4f;
            var rotation = projectile.Art == ArtKind.Ofuda && !projectile.Hostile ? MathF.Atan2(projectile.Velocity.Y, projectile.Velocity.X) + MathF.PI / 2 : projectile.Hostile ? 0 : Clock * 3 + projectile.Life;
            var tint = projectile.DreamOrb ? DreamColors[projectile.TintIndex % DreamColors.Length] : Colors.White;
            var frames = projectile.Art == ArtKind.YinYang && !projectile.Hostile ? actorAtlas[name].Columns : 1;
            batches[name].Add(position, new(size, size), tint, rotation, (int)(Clock * 10) % frames, frames);
        }
        UpdateActorBatch();
        VisibleBatchInstances = actorBatch.Count;
        foreach (var batch in batches.Values) { batch.Submit(); VisibleBatchInstances += batch.Count; }
    }
}
