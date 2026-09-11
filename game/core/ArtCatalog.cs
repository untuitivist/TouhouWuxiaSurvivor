namespace Rebirth.Core;

public sealed record ArtDefinition(ArtKind Id, string Name, string School, string Description, string Mastery, string Color, int MaxRank, HeroKind? Owner = null, string Source = "", string Symbol = "");

public static class ArtCatalog
{
    public static readonly ArtDefinition[] All =
    [
        new(ArtKind.Ofuda, "御札", "灵梦 · 符", "向目标发射直线御札；追踪与爆炸需分别领悟，可兼修。", "每轮六张御札；行为效果由已选分支决定。", "ef9fb3", 5, HeroKind.Reimu, "红魔乡/永夜抄说明书：御札；本作成长转译", "札"),
        new(ArtKind.YinYang, "阴阳玉", "灵梦 · 护身", "阴阳玉绕身，击退近敌；走位决定接触范围。", "六枚阴阳玉，回旋半径扩大至 115。", "8cdcc8", 5, HeroKind.Reimu, "红魔乡说明书：阴阳玉；本作回旋转译", "玉"),
        new(ArtKind.Boundary, "封魔阵", "灵梦 · 留阵", "在脚下留下方形封魔阵，持续伤害阵内敌人；离开后阵地不跟随。", "更大的驻留阵地；进退之间引敌入阵。", "efb7bf", 5, HeroKind.Reimu, "红魔乡：梦符「封魔阵」；本作驻留转译", "阵"),
        new(ArtKind.Stars, "引力星流", "魔理沙 · 持续", "不断向四面八方逐颗散出星体；星与星、星与怪按质量相互吸引，近身持续撕扯。", "提升单位质量的撕扯强度；质量分布与存活时间可反复修习，不保底生成行星。", "e6c786", 5, HeroKind.Marisa, "本作玩法改编：星光魔法与引力意象，不冒充原作符卡", "星"),
        new(ArtKind.Herbs, "药菇调合", "魔理沙 · 调息", "定期在身边留下药菇，拾取回血；满血时保留，有存续时间与数量上限。", "每份恢复 14 点生命；留药与缓释需分别领悟。", "8cdcc8", 5, HeroKind.Marisa, "本作玩法改编：蘑菇与草药调合，不改写原作职业", "药"),
        new(ArtKind.MasterSpark, "Master Spark", "魔理沙 · 魔炮", "蓄势后持续照射，默认锁向；追敌、广域与星光共鸣需分别领悟。", "更持久的魔炮；领悟追敌后自动转向最近敌人，让火线内星群共鸣。", "a9cadb", 5, HeroKind.Marisa, "红魔乡：恋符「Master Spark」；本作持续施放转译", "炮"),
        new(ArtKind.Power, "威力修习", "修习 · 威力", "威力系数增加基础值的 18%；符卡同样受益。", "", "e6c786", 3),
        new(ArtKind.Haste, "施法精进", "修习 · 节奏", "施放频率系数 +0.14；不加快星群或魔炮伤害脉冲，也不加快药菇生成。", "", "8cdcc8", 3),
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
            ArtKind.Stars => $"每单位质量基础撕扯 {next.Damage:0.00}/秒；质量分布与长明可反复修习。",
            ArtKind.Herbs => $"每 {next.Interval:0} 秒生成一份药菇，拾取恢复 {next.Damage:0} 点生命。",
            _ => $"光束宽 {AbilityTuning.BeamHalfWidth(rank + 1) * 2:0}，持续 {next.Duration:0.00} 秒；每次基础伤害 {next.Damage:0}。"
        });
    }

    public static string RankText(ArtKind kind, int rank) => kind == ArtKind.Recovery ? "即时生效" : $"第 {rank + 1} 重 / {Get(kind).MaxRank}";
}
