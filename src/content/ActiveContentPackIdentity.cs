namespace TouhouWuxiaSurvivor.Content;

/// <summary>
/// 冻结一局实际启用的内容包身份，不把完整目录或可变菜单状态带入战斗系统。
/// </summary>
public sealed class ActiveContentPackIdentity
{
    public string Id { get; }
    public string ContentVersion { get; }
    public string ManifestFingerprint { get; }

    /// <summary>从已验证定义提取稳定身份、版本和原始清单指纹。</summary>
    public ActiveContentPackIdentity(ContentPackDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Id = definition.Id;
        ContentVersion = definition.ContentVersion;
        ManifestFingerprint = definition.ManifestFingerprint;
    }

    /// <summary>生成注册表聚合指纹使用的无歧义单包材料。</summary>
    public string ToFingerprintMaterial() =>
        $"{Id}\u001f{ContentVersion}\u001f{ManifestFingerprint}";
}
