using System.Numerics;
using Rebirth.Core;

namespace Rebirth.Tests;

public static class EcsTests
{
    public static void Storage()
    {
        var store = new ComponentStore<Projectile>(4);
        for (var index = 0; index < 20; index++) store.Add(new() { TargetId = index, Life = index });
        store[4].Damage = 17;
        Check(store[4].Damage == 17, "Indexed component mutations persist");
        Check(store.RemoveAll(static projectile => projectile.TargetId % 2 == 0) == 10, "One compaction removes all marked slots");
        Check(store.Select(projectile => projectile.TargetId).SequenceEqual(Enumerable.Range(0, 10).Select(index => index * 2 + 1)), "Compaction preserves deterministic entity order");
        store.RemoveAt(0);
        Check(store[0].TargetId == 3 && store.Count == 9, "Removal has no stale front slot");
        store.Clear();
        store.Add(new() { Life = 2 });
        Check(store.Count == 1 && !store[0].HitIds.Contains(7), "Fresh entities have no previous state");
        var visits = 0;
        Check(store.RemoveAll(projectile => { visits++; return projectile.Life < 0; }) == 0 && visits == 1 && store[0].Life == 2, "Unchanged storage visits each element once and retains state");
        store.Add(new() { Life = -1 });
        store.Add(new() { Life = 3 });
        Check(store.RemoveAll(static projectile => projectile.Life < 0) == 1 && store.Count == 2 && store[1].Life == 3, "Compaction retains an untouched prefix and shifts survivors after the first hole");
        Check(store.RemoveAll(static projectile => true) == 2 && store.Count == 0, "Compaction clears every slot when all entities expire");
        Check(store.RemoveAll(static projectile => true) == 0, "Empty storage is safe to compact");
    }

    public static void History()
    {
        var history = new HitHistory();
        for (var identity = 0; identity < 12; identity++) history.Add(identity);
        for (var identity = 0; identity < 12; identity++) Check(history.Contains(identity), "Inline and overflow hits remain excluded");
        Check(!history.Contains(13), "Unseen targets remain hittable");
        history.Add(3);
        Check(history.Contains(11), "Repeated hits do not erase overflow entries");
    }

