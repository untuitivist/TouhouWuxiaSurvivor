using Godot;

namespace TouhouWuxiaSurvivor.Ui.Compendium;

/// <summary>
/// 将结构化图鉴属性转换为宽行或双列行，使排版策略独立于图鉴选择逻辑。
/// </summary>
public static class CompendiumFactView
{
    /// <summary>
    /// 清空旧控件并按顺序重建属性；宽属性会先结束未配对的普通属性行。
    /// </summary>
    public static void Rebuild(VBoxContainer container, IReadOnlyList<CompendiumFact> facts)
    {
        Clear(container);
        CompendiumFact? pending = null;
        foreach (CompendiumFact fact in facts)
        {
            if (fact.IsWide)
            {
                if (pending is not null)
                {
                    container.AddChild(CreateRow(pending, null));
                    pending = null;
                }

                container.AddChild(CreateRow(fact, null, true));
            }
            else if (pending is null)
            {
                pending = fact;
            }
            else
            {
                container.AddChild(CreateRow(pending, fact));
                pending = null;
            }
        }

        if (pending is not null)
        {
            container.AddChild(CreateRow(pending, null));
        }
    }

    /// <summary>
    /// 从容器立即移除旧属性节点，再排队释放，避免一次选择变化中出现新旧行叠加。
    /// </summary>
    private static void Clear(VBoxContainer container)
    {
        while (container.GetChildCount() > 0)
        {
            Node child = container.GetChild(0);
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    /// <summary>
    /// 创建一行一至两组属性；宽属性占满整行，普通单数属性保留半行空位对齐。
    /// </summary>
    private static HBoxContainer CreateRow(
        CompendiumFact first, CompendiumFact? second, bool isWide = false)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 9);
        row.AddChild(CreatePair(first, isWide));
        if (second is not null)
        {
            row.AddChild(CreatePair(second, false));
        }
        else if (!isWide)
        {
            row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        }

        return row;
    }

    /// <summary>
    /// 创建固定键宽和弹性值宽的属性组，宽值可以在整行剩余空间内自然换行。
    /// </summary>
    private static HBoxContainer CreatePair(CompendiumFact fact, bool isWide)
    {
        var pair = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        pair.AddThemeConstantOverride("separation", 4);
        var key = new Label
        {
            Text = fact.Label,
            AutowrapMode = TextServer.AutowrapMode.Off,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(50.0f, 0.0f),
        };
        key.AddThemeFontSizeOverride("font_size", 10);
        key.AddThemeColorOverride("font_color", new Color("aebca6"));
        pair.AddChild(key);

        var value = new Label
        {
            Text = fact.Value,
            AutowrapMode = isWide
                ? TextServer.AutowrapMode.WordSmart
                : TextServer.AutowrapMode.Off,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        value.AddThemeFontSizeOverride("font_size", 10);
        pair.AddChild(value);
        return pair;
    }
}
