using Godot;

namespace TouhouWuxiaSurvivor.Ecs.Combat.Bosses;

/// <summary>
/// 在角色 Boss 切换符卡时绘制短暂招式名，不把内容字符串或 UI 节点写入弹幕系统。
/// </summary>
public static class BossSpellPresentationRenderer
{
    /// <summary>在生命条上方绘制固定尺寸的符卡条；倒计时结束或名称为空时不提交绘制。</summary>
    public static void Draw(
        Node2D canvas,
        Font? font,
        EnemyComponent enemy,
        Vector2 position)
    {
        if (enemy.SpellAnnouncementTime <= 0.0f ||
            string.IsNullOrWhiteSpace(enemy.ActiveSpellName))
        {
            return;
        }

        const float width = 112.0f;
        const float height = 15.0f;
        Vector2 origin = (position + new Vector2(-width * 0.5f,
            -enemy.Definition.CollisionRadius - 31.0f)).Round();
        var bounds = new Rect2(origin, new Vector2(width, height));
        canvas.DrawRect(bounds, new Color(0.035f, 0.025f, 0.03f, 0.88f));
        canvas.DrawRect(bounds, new Color("b6424c"), false, 1.0f);
        canvas.DrawString(font, origin + new Vector2(4.0f, 11.0f),
            enemy.ActiveSpellName, HorizontalAlignment.Center, width - 8.0f,
            9, new Color("fff0dd"));
    }
}
