using Godot;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 以墨线和朱砂节点绘制武学前置与特化关系；动画只表达脉络流动，不伪造购买状态。
/// </summary>
public static class CompendiumBuildPreviewRenderer
{
    /// <summary>根据条目类型选择中心印记，并绘制一主脉、两分支的紧凑关系图。</summary>
    public static void Draw(
        Control canvas,
        CompendiumEntry entry,
        Rect2 area,
        double animationTime,
        Font font)
    {
        Vector2 center = area.GetCenter() - new Vector2(0.0f, 3.0f);
        float pulse = 0.5f + MathF.Sin((float)animationTime * 3.0f) * 0.18f;
        Vector2[] nodes =
        [
            center + new Vector2(-36.0f, 21.0f),
            center,
            center + new Vector2(36.0f, -19.0f),
            center + new Vector2(36.0f, 21.0f),
        ];
        Color line = new(0.55f, 0.46f, 0.28f, 0.85f);
        canvas.DrawLine(nodes[0], nodes[1], line, 2.0f);
        canvas.DrawLine(nodes[1], nodes[2], line, 2.0f);
        canvas.DrawLine(nodes[1], nodes[3], line, 2.0f);
        for (int index = 0; index < nodes.Length; index++)
        {
            float radius = index == 1 ? 9.0f + pulse : 6.0f;
            canvas.DrawCircle(nodes[index], radius, new Color(0.08f, 0.07f, 0.045f, 0.96f));
            canvas.DrawCircle(nodes[index], radius - 2.0f,
                index == 1 ? new Color(0.76f, 0.18f, 0.14f) : line);
        }

        string mark = entry.Summary.StartsWith("武学", StringComparison.Ordinal)
            ? "武"
            : entry.Summary.Contains("特化", StringComparison.Ordinal) ? "变" : "心";
        DrawMark(canvas, font, mark, nodes[1]);
    }

    /// <summary>在主节点中心绘制带单像素暗影的中文印记。</summary>
    private static void DrawMark(Control canvas, Font font, string mark, Vector2 center)
    {
        const int fontSize = 12;
        Color color = new(0.98f, 0.92f, 0.76f);
        Vector2 size = font.GetStringSize(mark, HorizontalAlignment.Left, -1.0f, fontSize);
        Vector2 baseline = center + new Vector2(-size.X * 0.5f, size.Y * 0.35f);
        canvas.DrawString(font, baseline + Vector2.One, mark,
            HorizontalAlignment.Left, -1.0f, fontSize, new Color(0.0f, 0.0f, 0.0f));
        canvas.DrawString(font, baseline, mark,
            HorizontalAlignment.Left, -1.0f, fontSize, color);
    }
}
