using System.Numerics;

namespace Rebirth.Core;

public sealed class EnemyGrid
{
    private const int CellSize = 96;
    private readonly Dictionary<(int, int), List<Enemy>> cells = [];

    public void Rebuild(List<Enemy> enemies)
    {
        foreach (var bucket in cells.Values) bucket.Clear();
        foreach (var enemy in enemies)
        {
            if (enemy.Health <= 0) continue;
            var key = ((int)MathF.Floor(enemy.Position.X / CellSize), (int)MathF.Floor(enemy.Position.Y / CellSize));
            if (!cells.TryGetValue(key, out var bucket)) cells[key] = bucket = [];
            bucket.Add(enemy);
        }
    }

    public IEnumerable<Enemy> Query(Vector2 center, float radius)
    {
        var minimumX = (int)MathF.Floor((center.X - radius) / CellSize);
        var maximumX = (int)MathF.Floor((center.X + radius) / CellSize);
        var minimumY = (int)MathF.Floor((center.Y - radius) / CellSize);
        var maximumY = (int)MathF.Floor((center.Y + radius) / CellSize);
        for (var column = minimumX; column <= maximumX; column++)
        for (var row = minimumY; row <= maximumY; row++)
            if (cells.TryGetValue((column, row), out var bucket))
                foreach (var enemy in bucket)
                    if (enemy.Health > 0) yield return enemy;
    }
}
