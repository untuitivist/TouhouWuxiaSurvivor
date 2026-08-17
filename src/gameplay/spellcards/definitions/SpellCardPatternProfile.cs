namespace TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

/// <summary>
/// 保存一张符卡经原作资料校对后的演出依据与归一化时序；数值按本局角色基础属性另行解析。
/// </summary>
public sealed class SpellCardPatternProfile
{
    public SpellCardPatternKind Kind { get; }
    public string OriginalReference { get; }
    public string ReferenceUrl { get; }
    public string OriginalBehavior { get; }
    public IReadOnlyList<SpellBulletStyleKind> AccentBulletStyles { get; }
    public int WaveCount { get; }
    public float PhaseRatio { get; }
    public float HoldRatio { get; }
    public float TurnRateScale { get; }
    public bool IsOriginalReferenceVerified => Kind != SpellCardPatternKind.LegacyGeometry;

    /// <summary>
    /// 建立可复用演出档案；时序使用总飞行时间或周天间隔的比例，禁止内容清单写入独立伤害常量。
    /// </summary>
    public SpellCardPatternProfile(
        SpellCardPatternKind kind,
        string originalReference,
        string originalBehavior,
        string referenceUrl = "",
        IReadOnlyList<SpellBulletStyleKind>? accentBulletStyles = null,
        int waveCount = 1,
        float phaseRatio = 0.0f,
        float holdRatio = 0.0f,
        float turnRateScale = 0.0f)
    {
        Kind = kind;
        OriginalReference = Require(originalReference, nameof(originalReference));
        OriginalBehavior = Require(originalBehavior, nameof(originalBehavior));
        ReferenceUrl = ValidateReferenceUrl(kind, referenceUrl);
        AccentBulletStyles = (accentBulletStyles ?? []).Distinct().ToArray();
        WaveCount = Math.Clamp(waveCount, 1, 8);
        PhaseRatio = NormalizeRatio(phaseRatio, nameof(phaseRatio));
        HoldRatio = NormalizeRatio(holdRatio, nameof(holdRatio));
        TurnRateScale = float.IsFinite(turnRateScale)
            ? Math.Clamp(turnRateScale, -4.0f, 4.0f)
            : throw new ArgumentOutOfRangeException(nameof(turnRateScale));
        ValidateTiming(Kind, PhaseRatio, HoldRatio, TurnRateScale);
    }

    /// <summary>为尚未逐卡校对的迁移库存建立明确回退，避免图鉴把旧几何冒充原作弹幕。</summary>
    public static SpellCardPatternProfile CreateLegacy(
        string sourceNote,
        string effectDescription) => new(
            SpellCardPatternKind.LegacyGeometry,
            sourceNote,
            $"尚未完成原作逐卡演出校对；当前沿用通用几何。{effectDescription}",
            string.Empty);

    /// <summary>按弹丸序号在主弹型与辅弹型之间稳定轮换，供玩家、Boss 与图鉴共用。</summary>
    public SpellBulletStyleKind ResolveStyle(SpellBulletStyleKind primary, int index)
    {
        if (AccentBulletStyles.Count == 0) return primary;
        int styleIndex = (int)(Math.Abs((long)index) % (AccentBulletStyles.Count + 1));
        return styleIndex == 0 ? primary : AccentBulletStyles[styleIndex - 1];
    }

    /// <summary>按演出类型验证必需阶段，阻止“声明冻结但没有停顿”一类静默退化进入战斗。</summary>
    private static void ValidateTiming(
        SpellCardPatternKind kind,
        float phaseRatio,
        float holdRatio,
        float turnRateScale)
    {
        if ((kind is SpellCardPatternKind.FreezeRelease or
                SpellCardPatternKind.TimeStopRedirect) &&
            (phaseRatio <= 0.0f || holdRatio <= 0.0f))
            throw new ArgumentException("A stop-and-release pattern requires two positive phases.");
        if ((kind is SpellCardPatternKind.RotatingStream or
                SpellCardPatternKind.SweepingBeam) && Math.Abs(turnRateScale) <= 0.0001f)
            throw new ArgumentException("A rotating pattern requires a non-zero turn rate.");
    }

    /// <summary>校验必需资料文本，阻止带空 Wiki 编号或空演出说明的内容进入运行目录。</summary>
    private static string Require(string value, string parameterName) =>
        !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException("Spell pattern text cannot be empty.", parameterName);

    /// <summary>把清单时序限制在单次生命周期内，并拒绝 NaN 或无穷值污染物理循环。</summary>
    private static float NormalizeRatio(float value, string parameterName) =>
        float.IsFinite(value)
            ? Math.Clamp(value, 0.0f, 1.0f)
            : throw new ArgumentOutOfRangeException(parameterName);

    /// <summary>已校对演出必须保留 HTTPS 资料入口；旧库存允许空链接但不得伪造已验证状态。</summary>
    private static string ValidateReferenceUrl(SpellCardPatternKind kind, string value)
    {
        if (kind == SpellCardPatternKind.LegacyGeometry && string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri.Scheme == "https"
            ? uri.AbsoluteUri
            : throw new ArgumentException("Verified spell patterns require an HTTPS reference URL.",
                nameof(value));
    }
}
