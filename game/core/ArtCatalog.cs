namespace Rebirth.Core;

public sealed record ArtDefinition(ArtKind Id, string Name, string School, string Description, string Mastery, string Color, int MaxRank, HeroKind? Owner = null, string Source = "", string Symbol = "");

public static class ArtCatalog
{
    public static readonly ArtDefinition[] All =
    [
        new(ArtKind.Ofuda, "御札", "灵梦 · 符", "向目标发射直线御札；追踪与爆炸需分别领悟，可兼修。", "每轮六张御札；行为效果由已选分支决定。", "ef9fb3", 5, HeroKind.Reimu, "红魔乡/永夜抄说明书：御札；本作成长转译", "札"),
        new(ArtKind.YinYang, "阴阳玉", "灵梦 · 护身", "阴阳玉绕身，击退近敌；走位决定接触范围。", "六枚阴阳玉，回旋半径扩大至 115。", "8cdcc8", 5, HeroKind.Reimu, "红魔乡说明书：阴阳玉；本作回旋转译", "玉"),
        new(ArtKind.Boundary, "封魔阵", "灵梦 · 留阵", "在脚下留下方形封魔阵，持续伤害阵内敌人；离开后阵地不跟随。", "更大的驻留阵地；进退之间引敌入阵。", "efb7bf", 5, HeroKind.Reimu, "红魔乡：梦符「封魔阵」；本作驻留转译", "阵"),
        new(ArtKind.Stars, "星光射击", "魔理沙 · 散射", "星弹自动朝妖群散射；慢移时收束角度，不增加弹数或伤害。", "七星齐发；散射覆盖与慢移集中自由切换。", "e6c786", 5, HeroKind.Marisa, "永夜抄说明书/求闻史纪：星尘与光热魔法", "星"),
        new(ArtKind.Stardust, "星尘幻想", "魔理沙 · 扩散", "星屑向四周扩散，贯穿少量敌人；近处覆盖更密集。", "二十三枚星屑扩散，贯穿两敌。", "bab0f0", 5, HeroKind.Marisa, "红魔乡：魔符「Stardust Reverie」；本作转译", "尘"),
        new(ArtKind.MasterSpark, "Master Spark", "魔理沙 · 魔炮", "自动锁向，蓄势后发射持续光束；方向锁定，走位可平移火线。", "更宽、更持久的魔炮；发射时仍可移动。", "a9cadb", 5, HeroKind.Marisa, "红魔乡：恋符「Master Spark」；本作自动施放", "炮"),
        new(ArtKind.Power, "威力修习", "修习 · 威力", "威力系数增加基础值的 18%；符卡同样受益。", "", "e6c786", 3),
        new(ArtKind.Haste, "施法精进", "修习 · 节奏", "施放频率系数 +0.14；魔炮只缩短休整，不加快光束伤害脉冲。", "", "8cdcc8", 3),
        new(ArtKind.Vitality, "体魄修习", "修习 · 生存", "生命上限 +25，立即恢复 35 点生命。", "", "ef9fb3", 3),
        new(ArtKind.Flow, "步法调息", "修习 · 游走", "移动增加基础值的 6%，拾取半径 +35，闪身冷却减少 0.35 秒。", "", "a9cadb", 3),
        new(ArtKind.Recovery, "调息", "调息 · 恢复", "立即恢复 40 点生命，获得 25 点符卡蓄势。", "", "8cdcc8", int.MaxValue)
    ];

    public static ArtDefinition Get(ArtKind kind) => All[(int)kind];
    public static IEnumerable<ArtDefinition> Abilities(HeroKind hero) => All.Where(art => art.Owner == hero);
    public static IEnumerable<ArtDefinition> Training => All.Where(art => art.Owner == null && art.Id != ArtKind.Recovery);
    public static bool Available(HeroKind hero, ArtKind kind) => Get(kind).Owner is not { } owner || owner == hero;
    public static string SignatureName(HeroKind hero) => hero == HeroKind.Reimu ? "灵符「梦想封印」" : "恋符「Master Spark」";

    public static string UpgradeText(ArtKind kind, int rank)
    {
        if (rank >= Get(kind).MaxRank) return "已达圆满；可转向其他能力或补强生存。";
        if (Get(kind).Owner == null) return Get(kind).Description;
        var next = AbilityTuning.Get(kind, rank + 1);
        var prefix = rank == 0 ? "习得：" : "下一重：";
        return prefix + (kind switch
        {
            ArtKind.Ofuda => $"每轮 {next.Count} 张御札，单札基础伤害 {next.Damage:0}。",
            ArtKind.YinYang => $"{next.Count} 枚阴阳玉，基础伤害 {next.Damage:0}，回旋半径 {next.Range:0}。",
            ArtKind.Boundary => $"半边长 {next.Range:0}，留阵 {next.Duration:0.0} 秒；每次基础伤害 {next.Damage:0}。",
            ArtKind.Stars => $"每轮 {next.Count} 枚星弹，单弹基础伤害 {next.Damage:0}；Shift 收束。",
            ArtKind.Stardust => $"每轮 {next.Count} 枚扩散星屑，基础伤害 {next.Damage:0}，贯穿两敌。",
            _ => $"光束宽 {AbilityTuning.BeamHalfWidth(rank + 1) * 2:0}，持续 {next.Duration:0.00} 秒；每次基础伤害 {next.Damage:0}。"
        });
    }

    public static string RankText(ArtKind kind, int rank) => kind == ArtKind.Recovery ? "即时生效" : $"第 {rank + 1} 重 / {Get(kind).MaxRank}";
}
