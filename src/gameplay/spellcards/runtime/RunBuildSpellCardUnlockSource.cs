using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Gameplay.Progression.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Contracts;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

/// <summary>
/// 将本局构筑与内容快照适配成已悟奥义来源，是成长域和独立计时域之间的唯一连接点。
/// </summary>
public sealed class RunBuildSpellCardUnlockSource : ISpellCardUnlockSource
{
    private readonly RunBuildState _build;
    private readonly ContentPackSelection _content;

    /// <summary>保存本局不可替换的构筑与内容快照，后续查询只观察升级重数变化。</summary>
    public RunBuildSpellCardUnlockSource(
        RunBuildState build,
        ContentPackSelection content)
    {
        _build = build ?? throw new ArgumentNullException(nameof(build));
        _content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>从启用作品的目录中过滤已悟一重奥义，并保持正式目录顺序。</summary>
    public IReadOnlyList<SpellCardDefinition> GetUnlockedCards() =>
        SpellCardCatalog.GetEnabled(_content)
            .Where(card => _build.GetRank(card.UnlockUpgradeId) > 0)
            .ToArray();
}
