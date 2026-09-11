using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private SpriteBatch redPellets = null!;
    private SpriteBatch violetPellets = null!;
    private SpriteBatch experienceBatch = null!;
    private SpriteBatch healingBatch = null!;
    private SpriteBatch herbBatch = null!;
    private SpriteBatch ofudaBatch = null!;
    private (SpriteBatch Batch, float Size, int Frames)[] enemyStyles = [];

    private void CacheCombatStyles()
    {
        redPellets = batches["red_pellet"];
        violetPellets = batches["violet_pellet"];
        experienceBatch = batches["experience"];
        healingBatch = batches["healing"];
        herbBatch = batches["herb"];
        ofudaBatch = batches["ofuda"];
        enemyStyles = Enum.GetValues<EnemyKind>().Select(kind =>
        {
            var name = kind switch { EnemyKind.Kedama => "actors/kedama", EnemyKind.Fairy => "actors/wild_fairy", EnemyKind.Charger => "actors/mountain_spirit", _ => "actors/great_youkai" };
            var scale = kind switch { EnemyKind.Kedama => 0.95f, EnemyKind.Elite => 1.7f, EnemyKind.Boss => 2f, _ => 1.15f };
            var (size, frames) = spriteFrames[name];
            return (batches[name], size * scale, frames);
        }).ToArray();
    }

    private bool InView(Vector2 position, float margin = 80)
        => Math.Abs(position.X - camera.X) <= 640 + margin && Math.Abs(position.Y - camera.Y) <= 370 + margin;

    private void UpdateCombatBatches()
    {
        if (Run == null) return;
        foreach (var batch in batches.Values) batch.Begin();
        foreach (ref readonly var pickup in Run.Pickups.Active)
        {
            var position = Palette.Vector(pickup.Position);
            if (InView(position)) (pickup.Herbal ? herbBatch : pickup.Healing ? healingBatch : experienceBatch).AddPosition(position.X, position.Y);
        }
        foreach (var enemy in Run.Enemies)
        {
            var position = Palette.Vector(enemy.Position);
            if (!InView(position, 100)) continue;
            var style = enemyStyles[(int)enemy.Kind];
            var frame = (int)(Clock * 9 + enemy.Id * 0.7f) % style.Frames;
            style.Batch.Add(new(position.X, position.Y - style.Size * 0.25f), new(style.Size, style.Size),
                enemy.Flash > 0 ? new Color(1.8f, 1.8f, 1.8f) : Colors.White, frame: frame, frameCount: style.Frames);
        }
        foreach (ref readonly var star in Run.Stars.Active)
        {
            var position = Palette.Vector(star.Position);
            if (!InView(position, 120)) continue;
            var size = 14 + MathF.Sqrt(star.Mass) * 8;
            var opacity = Math.Clamp(star.Life / 0.4f, 0, 1);
            if (star.Planet || star.Resonating)
            {
                var aura = MarisaTuning.DamageRadius(star.Mass) * 2;
                batches["stardust"].Add(position, new(aura, aura), new Color(1, 1, 1, opacity * (star.Resonating ? 0.35f : 0.16f)), -Clock * 0.5f);
            }
            batches["star"].Add(position, new(size, size), new Color(1, 1, 1, opacity), star.OrbitAngle + Clock);
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
            var name = projectile.DreamOrb ? "dream" : projectile.Art == ArtKind.YinYang ? "actors/yin_yang_orb" : "star";
            var size = projectile.Hostile ? 15 : projectile.DreamOrb ? projectile.Radius * 2.6f : projectile.Art == ArtKind.Ofuda ? 22 : projectile.Radius * 2.4f;
            var rotation = projectile.Art == ArtKind.Ofuda && !projectile.Hostile ? MathF.Atan2(projectile.Velocity.Y, projectile.Velocity.X) + MathF.PI / 2 : projectile.Hostile ? 0 : Clock * 3 + projectile.Life;
            var tint = projectile.DreamOrb ? DreamColors[projectile.TintIndex % DreamColors.Length] : Colors.White;
            var frames = projectile.Art == ArtKind.YinYang && !projectile.Hostile ? spriteFrames[name].Frames : 1;
            batches[name].Add(position, new(size, size), tint, rotation, (int)(Clock * 10) % frames, frames);
        }
        VisibleBatchInstances = 0;
        foreach (var batch in batches.Values) { batch.Submit(); VisibleBatchInstances += batch.Count; }
    }
}
