namespace TouhouWuxiaSurvivor.Tools.UiAssetGenerator;

/// <summary>
/// 区分 HUD 条的玩法语义，使生命、经验、节奏和亲和不会只靠文字辨认。
/// </summary>
internal enum UiProgressKind
{
    Health,
    Experience,
    Pacing,
    Affinity,
}
