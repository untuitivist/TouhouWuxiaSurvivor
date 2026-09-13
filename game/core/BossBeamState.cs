using System.Numerics;

namespace Rebirth.Core;

public sealed class BossBeamState
{
    public const float TelegraphSeconds = 1.5f;
    public Vector2 Origin { get; }
    public Vector2 Direction { get; }
    public int Count { get; }
    public float Length => 950;
    public float HalfWidth => 28;
    public float Damage => 16;
    public float Warmup = TelegraphSeconds;
    public float Remaining = 1.6f;
    public float PulseTimer;

    public BossBeamState(Vector2 origin, Vector2 direction, int phase)
    {
        Origin = origin;
        Direction = Geometry.Direction(direction);
        Count = phase == 2 ? 3 : 1;
    }

    public Vector2 Bearing(int index) => Geometry.Rotate(Direction, (index - (Count - 1) * 0.5f) * 0.6f);
    public bool InLane(Vector2 point, float padding = 0)
    {
        var offset = point - Origin;
        for (var index = 0; index < Count; index++)
        {
            var direction = Bearing(index);
            var along = Vector2.Dot(offset, direction);
            var across = Math.Abs(offset.X * direction.Y - offset.Y * direction.X);
            if (along >= 0 && along <= Length && across <= HalfWidth + padding) return true;
        }
        return false;
    }

    public bool Contains(Vector2 point, float padding = 0) => Warmup <= 0 && Remaining > 0 && InLane(point, padding);
}
