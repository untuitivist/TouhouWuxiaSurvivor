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

        int schemaVersion = document.RootElement.GetProperty(
            "spellcard_schema_version").GetInt32();
        if (schemaVersion != 2)
        {
            throw new InvalidDataException(
                $"Unsupported spell card schema version: {schemaVersion}");
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
        RejectLegacyFields(card);
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
            ParseGeometry(RequiredString(card, "geometry")),
            ParseActivation(RequiredString(card, "activation")),
            RequiredString(card, "prerequisite"),
            RequiredInt(card, "minimum_rank"),
            new SpellCardCombatProfile(
                RequiredFloat(card, "interval_scale"),
                RequiredFloat(card, "range_scale"),
                RequiredFloat(card, "damage_scale"),
                RequiredFloat(card, "target_scale"),
                RequiredFloat(card, "activation_threshold_scale"),
                RequiredFloat(card, "defense_scale"),
                RequiredFloat(card, "projectile_speed_scale"),
                RequiredFloat(card, "impact_range_scale"),
                RequiredFloat(card, "travel_duration_scale"),
                RequiredFloat(card, "spawn_distance_scale")));
    }

    /// <summary>
    /// v2 明确拒绝旧充能、绝对值与战况触发字段，避免新旧语义混合后被 JSON 解析器静默忽略。
    /// </summary>
    private static void RejectLegacyFields(JsonElement card)
    {
        string[] legacyFields =
        [
            "trigger", "power_cost", "cooldown_seconds", "effect_range",
            "damage", "target_count", "defense_seconds",
        ];
        string? legacy = legacyFields.FirstOrDefault(
            field => card.TryGetProperty(field, out _));
        if (legacy is not null)
        {
            throw new InvalidDataException($"Spell schema v2 contains legacy field: {legacy}");
        }
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

    /// <summary>把空间组织字段映射为独立策略枚举，拒绝未实现的内容拼写或隐式默认值。</summary>
    private static SpellCardGeometryKind ParseGeometry(string value) => value switch
    {
        "orbit" => SpellCardGeometryKind.Orbit,
        "fan" => SpellCardGeometryKind.Fan,
        "line" => SpellCardGeometryKind.Line,
        "ring" => SpellCardGeometryKind.Ring,
        "backstab" => SpellCardGeometryKind.Backstab,
        _ => throw new InvalidDataException($"Unknown spell geometry: {value}"),
    };

    /// <summary>严格解析无资源消耗的自动运转方式，禁止内容包以任意字符串偷偷恢复灵力条件。</summary>
    private static SpellCardActivationKind ParseActivation(string value) => value switch
    {
        "periodic" => SpellCardActivationKind.Periodic,
        "crowd" => SpellCardActivationKind.Crowd,
        "on_damaged" => SpellCardActivationKind.OnDamaged,
        _ => throw new InvalidDataException($"Unknown spell activation: {value}"),
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
