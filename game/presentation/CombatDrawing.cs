using Godot;
using Rebirth.Core;

namespace Rebirth.Presentation;

public partial class GameCanvas
{
    private void DrawCombat()
    {
        if (Run == null) return;
        foreach (var pickup in Run.Pickups)
        {
            var position = Palette.Vector(pickup.Position);
            if (position.DistanceSquaredTo(camera) > 820 * 820) continue;
            var color = pickup.Healing ? Palette.Red : Palette.Jade;
            DrawCircle(position, 8, Palette.Alpha(color, 0.09f));
            Diamond(position, pickup.Healing ? 6 : 4, color);
            if (pickup.Healing) { DrawLine(position + new Vector2(-3, 0), position + new Vector2(3, 0), Palette.Paper, 2); DrawLine(position + new Vector2(0, -3), position + new Vector2(0, 3), Palette.Paper, 2); }
        }
        foreach (var enemy in Run.Enemies)
        {
            var position = Palette.Vector(enemy.Position);
            if (position.DistanceSquaredTo(camera) > 850 * 850) continue;
            if (enemy.Telegraph > 0 && enemy.Kind == EnemyKind.Charger)
            {
                var target = position + Palette.Vector(enemy.Aim) * 265;
                DrawLine(position, target, Palette.Alpha(Palette.Red, 0.18f), 30);
                DrawLine(position, target, Palette.Alpha(Palette.Red, 0.65f), 2);
            }
            DrawCircle(position + new Vector2(0, 4), enemy.Radius, new Color(0, 0, 0, 0.2f));
            var name = enemy.Kind switch { EnemyKind.Kedama => "actors/kedama", EnemyKind.Fairy => "actors/wild_fairy", EnemyKind.Charger => "actors/mountain_spirit", EnemyKind.Elite => "actors/great_youkai", _ => "players/marisa" };
            var scale = enemy.Kind switch { EnemyKind.Kedama => 0.95f, EnemyKind.Elite => 1.7f, EnemyKind.Boss => 2.0f, _ => 1.15f };
            if (enemy.Kind is EnemyKind.Elite or EnemyKind.Boss)
            {
                var color = enemy.Kind == EnemyKind.Boss ? Palette.Violet : Palette.Red;
                DrawArc(position, enemy.Radius + 9, Clock, Clock + MathF.Tau, 36, Palette.Alpha(color, 0.6f), 2);
                if (enemy.Telegraph > 0) DrawArc(position, 55 + enemy.Telegraph * 35, 0, MathF.Tau, 40, Palette.Alpha(color, 0.6f), 2);
                DrawRect(new(position + new Vector2(-27, -65), new(54, 4)), Palette.Deep);
                DrawRect(new(position + new Vector2(-27, -65), new(54 * Math.Max(0, enemy.Health / enemy.MaxHealth), 4)), color);
            }
            Sprite(name, position, scale, enemy.Id * 0.7f, enemy.Flash > 0 ? new Color(1.8f, 1.8f, 1.8f) : Colors.White);
        }
        foreach (var projectile in Run.Projectiles.Where(projectile => !projectile.Hostile)) DrawProjectile(projectile);
        var player = Palette.Vector(Run.PlayerPosition);
        var orbitRank = Run.Ranks[(int)ArtKind.Orbit];
        if (orbitRank > 0)
        {
            DrawArc(player, Run.OrbitRadius, 0, MathF.Tau, 64, Palette.Alpha(Palette.Jade, 0.10f), 1);
            for (var index = 0; index < orbitRank + 1; index++)
            {
                var position = player + Vector2.FromAngle(Run.OrbitAngle + index * MathF.Tau / (orbitRank + 1)) * Run.OrbitRadius;
                DrawCircle(position, 14, Palette.Alpha(Palette.Jade, 0.12f));
                Sprite("actors/yin_yang_orb", position + new Vector2(0, 7), 0.62f);
            }
        }
        DrawCircle(player + new Vector2(0, 8), 17, new Color(0, 0, 0, 0.3f));
        if (Run.DashDuration > 0)
            for (var index = 1; index <= 4; index++)
                Sprite(Run.Hero == HeroKind.Reimu ? "players/reimu" : "players/marisa", player - Palette.Vector(Run.Facing) * (index * 18), 1.45f, 0, Palette.Alpha(Palette.Jade, 0.35f - index * 0.06f));
        var playerTint = Run.Invulnerability > 0 && (int)(Clock * 20) % 2 == 0 ? new Color(1, 1, 1, 0.4f) : Colors.White;
        Sprite(Run.Hero == HeroKind.Reimu ? "players/reimu" : "players/marisa", player, 1.45f, 0, playerTint);
        foreach (var projectile in Run.Projectiles.Where(projectile => projectile.Hostile)) DrawProjectile(projectile);
        if (Focused)
        {
            DrawArc(player, 34, 0, MathF.Tau, 40, Palette.Alpha(Palette.Paper, 0.35f), 1);
            DrawCircle(player, 6, Palette.Deep);
            DrawCircle(player, 3.5f, Palette.Paper);
            DrawArc(player, 8, 0, MathF.Tau, 24, Palette.Red, 1);
        }
        else DrawCircle(player, 2, Palette.Alpha(Palette.Paper, 0.8f));
        DrawEffects();
    }

