using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;

/// <summary>
/// 把弹型选择、世界位置与速度提交为统一 Canvas 绘制指令，供正式 ECS 和兼容运行时复用。
/// </summary>
public static class ProjectileVisualDrawHelper
{
    /// <summary>在像素网格位置绘制弹丸；方向型帧围绕中心旋转，并在提交后恢复 Canvas 变换。</summary>
    public static void Draw(
        Node2D canvas,
        Texture2D texture,
        SpellBulletVisualSelection selection,
        Vector2 position,
        Vector2 velocity)
    {
        float rotation = ProjectileVisualPosePolicy.ResolveRotation(selection.Style, velocity);
        Vector2 roundedPosition = position.Round();
        if (Mathf.IsZeroApprox(rotation))
        {
            canvas.DrawTextureRectRegion(texture,
                selection.CreateDestination(roundedPosition), selection.Source);
            return;
        }

        Vector2 size = Vector2.One * selection.DisplaySize;
        canvas.DrawSetTransform(roundedPosition, rotation, Vector2.One);
        canvas.DrawTextureRectRegion(texture, new Rect2(size * -0.5f, size), selection.Source);
        canvas.DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }
}
