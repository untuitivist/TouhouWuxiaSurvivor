using Godot;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

namespace TouhouWuxiaSurvivor.Ui.Hud.SpellCards;

/// <summary>
/// 管理 HUD 右上方最多六枚奥义印章，只在已悟列表改变时重建节点，平时仅刷新权威快照。
/// </summary>
public partial class SpellCardHudStrip : HBoxContainer
{
    private readonly Dictionary<string, SpellCardCooldownIcon> _icons =
        new(StringComparer.Ordinal);
    private string[] _displayedIds = [];

    public int VisibleIconCount => _icons.Count;

    /// <summary>声明紧凑间距和右对齐，使新增奥义从状态栏右侧稳定向左扩展。</summary>
    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 3);
        Alignment = AlignmentMode.End;
        MouseFilter = MouseFilterEnum.Pass;
        Hide();
    }

    /// <summary>按稳定展示序列同步图标集合，并逐张刷新冷却遮罩与待机提示。</summary>
    public void SetSnapshot(SpellCardRuntimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        string[] ids = snapshot.PresentationTimers.Select(timer => timer.Card.Id).ToArray();
        if (!_displayedIds.SequenceEqual(ids, StringComparer.Ordinal))
        {
            Rebuild(snapshot.PresentationTimers);
            _displayedIds = ids;
        }

        foreach (SpellCardTimerSnapshot timer in snapshot.PresentationTimers)
        {
            _icons[timer.Card.Id].SetTimer(timer);
        }

        Visible = _icons.Count > 0;
    }

    /// <summary>按当前展示索引返回图标，供正式界面验收读取而不暴露可变字典。</summary>
    public SpellCardCooldownIcon GetIcon(int index)
    {
        if (index < 0 || index >= _displayedIds.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _icons[_displayedIds[index]];
    }

    /// <summary>释放旧图标并按新序列建立固定尺寸节点；该路径只在悟得新奥义时执行。</summary>
    private void Rebuild(IReadOnlyList<SpellCardTimerSnapshot> timers)
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.Free();
        }

        _icons.Clear();
        foreach (SpellCardTimerSnapshot timer in timers)
        {
            var icon = new SpellCardCooldownIcon { Name = $"Spell{_icons.Count + 1}" };
            AddChild(icon);
            icon.SetTimer(timer);
            _icons.Add(timer.Card.Id, icon);
        }
    }
}
