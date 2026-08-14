namespace TouhouWuxiaSurvivor.Versioning;

/// <summary>
/// 列出版本号允许使用的开发阶段；阶段是版本首段，不再伪装成 SemVer 的尾部预发布标记。
/// </summary>
public enum GameReleaseStage
{
    Alpha,
    Beta,
    ReleaseCandidate,
    Stable,
}
