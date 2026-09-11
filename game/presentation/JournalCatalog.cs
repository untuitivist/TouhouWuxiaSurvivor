using Rebirth.Core;

namespace Rebirth.Presentation;

internal enum JournalCategory { Character, Ability, Training, Spell, Enemy, World }

internal sealed record JournalEntry(string Id, JournalCategory Category, string Name, string Summary, string Source, string Asset, bool Strip, string Details);

internal static class JournalCatalog
{
    public const string ArtRoot = "res://assets/internal_original/base/";
    public static string AssetPath(string asset) => asset.StartsWith("res://", StringComparison.Ordinal) ? asset : ArtRoot + asset;
    private static string cachedLanguage = "";
    private static IReadOnlyList<JournalEntry> cachedEntries = [];
    public static IReadOnlyList<JournalEntry> All
    {
        get
        {
            if (cachedLanguage == GameText.Language) return cachedEntries;
            cachedEntries = Array.AsReadOnly(Build().ToArray());
            cachedLanguage = GameText.Language;
            return cachedEntries;
        }
    }
    public static string CategoryName(JournalCategory category) => category switch
    {
        JournalCategory.Character => GameText.Get("行者"), JournalCategory.Ability => GameText.Get("术式"), JournalCategory.Training => GameText.Get("修习"),
        JournalCategory.Spell => GameText.Get("符卡"), JournalCategory.Enemy => GameText.Get("妖怪"), _ => GameText.Get("夜境")
    };