    private void DrawProjectile(Projectile projectile)
    {
        var position = Palette.Vector(projectile.Position);
        if (position.DistanceSquaredTo(camera) > 850 * 850) return;
        var direction = Palette.Vector(projectile.Velocity).Normalized();
        if (projectile.Hostile)
        {
            var color = projectile.Alternate ? Palette.Violet : Palette.Red;
            DrawCircle(position, 10, Palette.Alpha(color, 0.1f));
            DrawCircle(position, 6.5f, new Color("272a36"));
            DrawCircle(position, 5.5f, color);
            DrawCircle(position, 2.5f, Palette.Paper);
        }
        else if (projectile.Art == ArtKind.Talisman)
        {
            DrawCircle(position, 14, Palette.Alpha(Palette.Red, 0.1f));
            DrawLine(position - direction * 9, position + direction * 9, Palette.Paper, 7);
            DrawLine(position - direction * 5, position + direction * 5, Palette.Red, 3);
        }
        else
        {
            DrawLine(position - direction * 30, position, Palette.Alpha(Palette.Gold, 0.12f), 9);
            DrawLine(position - direction * 19, position + direction * 10, Palette.Gold, 3);
            DrawLine(position - direction * 5, position + direction * 10, Palette.Paper, 2);
            var cross = direction.Orthogonal() * 5;
            DrawLine(position - direction * 12 - cross, position - direction * 12 + cross, Palette.Gold, 2);
        }
    }

    private void DrawEffects()
    {
        foreach (var effect in effects)
        {
            var position = Palette.Vector(effect.Entry.Position);
            var progress = effect.Age / effect.Duration;
            var color = Palette.Alpha(Palette.Paper, 1 - progress);
            switch (effect.Entry.Kind)
            {
                case EffectKind.Hit:
                    Text(((int)effect.Entry.Value).ToString(), position + new Vector2(-8, -30 - progress * 22), 13, Palette.Alpha(Palette.Gold, 1 - progress));
                    break;
                case EffectKind.Lightning:
                    var target = Palette.Vector(effect.Entry.Target);
                    var middle = (position + target) / 2 + (target - position).Normalized().Orthogonal() * 18;
                    DrawPolyline([position, middle, target], Palette.Alpha(Palette.Violet, 0.18f * (1 - progress)), 10);
                    DrawPolyline([position, middle, target], color, 2);
                    break;
                case EffectKind.Explosion:
                    DrawCircle(position, effect.Entry.Value * progress, Palette.Alpha(Palette.Red, 0.12f * (1 - progress)));
                    DrawArc(position, effect.Entry.Value * progress, 0, MathF.Tau, 40, Palette.Alpha(Palette.Red, 1 - progress), 2);
                    break;
                case EffectKind.Burst:
                    DrawArc(position, 540 * progress, 0, MathF.Tau, 96, Palette.Alpha(Palette.Gold, 1 - progress), ReducedMotion ? 2 : 5);
                    break;
                case EffectKind.Seal:
                    DrawArc(position, 160 * progress, 0, MathF.Tau, 48, Palette.Alpha(Palette.Jade, 1 - progress), 3);
                    break;
                case EffectKind.Graze:
                    DrawArc(position, 20 + progress * 20, 0, MathF.Tau, 24, Palette.Alpha(Palette.Jade, 1 - progress), 1);
                    break;
                case EffectKind.Defeat:
                    for (var index = 0; index < 5; index++) Diamond(position + Vector2.FromAngle(index * MathF.Tau / 5) * progress * 25, 3 * (1 - progress), Palette.Alpha(Palette.Gold, 1 - progress));
                    break;
            }
        }
    }
}
