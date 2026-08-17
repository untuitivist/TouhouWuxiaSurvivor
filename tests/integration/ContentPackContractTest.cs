using Godot;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证内容清单身份、横向依赖、能力声明和本局冻结指纹都来自同一严格目录。
/// </summary>
public partial class ContentPackContractTest : Node
{
    /// <summary>执行完整身份契约；任一清单漂移都用非零退出码阻断运行与发布。</summary>
    public override void _Ready()
    {
        try
        {
            VerifyInstalledCatalog();
            VerifyFrozenRunContext();
            GD.Print("Content pack contract test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认唯一 Base、二十个可选包及 Boss 能力不再由作品编号推断。</summary>
    private static void VerifyInstalledCatalog()
    {
        ContentPackDefinition foundation = ContentPackCatalog.Base;
        Require(ContentPackCatalog.Installed.Count == 21 &&
            ContentPackCatalog.All.Count == 20,
            "Installed catalog does not contain Base plus TH01-TH20.");
        Require(foundation.SchemaVersion == 1 && foundation.ContentVersion == "0.1.0" &&
            foundation.HostApi == "content-v1" &&
            foundation.Kind == ContentPackKind.Foundation &&
            foundation.RequiredDependencies.Count == 0,
            "Base identity header is incomplete or not the foundation.");
        Require(IsFingerprint(foundation.ManifestFingerprint),
            "Base manifest fingerprint is not a lowercase SHA-256 value.");

        foreach (ContentPackDefinition optional in ContentPackCatalog.All)
        {
            Require(optional.Kind == ContentPackKind.Optional &&
                optional.RequiredDependencies.SequenceEqual([foundation.Id]) &&
                IsFingerprint(optional.ManifestFingerprint),
                $"Optional identity or Base dependency is invalid: {optional.Id}");
        }

        string[] capable = ContentPackCatalog.Installed
            .Where(pack => pack.HasCapability(ContentPackCapabilityIds.BossSpellSequences))
            .Select(pack => pack.Id)
            .ToArray();
        Require(capable.SequenceEqual(["base", "th06_eosd"]),
            "Boss spell capability is no longer limited to the accepted Base/TH06 experiment.");
    }

    /// <summary>确认同一输入得到同一指纹，改变种子或活动包则只改变对应本局身份。</summary>
    private static void VerifyFrozenRunContext()
    {
        var character = new CharacterSelection(CharacterCatalog.Default);
        var first = new RunContentContext(ContentPackSelection.BaseOnly, character, 12345);
        var repeated = new RunContentContext(ContentPackSelection.BaseOnly, character, 12345);
        var otherSeed = new RunContentContext(ContentPackSelection.BaseOnly, character, 54321);
        var eosd = new RunContentContext(
            new ContentPackSelection(["th06_eosd"]), character, 12345);
        Require(first.ActiveContentPacks.Select(pack => pack.Id).SequenceEqual(["base"]) &&
            eosd.ActiveContentPacks.Select(pack => pack.Id)
                .SequenceEqual(["base", "th06_eosd"]),
            "Active pack snapshot does not match the selected horizontal content.");
        Require(first.RegistryFingerprint == repeated.RegistryFingerprint &&
            first.RunFingerprint == repeated.RunFingerprint &&
            first.RegistryFingerprint == otherSeed.RegistryFingerprint &&
            first.RunFingerprint != otherSeed.RunFingerprint &&
            first.RegistryFingerprint != eosd.RegistryFingerprint,
            "Run or registry fingerprint is not deterministic and input-sensitive.");

        bool rejected = false;
        try
        {
            _ = new RunContentContext(
                new ContentPackSelection(["missing_pack"]), character, 12345);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Require(rejected, "Unknown selected content was silently accepted into a run.");
    }

    /// <summary>识别严格小写十六进制 SHA-256，避免只检查非空字符串的假覆盖。</summary>
    private static bool IsFingerprint(string value) =>
        value.Length == 64 && value.All(character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    /// <summary>把任一内容身份契约失败转换为带原因的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
