using System.Numerics;

namespace Rebirth.Core;

public sealed class EnemyGrid
{
    private const int CellSize = 96;
    private const int OriginX = -((int)RunState.ArenaHalfWidth / CellSize + 1);
    private const int OriginY = -((int)RunState.ArenaHalfHeight / CellSize + 1);
    private const int Columns = -OriginX * 2;
    private const int Rows = -OriginY * 2;
    private readonly List<Enemy>?[] cells = new List<Enemy>?[Columns * Rows];
    private readonly Dictionary<(int, int), List<Enemy>> overflow = [];
    private readonly List<List<Enemy>> occupied = new(RunState.EnemyLimit);
    private readonly Dictionary<int, Enemy> identities = new(RunState.EnemyLimit + 4);

    public void Rebuild(ComponentStore<Enemy> enemies)
    {
        identities.Clear();
        foreach (var bucket in occupied) bucket.Clear();
        occupied.Clear();
        foreach (var enemy in enemies)
        {
            if (enemy.Health <= 0) continue;
            identities.TryAdd(enemy.Id, enemy);
            var column = (int)MathF.Floor(enemy.Position.X / CellSize);
            var row = (int)MathF.Floor(enemy.Position.Y / CellSize);
            var bucket = GetBucket(column, row);
            if (bucket == null)
            {
                bucket = [];
                if ((uint)(column - OriginX) < Columns && (uint)(row - OriginY) < Rows)
                    cells[(column - OriginX) * Rows + row - OriginY] = bucket;
                else overflow[(column, row)] = bucket;
            }
            if (bucket.Count == 0) occupied.Add(bucket);
            bucket.Add(enemy);
        }
    }

    public Enemy? FindById(int identity) => identities.GetValueOrDefault(identity);

    private List<Enemy>? GetBucket(int column, int row)
    {
        var localColumn = column - OriginX;
        var localRow = row - OriginY;
        return (uint)localColumn < Columns && (uint)localRow < Rows
            ? cells[localColumn * Rows + localRow] : overflow.GetValueOrDefault((column, row));
    }

    public QueryEnumerator Query(Vector2 center, float radius) => new(this, center, radius);

    public struct QueryEnumerator
    {
        private readonly EnemyGrid grid;
        private readonly int maximumX;
        private readonly int minimumY;
        private readonly int maximumY;
        private int column;
        private int row;
        private int offset;
        private List<Enemy>? bucket;
        public Enemy Current { get; private set; }

        internal QueryEnumerator(EnemyGrid grid, Vector2 center, float radius)
        {
            this.grid = grid;
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
                bucket = grid.GetBucket(column, row);
                offset = 0;
            }
            return false;
        }
    }
}
