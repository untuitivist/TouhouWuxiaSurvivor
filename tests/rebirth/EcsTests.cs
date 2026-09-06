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

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
