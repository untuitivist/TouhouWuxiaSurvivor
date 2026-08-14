using Godot;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>
/// 保存当前画布可见的 ECS 局部世界矩形，并统一提供带实体半径的 CPU 剔除判断。
/// </summary>
public readonly record struct CombatVisibilityBounds(Rect2 Rect)
{
    public const float Margin = 96.0f;

    /// <summary>
    /// 把视口四角逆变换到战斗画布局部坐标，再扩展余量容纳 Boss 血条、文字和高速边缘实体。
    /// </summary>
    public static CombatVisibilityBounds FromCanvas(Node2D canvas)
    {
        Rect2 viewport = canvas.GetViewport().GetVisibleRect();
        Transform2D inverse = canvas.GetGlobalTransformWithCanvas().AffineInverse();
        Vector2 first = inverse * viewport.Position;
        Vector2 second = inverse * new Vector2(viewport.End.X, viewport.Position.Y);
        Vector2 third = inverse * viewport.End;
        Vector2 fourth = inverse * new Vector2(viewport.Position.X, viewport.End.Y);
        float left = Math.Min(Math.Min(first.X, second.X), Math.Min(third.X, fourth.X));
        float top = Math.Min(Math.Min(first.Y, second.Y), Math.Min(third.Y, fourth.Y));
        float right = Math.Max(Math.Max(first.X, second.X), Math.Max(third.X, fourth.X));
        float bottom = Math.Max(Math.Max(first.Y, second.Y), Math.Max(third.Y, fourth.Y));
        var rect = new Rect2(left - Margin, top - Margin,
            right - left + Margin * 2.0f, bottom - top + Margin * 2.0f);
        return new(rect);
    }

    /// <summary>
    /// 判断以 position 为中心的圆是否触及可视矩形，半径使大 Boss 与边缘反馈不会过早消失。
    /// </summary>
    public bool Intersects(Vector2 position, float radius = 0.0f)
    {
        float safeRadius = Math.Max(0.0f, radius);
        return position.X + safeRadius >= Rect.Position.X &&
            position.Y + safeRadius >= Rect.Position.Y &&
            position.X - safeRadius <= Rect.End.X &&
            position.Y - safeRadius <= Rect.End.Y;
    }
}
