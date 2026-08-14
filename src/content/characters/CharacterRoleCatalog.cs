namespace TouhouWuxiaSurvivor.Content.Characters;

/// <summary>
/// 以角色规范中文名登记战斗定位，确保每个角色的数值来源可以审查且不会随清单顺序漂移。
/// </summary>
public static class CharacterRoleCatalog
{
    private static readonly IReadOnlyDictionary<string, CharacterCombatRole> Roles = BuildRoles();

    public static IReadOnlyCollection<string> RegisteredNames => Roles.Keys.ToArray();

    /// <summary>
    /// 返回角色明确登记的战斗定位；遗漏新角色时直接失败，强制内容开发者完成数值策划。
    /// </summary>
    public static CharacterCombatRole GetRequired(string displayName) =>
        Roles.TryGetValue(displayName, out CharacterCombatRole role)
            ? role
            : throw new KeyNotFoundException($"Character combat role is not registered: {displayName}");

    /// <summary>
    /// 按六类横向玩法建立完整名单，并在重复登记时抛错以避免同一角色获得不确定定位。
    /// </summary>
    private static IReadOnlyDictionary<string, CharacterCombatRole> BuildRoles()
    {
        var roles = new Dictionary<string, CharacterCombatRole>(StringComparer.Ordinal);
        Add(roles, CharacterCombatRole.Balanced,
            "博丽灵梦", "神玉", "明罗", "爱莲", "小兔姬", "奥莲姬", "露易兹",
            "露米娅", "蕾蒂·霍瓦特洛克", "上白泽慧音", "秋静叶", "东风谷早苗",
            "古明地觉", "寅丸星", "物部布都", "清兰", "高丽野阿吽", "庭渡久侘歌",
            "豪德寺三花", "孙美天", "尘塚姥芽");
        Add(roles, CharacterCombatRole.Power,
            "萨丽爱尔", "魅魔", "冈崎梦美", "幽香", "幻月", "梦子", "神绮",
            "帕秋莉·诺蕾姬", "芙兰朵露·斯卡蕾特", "西行寺幽幽子", "藤原妹红",
            "四季映姬·夜摩仙那度", "八坂神奈子", "星熊勇仪", "灵乌路空", "圣白莲",
            "丰聪耳神子", "堀川雷鼓", "纯狐", "赫卡提亚·拉碧斯拉祖利", "摩多罗隐岐奈",
            "埴安神袿姬", "姬虫百百世", "日白残无", "磐永阿梨夜");
        Add(roles, CharacterCombatRole.Rapid,
            "幽幻魔眼", "里香", "北白河千百合", "胡桃", "雪", "舞", "琪露诺",
            "普莉兹姆利巴三姐妹", "米斯蒂娅·萝蕾拉", "铃仙·优昙华院·因幡",
            "梅蒂欣·梅兰可莉", "河城荷取", "黑谷山女", "火焰猫燐", "幽谷响子",
            "苏我屠自古", "九十九弁弁", "九十九八桥", "铃瑚", "克劳恩皮丝",
            "爱塔妮缇拉尔瓦", "尔子田里乃", "丁礼田舞", "戎璎花", "杖刀偶磨弓",
            "山城高岭", "饭纲丸龙", "天火人血枪", "封兽魑魅");
        Add(roles, CharacterCombatRole.Swift,
            "伊莉斯", "矜羯罗", "雾雨魔理沙", "卡娜·安娜贝拉尔", "艾丽", "梦月",
            "十六夜咲夜", "魂魄妖梦", "八云蓝", "因幡帝",
            "小野塚小町", "琪斯美", "古明地恋", "娜兹玲", "多多良小伞", "村纱水蜜",
            "封兽鵺", "霍青娥", "二岩猯藏", "若鹭姬", "赤蛮奇", "今泉影狼",
            "鬼人正邪", "少名针妙丸", "稀神探女", "坂田合欢乃", "骊驹早鬼",
            "驹草山如", "豫母都日狭美", "道神驯子", "渡里贝子");
        Add(roles, CharacterCombatRole.Formation,
            "菊理", "朝仓理香子", "八云紫",
            "莉格露·奈特巴格", "八意永琳", "蓬莱山辉夜", "键山雏", "水桥帕露西",
            "哆来咪·苏伊特", "矢田寺成美", "吉吊八千慧", "玉造魅须丸", "天弓千亦",
            "三头慧之子", "维缦·浅间", "绵月丰姬");
        Add(roles, CharacterCombatRole.Guardian,
            "红美铃", "爱丽丝", "蕾米莉亚·斯卡蕾特", "洩矢诹访子", "宫古芳香",
            "云居一轮与云山", "牛崎润美", "秋穰子", "萨拉", "橙");
        return roles;
    }

    /// <summary>
    /// 将一组角色加入指定定位，并拒绝重复名字，使策划表错误在目录初始化时立即暴露。
    /// </summary>
    private static void Add(
        IDictionary<string, CharacterCombatRole> roles,
        CharacterCombatRole role,
        params string[] names)
    {
        foreach (string name in names)
        {
            if (!roles.TryAdd(name, role))
            {
                throw new InvalidOperationException($"Character combat role is duplicated: {name}");
            }
        }
    }
}