    public static void Queries()
    {
        var enemies = new ComponentStore<Enemy>(4);
        enemies.Add(new() { Id = 17, Position = new(-20, -20), Health = 10 });
        enemies.Add(new() { Id = 18, Position = new(20, 20), Health = 10 });
        var grid = new EnemyGrid();
        grid.Rebuild(enemies);
        Check(grid.FindById(17) == enemies[0], "Stable identity resolves its managed enemy component");
        var count = 0;
        foreach (var enemy in grid.Query(Vector2.Zero, 40)) count++;
        Check(count == 2, "Queries span negative and positive cells");
        enemies[0].Health = 0;
        count = 0;
        foreach (var enemy in grid.Query(Vector2.Zero, 40)) count++;
        Check(count == 1, "An enemy killed after grid build cannot be hit again");
        grid.Rebuild(enemies);
        Check(grid.FindById(17) == null, "Dead identities are cleared on rebuild");
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 100; iteration++) foreach (var enemy in grid.Query(Vector2.Zero, 40)) count++;
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Hot spatial queries allocate no iterator objects");
    }

    public static void GeometryEquivalence()
    {
        var random = new Random(71);
        Vector2 Point() => new(random.Next(-2000, 2000), random.Next(-1500, 1500));
        for (var index = 0; index < 10000; index++)
        {
            var from = (float)(random.NextDouble() * MathF.Tau - MathF.PI);
            var to = (float)(random.NextDouble() * MathF.Tau - MathF.PI);
            var referenceAngle = MathF.Atan2(MathF.Sin(to - from), MathF.Cos(to - from));
            Check(Math.Abs(Geometry.AngleDelta(from, to) - referenceAngle) < 0.000001f, "Bounded heading difference matches shortest-angle reference");
            var start = Point();
            var end = index % 10 == 0 ? start : Point();
            var point = Point();
            var segment = end - start;
            var length = segment.LengthSquared();
            var fraction = length < 0.0001f ? 0 : Math.Clamp(Vector2.Dot(point - start, segment) / length, 0, 1);
            var expected = Vector2.DistanceSquared(point, start + segment * fraction);
            var actual = Geometry.SegmentDistanceSquared(point, start, end);
            Check(Math.Abs(Geometry.DistanceSquared(start, end) - length) <= Math.Max(0.001f, length * 0.00001f), "Scalar squared distance matches vector reference");
            Check(Math.Abs(Geometry.Length(segment) - segment.Length()) < 0.001f, "Scalar length matches vector reference");
            Check(Vector2.DistanceSquared(Geometry.Advance(start, segment, 1f / 60), start + segment * (1f / 60)) < 0.000001f, "Scalar movement retains fixed-step displacement");
            Check(Math.Abs(actual - expected) <= Math.Max(0.001f, expected * 0.00001f), "Scalar swept geometry retains segment distances");
            var direction = Geometry.Direction(segment);
            var expectedDirection = length > 0.0001f ? Vector2.Normalize(segment) : Vector2.UnitY;
            Check(Vector2.DistanceSquared(direction, expectedDirection) < 0.00000001f, "Scalar direction retains normalization and zero fallback");
        }
        Check(Geometry.SegmentDistanceSquared(new(50, 5), Vector2.Zero, new(100, 0)) == 25, "Swept path still catches a small target between endpoints");
    }

    public static void GridEquivalence()
    {
        var random = new Random(73);
        var enemies = new ComponentStore<Enemy>(400);
        var grid = new EnemyGrid();
        for (var index = 0; index < 400; index++)
            enemies.Add(new() { Id = index + 1, Position = new(random.Next(-2600, 2600), random.Next(-2000, 2000)), Health = 10 });
        for (var pass = 0; pass < 4; pass++)
        {
            grid.Rebuild(enemies);
            for (var query = 0; query < 100; query++)
            {
                var center = new Vector2(random.Next(-2200, 2200), random.Next(-1800, 1800));
                var radius = random.Next(1, 180);
                var minimumColumn = (int)MathF.Floor((center.X - radius) / 96);
                var maximumColumn = (int)MathF.Floor((center.X + radius) / 96);
                var minimumRow = (int)MathF.Floor((center.Y - radius) / 96);
                var maximumRow = (int)MathF.Floor((center.Y + radius) / 96);
                var expected = enemies.Where(enemy => enemy.Health > 0 && MathF.Floor(enemy.Position.X / 96) >= minimumColumn && MathF.Floor(enemy.Position.X / 96) <= maximumColumn
                    && MathF.Floor(enemy.Position.Y / 96) >= minimumRow && MathF.Floor(enemy.Position.Y / 96) <= maximumRow)
                    .OrderBy(enemy => MathF.Floor(enemy.Position.X / 96)).ThenBy(enemy => MathF.Floor(enemy.Position.Y / 96)).Select(enemy => enemy.Id).ToArray();
                var actual = new List<int>();
                foreach (var enemy in grid.Query(center, radius)) actual.Add(enemy.Id);
                Check(actual.SequenceEqual(expected), "Dense and overflow cells preserve candidate order across rebuilds");
                var start = center + new Vector2(40, -25);
                var end = center - new Vector2(35, -15);
                var expectedSwept = new List<int>();
                foreach (var enemy in grid.Query(center, radius))
                {
                    var hitRadius = enemy.Radius + 8;
                    if (enemy.Position.X >= MathF.Min(start.X, end.X) - hitRadius && enemy.Position.X <= MathF.Max(start.X, end.X) + hitRadius
                        && enemy.Position.Y >= MathF.Min(start.Y, end.Y) - hitRadius && enemy.Position.Y <= MathF.Max(start.Y, end.Y) + hitRadius) expectedSwept.Add(enemy.Id);
                }
                var actualSwept = new List<int>();
                foreach (var enemy in grid.QuerySwept(center, radius, start, end, 8)) actualSwept.Add(enemy.Id);
                Check(actualSwept.SequenceEqual(expectedSwept), "Swept broad phase preserves filtered candidate order");
            }
            foreach (var enemy in enemies) { enemy.Position = -enemy.Position; if (enemy.Id % 7 == 0) enemy.Health = 0; }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
