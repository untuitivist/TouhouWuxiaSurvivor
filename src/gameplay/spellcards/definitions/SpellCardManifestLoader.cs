using System.Text.Json;
using Godot;
using TouhouWuxiaSurvivor.Content.Characters;

namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 从作品内容包解析结构化符卡数组，并在加载边界完成角色、枚举与数值字段校验。
/// </summary>
public static class SpellCardManifestLoader
{
    /// <summary>
    /// 读取一部作品的全部符卡；没有 spellcards 字段时返回空数组，便于本体清单保持无符卡状态。
    /// </summary>
    public static IReadOnlyList<SpellCardDefinition> Load(string manifestPath, string sourcePackId)
    {
        using JsonDocument document = JsonDocument.Parse(
            Godot.FileAccess.GetFileAsString(manifestPath));
        if (!document.RootElement.TryGetProperty("spellcards", out JsonElement cards))
        {
            return [];
        }

        return cards.EnumerateArray()
            .Select(card => Parse(card, sourcePackId))
            .ToArray();
    }

    /// <summary>
    /// 把单项 JSON 转成强类型定义，并将中文 owner 立即解析为稳定 CharacterId。
    /// </summary>
    private static SpellCardDefinition Parse(JsonElement card, string sourcePackId)
    {
        string ownerName = RequiredString(card, "owner");
        CharacterDefinition owner = CharacterCatalog.GetRequiredByDisplayName(ownerName);
        return new SpellCardDefinition(
            RequiredString(card, "id"),
            sourcePackId,
            RequiredString(card, "name"),
            RequiredString(card, "short_name"),
            owner.CharacterId,
            owner.DisplayName,
            ParseCanon(RequiredString(card, "canon")),
            RequiredString(card, "source_note"),
            RequiredString(card, "wuxia_style"),
            RequiredString(card, "description"),
            ParseEffect(RequiredString(card, "effect")),
            ParseTrigger(RequiredString(card, "trigger")),
            RequiredString(card, "prerequisite"),
            RequiredInt(card, "minimum_rank"),
            new SpellCardCombatProfile(
                RequiredInt(card, "power_cost"),
                RequiredFloat(card, "cooldown_seconds"),
                RequiredFloat(card, "effect_range"),
                RequiredInt(card, "damage"),
                RequiredInt(card, "target_count"),
                RequiredFloat(card, "defense_seconds")));
    }

    /// <summary>把清单规范层字符串严格映射为枚举，拒绝未声明的来源语义。</summary>
    private static SpellCardCanonLevel ParseCanon(string value) => value switch
    {
        "official" => SpellCardCanonLevel.Official,
        "adapted_pre_spell_card" => SpellCardCanonLevel.AdaptedPreSpellCard,
        _ => throw new InvalidDataException($"Unknown spell canon level: {value}"),
    };

    /// <summary>把四类共享效果标识转换为运行时枚举，清单拼写错误会立即终止加载。</summary>
    private static SpellCardEffectKind ParseEffect(string value) => value switch
    {
        "homing_volley" => SpellCardEffectKind.HomingVolley,
        "focused_volley" => SpellCardEffectKind.FocusedVolley,
        "area_burst" => SpellCardEffectKind.AreaBurst,
        "guard_field" => SpellCardEffectKind.GuardField,
        _ => throw new InvalidDataException($"Unknown spell effect: {value}"),
    };

    /// <summary>把自动触发标识转换为运行时枚举，确保数据不能偷偷增加主动输入路径。</summary>
    private static SpellCardTriggerKind ParseTrigger(string value) => value switch
    {
        "crowd" => SpellCardTriggerKind.Crowd,
        "danger" => SpellCardTriggerKind.Danger,
        "single_target" => SpellCardTriggerKind.SingleTarget,
        _ => throw new InvalidDataException($"Unknown spell trigger: {value}"),
    };

    /// <summary>读取非空字符串字段并给出包含字段名的清单错误。</summary>
    private static string RequiredString(JsonElement source, string name)
    {
        string? value = source.GetProperty(name).GetString();
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidDataException($"Spell field is empty: {name}");
    }

    /// <summary>读取必需整数，字段缺失或类型错误由 JSON API 直接给出精确异常。</summary>
    private static int RequiredInt(JsonElement source, string name) =>
        source.GetProperty(name).GetInt32();

    /// <summary>读取必需浮点数，使整数与小数 JSON 表达都进入同一战斗参数类型。</summary>
    private static float RequiredFloat(JsonElement source, string name) =>
        source.GetProperty(name).GetSingle();
}
