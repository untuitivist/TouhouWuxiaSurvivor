using System.Numerics;

namespace Rebirth.Presentation;

public static class WorldProjection
{
    public const float DepthScale = 0.72f;
    public const float UprightScale = 1 / DepthScale;

    public static Vector2 Project(Vector2 position) => new(position.X, position.Y * DepthScale);
    public static Vector2 Unproject(Vector2 position) => new(position.X, position.Y / DepthScale);

    public static Vector2 ControlDirection(Vector2 screenDirection)
    {
        var strength = MathF.Min(1, screenDirection.Length());
        if (strength <= 0.0001f) return Vector2.Zero;
        return Vector2.Normalize(Unproject(screenDirection)) * strength;
    }

    public static int FacingRow(Vector2 worldDirection)
    {
        var projected = Project(worldDirection);
        return MathF.Abs(projected.X) > MathF.Abs(projected.Y) ? projected.X < 0 ? 3 : 1 : projected.Y < 0 ? 2 : 0;
    }
}
