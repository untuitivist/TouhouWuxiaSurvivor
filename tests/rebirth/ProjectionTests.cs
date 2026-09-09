using System.Numerics;
using Rebirth.Presentation;

namespace Rebirth.Tests;

internal static class ProjectionTests
{
    public static void RoundTrip()
    {
        foreach (var point in new[] { Vector2.Zero, new Vector2(-1500, -1100), new Vector2(1500, 1100), new Vector2(37.25f, -143.5f) })
            Require(Vector2.Distance(point, WorldProjection.Unproject(WorldProjection.Project(point))) < 0.001f, "World/view conversion must round trip");
        var vertical = WorldProjection.Project(Vector2.UnitY);
        Require(vertical.Y > 0 && vertical.Y < 1 && vertical.X == 0, "Oblique depth affects the world plane, not its handedness");
        Require(MathF.Abs(WorldProjection.UprightScale * WorldProjection.DepthScale - 1) < 0.0001f, "Billboard compensation preserves height");
    }

    public static void Controls()
    {
        foreach (var input in new[] { Vector2.Zero, Vector2.UnitX, Vector2.UnitY, new Vector2(-1, -1), new Vector2(0.3f, -0.4f), new Vector2(2, 2) })
        {
            var movement = WorldProjection.ControlDirection(input);
            Require(MathF.Abs(movement.Length() - MathF.Min(1, input.Length())) < 0.0001f, "Projection adapter preserves analog strength and speed cap");
            if (input.LengthSquared() == 0) continue;
            var visible = Vector2.Normalize(WorldProjection.Project(movement));
            Require(Vector2.Distance(visible, Vector2.Normalize(input)) < 0.0001f, "Keyboard and touch movement follow screen direction");
        }
        Require(WorldProjection.FacingRow(Vector2.UnitY) == 0, "Down uses front frames");
        Require(WorldProjection.FacingRow(Vector2.UnitX) == 1 && WorldProjection.FacingRow(-Vector2.UnitX) == 3, "Left and right use independently drawn frames");
        Require(WorldProjection.FacingRow(-Vector2.UnitY) == 2, "Up uses back frames");
    }

    public static void Animation()
    {
        var animation = new HeroAnimation();
        Require(animation.Frame(0, 0, false, false, false) == 0, "Idle must not play casting frames");
        Require(animation.Frame(1, 1, false, false, false) == 5, "A real cooldown reset begins casting");
        Require(animation.Frame(1.12f, 0.88f, false, false, false) == 6, "Casting advances to release");
        Require(animation.Frame(1.2f, 0.8f, false, false, false) == 7, "Casting advances to recovery");
        Require(animation.Frame(1.4f, 0.6f, false, false, false) == 0, "Casting returns to idle");
        Require(animation.Frame(1.5f, 0.5f, true, false, false) is >= 1 and <= 4, "Walking only selects walk columns");
        Require(animation.Frame(1.6f, 0.4f, true, true, true) == 5, "Beam warmup poses independently of movement");
        Require(animation.Frame(1.7f, 0.3f, true, true, false) == 6, "Beam holds its release pose");
        Require(animation.Frame(0, 0, false, false, false) == 0, "A new run resets animation timing");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
