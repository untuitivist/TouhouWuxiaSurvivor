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
            var character = CharacterCatalog.Get(hero);
            var reimu = hero == HeroKind.Reimu;
            var name = reimu ? GameText.Get("博丽灵梦") : GameText.Get("雾雨魔理沙");
            var abilities = ArtCatalog.Abilities(hero).ToArray();
            var initial = string.Join("、", abilities.Where(art => preview.Ranks[(int)art.Id] > 0).Select(art => GameText.Get(art.Name)));
            yield return new("hero-" + hero, JournalCategory.Character, name,
                reimu ? GameText.Get("基础御札 · 解锁与兼修") : GameText.Get("基础星光 · 解锁与兼修"), GameText.Get("角色设定沿用现有角色目录；数值为本作改编。"),
                reimu ? "players/reimu.png" : "players/marisa.png", true,
                GameText.Format($"初始生命  {preview.MaxHealth:0}\n移动速度  {preview.MoveSpeed:0}\n基础威力  ×{preview.Power:0.00}\n初始术式  {initial}\n专属术式  {string.Join("、", abilities.Select(art => GameText.Get(art.Name)))}\n满蓄势符卡  {ArtCatalog.SignatureName(hero)}\n\n没有局外数值加成。武侠体现在走位、进退与修习，不替换角色原有能力身份。") +
                GameText.Format($"\n\n角色身份  {character.CharacterId}\n同一角色具有自机与首领双档案；选为自机后，本局首领池排除该角色。"));
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
                foreach (var upgrade in UpgradeCatalog.All.Where(upgrade => upgrade.Ability == art.Id && (upgrade.Kind is UpgradeKind.Behavior or UpgradeKind.Training or UpgradeKind.Stance)))
                    details += GameText.Format($"\n\n{upgrade.Name} · {UpgradeCatalog.Requirement(upgrade)}\n{upgrade.Description}");
            }
            else details += art.Id == ArtKind.Recovery ? GameText.Get("即时恢复，不累计重数；候选不足时提供调息。") : GameText.Format($"最多修习 {art.MaxRank} 重；只影响本局。");
            yield return new("art-" + art.Id, art.Owner.HasValue ? JournalCategory.Ability : JournalCategory.Training,
                art.Name, art.School, string.IsNullOrEmpty(art.Source) ? GameText.Get("本作通用修习，不对应原作符卡。") : art.Source,
                AbilityAsset(art.Id), art.Id == ArtKind.YinYang, details);
        }
        var enemyPreview = new RunState(HeroKind.Reimu, 42);
        foreach (var kind in Enum.GetValues<EnemyKind>().Where(kind => kind != EnemyKind.Boss))
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
                GameText.Get("阶段生命倍率依次为 1、1.6、2.4、3.5；续战每完成一轮再增加基准的 30%。速度不随阶段暗增。") +
                GameText.Format($"\n非首领战每 {RunPacing.WaveSeconds:0} 秒有一次休整窗口：减少刷怪，并在附近生成一份 {RunPacing.RecoveryAmount} 点恢复拾取物；需要走位拾取，不自动回血。") +
                "\n\n" + summary);
        }
        foreach (var character in CharacterCatalog.All)
        {
            var profile = character.Boss;
            var reimu = character.Id == HeroKind.Reimu;
            yield return new("boss-" + character.CharacterId, JournalCategory.Enemy, character.Name, GameText.Get("角色首领 · 三阶段切磋"),
                GameText.Get("人物能力沿用角色身份；首领前摇、弹速与伤害为本作改编。"),
                reimu ? "players/reimu.png" : "players/marisa.png", true,
                GameText.Format($"首轮生命  {profile.Health:0}\n移动速度  {profile.Speed:0}\n质量  {profile.Mass:0}\n接触伤害  {profile.ContactDamage:0}\n登场时间  {RunState.BossArrival / 60:0} 分钟\n\n本局自机不会成为对手；同一角色的首领状态与玩家构筑独立。") +
                GameText.Get(reimu ? "\n\n御札 → 预警封魔阵 → 阴阳玉。血量低于 66% 与 33% 时切换阶段；封魔阵可闪身脱离，阴阳玉每 0.55 秒至多消除三发玩家弹。"
                    : "\n\n四方散星 → 追敌魔炮 → 广域魔炮。血量低于 66% 与 33% 时切换阶段；魔炮前摇 1.15 秒，转向每秒 36 度。药菇调合每场至多两次，每次回复 4% 生命。") +
                GameText.Get("\n\n击破保留标准通关记录。自愿续战每五分钟再次切磋，首领生命每轮增加基准的 60%；星体引力按质量作用，不设首领免疫。"));
        }
        yield return new("world-shrine", JournalCategory.World, GameText.Get("博丽夜境"), GameText.Get("有限夜境 · 古印与终局"), GameText.Get("当前战场为本作场景；背景使用已接入的神社原作素材。"), "scenery/title_shrine.png", false,
            GameText.Format($"场地范围  {RunState.ArenaHalfWidth * 2:0} × {RunState.ArenaHalfHeight * 2:0}\n首领登场  {RunState.BossArrival / 60:0} 分钟\n\n在有限场地中走位、修习、净化古印，击破另一位角色首领完成标准局。可结算，也可保留构筑继续挑战；续战失败不抹去标准通关。古印不重置。旧版多群系与作品包尚未迁回。"));
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
