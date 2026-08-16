using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;

/// <summary>
/// 为运行期弹丸分配一开始于一的紧凑视觉编号；零始终表示默认弹幕图集。
/// </summary>
public static class SpellCardVisualBindingCatalog
{
    private static IReadOnlyDictionary<string, int>? _bindings;

    /// <summary>按稳定符卡 ID 返回运行期视觉编号，不存在时返回零以触发默认素材。</summary>
    public static int GetBindingId(string spellCardId)
    {
        _bindings ??= SpellCardCatalog.All
            .OrderBy(card => card.Id, StringComparer.Ordinal)
            .Select((card, index) => (card.Id, Binding: index + 1))
            .ToDictionary(item => item.Id, item => item.Binding, StringComparer.Ordinal);
        return _bindings.TryGetValue(spellCardId, out int binding) ? binding : 0;
    }
}
