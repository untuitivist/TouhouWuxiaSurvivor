using Godot;

namespace Rebirth.Presentation;

internal readonly record struct ActorInstance(Vector2 Foot, Vector2 Size, float Anchor, Rect2 Region, Color Tint, bool Mirror, int Sequence) : IComparable<ActorInstance>
{
    public int CompareTo(ActorInstance other)
    {
        var depth = Foot.Y.CompareTo(other.Foot.Y);
        return depth == 0 ? Sequence.CompareTo(other.Sequence) : depth;
    }
}
