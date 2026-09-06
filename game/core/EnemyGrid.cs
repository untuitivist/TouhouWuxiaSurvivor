using System.Numerics;

namespace Rebirth.Core;

public sealed class EnemyGrid
{
    private const int CellSize = 96;
    private readonly Dictionary<(int, int), List<Enemy>> cells = [];
    private readonly Dictionary<int, Enemy> identities = new(RunState.EnemyLimit + 4);

    public void Rebuild(ComponentStore<Enemy> enemies)
    {
        identities.Clear();
        foreach (var bucket in cells.Values) bucket.Clear();
        foreach (var enemy in enemies)
        {
            if (enemy.Health <= 0) continue;
            identities.TryAdd(enemy.Id, enemy);
            var key = ((int)MathF.Floor(enemy.Position.X / CellSize), (int)MathF.Floor(enemy.Position.Y / CellSize));
            if (!cells.TryGetValue(key, out var bucket)) cells[key] = bucket = [];
            bucket.Add(enemy);
        }
    }

    public Enemy? FindById(int identity) => identities.GetValueOrDefault(identity);

    public QueryEnumerator Query(Vector2 center, float radius) => new(cells, center, radius);

    public struct QueryEnumerator
    {
        private readonly Dictionary<(int, int), List<Enemy>> cells;
        private readonly int maximumX;
        private readonly int minimumY;
        private readonly int maximumY;
        private int column;
        private int row;
        private int offset;
        private List<Enemy>? bucket;
        public Enemy Current { get; private set; }

        internal QueryEnumerator(Dictionary<(int, int), List<Enemy>> cells, Vector2 center, float radius)
        {
            this.cells = cells;
            column = (int)MathF.Floor((center.X - radius) / CellSize);
            maximumX = (int)MathF.Floor((center.X + radius) / CellSize);
            minimumY = (int)MathF.Floor((center.Y - radius) / CellSize);
            maximumY = (int)MathF.Floor((center.Y + radius) / CellSize);
            row = minimumY - 1;
            offset = 0;
            bucket = null;
            Current = null!;
        }

        public QueryEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            while (column <= maximumX)
            {
                while (bucket != null && offset < bucket.Count)
                {
                    Current = bucket[offset++];
                    if (Current.Health > 0) return true;
                }
                if (++row > maximumY) { column++; row = minimumY; }
                if (column > maximumX) break;
                cells.TryGetValue((column, row), out bucket);
                offset = 0;
            }
            return false;
        }
    }
}
