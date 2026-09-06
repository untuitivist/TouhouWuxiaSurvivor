namespace Rebirth.Core;

public sealed record ArtDefinition(ArtKind Id, string Name, string School, string Description, string Mastery, string Color, int MaxRank);

public static class ArtCatalog
{
    public static readonly ArtDefinition[] All =
    [
        new(ArtKind.Sword, "御剑诀", "御剑 · 破阵", "飞剑自动追敌。每重提高剑伤，二、四重增加飞剑，三重开始穿透。", "万剑归宗：五剑齐发，贯穿四敌。", "e6c786", 5),
        new(ArtKind.Orbit, "阴阳两仪", "结界 · 护身", "阴阳玉绕身旋转，持续击退近敌。每重增加玉数与伤害。", "太极无相：回旋半径扩大，近身伤害大增。", "8cdcc8", 5),
        new(ArtKind.Talisman, "灵符·散华", "符术 · 爆破", "向妖群投出灵符，命中后范围爆破。每重扩大爆破并提高伤害。", "梦想散华：三符齐出，爆破覆盖更大范围。", "ef9fb3", 5),
        new(ArtKind.Lightning, "紫电游龙", "雷法 · 连锁", "雷光在邻近敌人间跳跃。每重增加伤害和连锁目标。", "九霄雷劫：连锁九敌，施法间隔缩短。", "bab0f0", 5),
        new(ArtKind.Power, "破军心法", "心法 · 威力", "全部武学伤害 +18%。剑意爆发同样受益。", "", "e6c786", 3),
        new(ArtKind.Haste, "行云流水", "心法 · 节奏", "全部武学施放频率 +14%。", "", "8cdcc8", 3),
        new(ArtKind.Vitality, "长生真经", "心法 · 生存", "生命上限 +25，立即恢复 35 点生命。", "", "ef9fb3", 3),
        new(ArtKind.Flow, "踏雪无痕", "身法 · 游走", "移动 +6%，拾取半径 +35，闪身冷却减少 0.35 秒。", "", "a9cadb", 3),
        new(ArtKind.Recovery, "调息归元", "调息 · 恢复", "立即恢复 40 点生命，获得 25 点剑意。", "", "8cdcc8", int.MaxValue)
    ];

    public static ArtDefinition Get(ArtKind kind) => All[(int)kind];
    public static string RankText(ArtKind kind, int rank) => kind == ArtKind.Recovery ? "即时生效" : $"第 {rank + 1} 重 / {Get(kind).MaxRank}";
}
