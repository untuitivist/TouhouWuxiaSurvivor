using Godot;

namespace Rebirth.Presentation;

public partial class CanvasPass : Node2D
{
    public Action<Node2D>? Paint;
    public override void _Draw() => Paint?.Invoke(this);
}