    private static IEnumerable<JournalEntry> Build()
    {
        foreach (var hero in Enum.GetValues<HeroKind>())
        {
            var preview = new RunState(hero, 42);
            var reimu = hero == HeroKind.Reimu;
            var name = reimu ? GameText.Get("博丽灵梦") : GameText.Get("雾雨魔理沙");
            var abilities = ArtCatalog.Abilities(hero).ToArray();
            var initial = string.Join("、", abilities.Where(art => preview.Ranks[(int)art.Id] > 0).Select(art => GameText.Get(art.Name)));
            yield return new("hero-" + hero, JournalCategory.Character, name,
                reimu ? GameText.Get("基础御札 · 解锁与兼修") : GameText.Get("基础星光 · 解锁与兼修"), GameText.Get("角色设定沿用现有角色目录；数值为本作改编。"),
                reimu ? "players/reimu.png" : "players/marisa.png", true,
                GameText.Format($"初始生命  {preview.MaxHealth:0}\n移动速度  {preview.MoveSpeed:0}\n基础威力  ×{preview.Power:0.00}\n初始术式  {initial}\n专属术式  {string.Join("、", abilities.Select(art => GameText.Get(art.Name)))}\n满蓄势符卡  {ArtCatalog.SignatureName(hero)}\n\n没有局外数值加成。武侠体现在走位、进退与修习，不替换角色原有能力身份。"));
            yield return new("spell-" + hero, JournalCategory.Spell, ArtCatalog.SignatureName(hero),
                reimu ? GameText.Get("满蓄势自动释放追踪灵光") : GameText.Get("满蓄势自动释放强化魔炮"), GameText.Get("原作命名沿用现有符卡目录；施放时序与战斗效果为本作改编。"),
                reimu ? "effects/reimu_aura.png" : "effects/master_spark.png", false,
                (reimu ? GameText.Get("先领悟梦想封印（修习 5 起可选），未解锁时不会自动施放。\n\n") : GameText.Get("先解锁魔炮，再领悟满蓄势魔炮（修习 5 起可选）；未解锁时只积累蓄势。\n\n")) +
                GameText.Get("擦弹与退治积累蓄势；解锁后蓄势满时自动发动，无需额外按键。发动时清除敌弹并吸取场上拾取物。\n\n") +
                (reimu ? GameText.Get("梦想封印：释放追踪灵光，寻找妖怪并造成范围爆发。") : GameText.Get("强化魔炮：蓄势后持续照射；领悟追敌后自动瞄准最近敌人，广域与消弹同样生效。")) +
                GameText.Get("\n\n图中为当前局内使用的素材，不是原作符卡逐帧复刻。"));
        }
        foreach (var art in ArtCatalog.All)
        {
            var details = GameText.Get(art.Description) + "\n\n";
            if (art.Owner.HasValue)
            {
                details += GameText.Get("各重基础效果（未乘本局威力加成）\n");
                for (var rank = 1; rank <= art.MaxRank; rank++) details += GameText.Format($"第 {rank} 重  {ArtCatalog.UpgradeText(art.Id, rank - 1)}\n");
                details += GameText.Get("\n圆满  ") + GameText.Get(art.Mastery);
                details += art.Id == ArtKind.Ofuda ? GameText.Get("\n\n初始能力：只有直射符。")
                    : art.Id == ArtKind.Stars ? GameText.Get("\n\n初始能力：星体逐颗四射，独立抽样质量；多体相互吸引并持续撕扯。") : GameText.Get("\n\n需先在修习中解锁此能力。");
                foreach (var upgrade in UpgradeCatalog.All.Where(upgrade => upgrade.Ability == art.Id && (upgrade.Kind is UpgradeKind.Behavior or UpgradeKind.Training)))
                    details += GameText.Format($"\n\n{upgrade.Name} · {UpgradeCatalog.Requirement(upgrade)}\n{upgrade.Description}");
            }
            else details += art.Id == ArtKind.Recovery ? GameText.Get("即时恢复，不累计重数；候选不足时提供调息。") : GameText.Format($"最多修习 {art.MaxRank} 重；只影响本局。");
            yield return new("art-" + art.Id, art.Owner.HasValue ? JournalCategory.Ability : JournalCategory.Training,
                art.Name, art.School, string.IsNullOrEmpty(art.Source) ? GameText.Get("本作通用修习，不对应原作符卡。") : art.Source,
                AbilityAsset(art.Id), art.Id == ArtKind.YinYang, details);
        }
        var enemyPreview = new RunState(HeroKind.Reimu, 42);
        foreach (var kind in Enum.GetValues<EnemyKind>())
        {
            var enemy = enemyPreview.SpawnEnemy(kind, System.Numerics.Vector2.Zero);
            var (name, summary, asset) = kind switch
            {
                EnemyKind.Kedama => (GameText.Get("毛玉"), GameText.Get("近身追逐 · 基础敌群"), "kedama"),
                EnemyKind.Fairy => (GameText.Get("野妖精"), GameText.Get("保持距离 · 扇形弹幕"), "wild_fairy"),
                EnemyKind.Charger => (GameText.Get("山精"), GameText.Get("预警锁向 · 直线冲锋"), "mountain_spirit"),
                EnemyKind.Elite => (GameText.Get("精英妖怪"), GameText.Get("高耐久 · 环形弹幕"), "great_youkai"),
                _ => (GameText.Get("结界残影"), GameText.Get("终局首领 · 分阶段弹幕"), "great_youkai")
            };
            yield return new("enemy-" + kind, JournalCategory.Enemy, name, summary,
                GameText.Get("敌人职责与名称为本作战斗定义；展示现有局内精灵，不指认原作角色。"), "actors/" + asset + ".png", true,
                GameText.Format($"零时刻生成样本\n生命  {enemy.MaxHealth:0.#}\n基础移动速度  {enemy.Speed:0.#}\n接触伤害  {enemy.ContactDamage:0.#}\n碰撞半径  {enemy.Radius:0.#}\n\n") +
                (kind == EnemyKind.Boss ? GameText.Format($"正常流程在 {RunState.BossArrival / 60:0} 分钟后登场，击破后获胜。与精英共用当前精灵；并非额外可选角色。") : GameText.Get("部分敌人的生成生命与速度随局内时间增长；这些是基准值，不是所有时刻的固定值。")) +
                "\n\n" + summary);
        }
        yield return new("world-shrine", JournalCategory.World, GameText.Get("博丽夜境"), GameText.Get("有限夜境 · 古印与终局"), GameText.Get("当前战场为本作场景；背景使用已接入的神社原作素材。"), "scenery/title_shrine.png", false,
            GameText.Format($"场地范围  {RunState.ArenaHalfWidth * 2:0} × {RunState.ArenaHalfHeight * 2:0}\n终局登场  {RunState.BossArrival / 60:0} 分钟\n\n在有限场地中走位、修习、净化古印，击破结界残影结束本局。旧版多群系、无限地图与作品包尚未迁回，不作为当前可玩条目展示。"));
        yield return new("world-seal", JournalCategory.World, GameText.Get("古印"), GameText.Get("靠近净化 · 离开保留进度"), GameText.Get("本作交互目标；预览使用当前局内阵纹。"), "effects/ritual_array.png", false,
            GameText.Get("靠近古印时积累净化进度，离开不会清空。完成后提供修习机会、生命恢复与符卡蓄势，并吸取拾取物。\n\n不净化也能迎战终局；遇险先退出阵地，比停在弹幕里更重要。"));
    }

    private static string AbilityAsset(ArtKind kind) => kind switch
    {
        ArtKind.Ofuda => "effects/reimu_talisman.png", ArtKind.YinYang => "actors/yin_yang_orb.png",
        ArtKind.Boundary => "effects/reimu_seal_ink.png", ArtKind.Stars => "combat/star.png",
        ArtKind.Herbs => "combat/marisa_mushroom.png", ArtKind.MasterSpark => "effects/master_spark.png",
        _ => "effects/ritual_array.png"
    };
}
