using Godot;

namespace Rebirth.Presentation;

public readonly record struct ActorArtwork(Vector2 Origin, Vector2 FrameSize, int Columns, int Rows, float FootAnchor, Vector2 AtlasSize)
{
    public Rect2 Region(int frame, int row = 0)
        => new((Origin + new Vector2(Math.Abs(frame) % Columns * FrameSize.X, Math.Clamp(row, 0, Rows - 1) * FrameSize.Y)) / AtlasSize, FrameSize / AtlasSize);
}
