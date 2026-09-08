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

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
