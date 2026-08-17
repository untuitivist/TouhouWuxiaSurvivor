using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;
using TouhouWuxiaSurvivor.Content;
using TouhouWuxiaSurvivor.Content.Characters;
using TouhouWuxiaSurvivor.Ecs.Combat;
using TouhouWuxiaSurvivor.Ecs.Combat.Projectiles;
using TouhouWuxiaSurvivor.Gameplay.Encounters;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Effects;
using TouhouWuxiaSurvivor.Visuals.Internal;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 验证弹丸、角色和 Boss 视觉严格归属本局内容包，并只通过显式同语义代理补齐缺失素材。
/// </summary>
public partial class ProjectileContentVisualTest : Node
{
    /// <summary>执行来源编号、逐包解析、ECS 传播和跨作品角色来源四组契约。</summary>
    public override void _Ready()
    {
        try
        {
            VerifySourceBindings();
            VerifyPackFirstResolution();
            VerifyExactSpellResolution();
            VerifyEcsSourcePropagation();
            VerifySharedCharacterSource();
            GD.Print("Projectile content visual test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>确认每个已安装内容包获得唯一非零编号，零值绝不被解释成本体。</summary>
    private static void VerifySourceBindings()
    {
        var ids = new HashSet<int>();
        foreach (ContentPackDefinition pack in ContentPackCatalog.Installed)
        {
            int binding = ProjectileVisualSourceBindingCatalog.GetBindingId(pack.Id);
            Require(binding > 0 && ids.Add(binding),
                $"Projectile source binding is invalid or duplicated: {pack.Id}/{binding}");
            Require(ProjectileVisualSourceBindingCatalog.TryGetSourcePackId(
                    binding, out string sourceId) && sourceId == pack.Id,
                $"Projectile source binding cannot round-trip: {pack.Id}");
        }

        Require(!ProjectileVisualSourceBindingCatalog.TryGetSourcePackId(0, out _) &&
            !ProjectileVisualSourceBindingCatalog.TryGetSourcePackId(int.MaxValue, out _),
            "Unknown projectile source silently fell back to another content pack.");
    }

    /// <summary>逐包逐语义确认原生图集优先；整包无原生图集时只能采用明确登记的代理。</summary>
    private static void VerifyPackFirstResolution()
    {
        var visuals = new InternalVisualCatalog();
        var resolver = new SpellCardProjectileVisualResolver();
        resolver.Configure(visuals);
        foreach (ContentPackDefinition pack in ContentPackCatalog.Installed)
        {
            InternalVisualDefinition[] definitions = GetSpellVisuals(visuals, pack.Id);
            Require(definitions.Length > 0,
                $"Content pack has no projectile visual declaration: {pack.Id}");
            bool hasNativeAtlas = definitions.Any(item => item.ProxySourceWork is null);
            int binding = ProjectileVisualSourceBindingCatalog.GetBindingId(pack.Id);
            foreach (SpellBulletStyleKind style in Enum.GetValues<SpellBulletStyleKind>())
            {
                Require(resolver.TryResolveSource(binding, style, 3,
                        out Texture2D texture, out SpellBulletVisualSelection selection,
                        out InternalVisualDefinition selected),
                    $"Content pack cannot resolve projectile semantics: {pack.Id}/{style}");
                Require(selected.SourceId == pack.Id && selection.Style == style &&
                    HasVisiblePixels(texture.GetImage(), selection.Source),
                    $"Content projectile resolved an alien or empty region: {pack.Id}/{style}");
                Require(hasNativeAtlas
                        ? selected.ProxySourceWork is null
                        : selected.ProxySourceWork is not null,
                    $"Content projectile violated native-first proxy rules: {pack.Id}/{style}");
            }
        }

        Require(!resolver.TryResolveSource(0, SpellBulletStyleKind.Orb, 0,
                out _, out _, out _),
            "A source-less projectile silently borrowed the Base atlas.");
    }

    /// <summary>逐张奥义确认消费方身份不变，且同包已有原生图集时不会继续使用跨作代理。</summary>
    private static void VerifyExactSpellResolution()
    {
        var visuals = new InternalVisualCatalog();
        var resolver = new SpellCardProjectileVisualResolver();
        resolver.Configure(visuals);
        foreach (SpellCardDefinition card in SpellCardCatalog.All)
        {
            int binding = SpellCardVisualBindingCatalog.GetBindingId(card.Id);
            Require(resolver.TryResolve(binding, 5, out Texture2D texture,
                    out SpellBulletVisualSelection selection,
                    out InternalVisualDefinition selected),
                $"Spell projectile visual is unavailable: {card.Id}");
            bool packHasNative = GetSpellVisuals(visuals, card.SourcePackId)
                .Any(item => item.ProxySourceWork is null);
            Require(selected.SourceId == card.SourcePackId &&
                    (!packHasNative || selected.ProxySourceWork is null) &&
                    HasVisiblePixels(texture.GetImage(), selection.Source),
                $"Spell projectile leaked an avoidable cross-pack proxy: {card.Id}");
        }
    }

    /// <summary>确认玩家池与普通敌人发射请求都携带来源编号，高频 ECS 不依赖渲染器猜包。</summary>
    private static void VerifyEcsSourcePropagation()
    {
        int scarletSource = ProjectileVisualSourceBindingCatalog.GetBindingId(
            ContentPackIds.EmbodimentOfScarletDevil);
        var projectiles = new ProjectilePool();
        Require(projectiles.TryAdd(Vector2.Zero, Vector2.Right, 360.0f, 10,
                ProjectileFaction.Player, 2.0f, 4.0f, 0, out _,
                visualSourceId: scarletSource) &&
            projectiles.Get(0).VisualSourceId == scarletSource,
            "Player projectile pool discarded its content source.");

        EnemyDefinition shooter = EnemyCatalog.All.First(enemy =>
            enemy.RequiredContentPack == ContentPackIds.EmbodimentOfScarletDevil &&
            enemy.ProjectileProfile.Enabled);
        var enemies = new EnemyPool();
        enemies.Add(new Vector2(64.0f, 0.0f), shooter);
        EnemyComponent component = enemies.Get(0);
        component.FireCooldown = 0.0f;
        enemies.Set(0, component);
        var requests = new List<EnemyProjectileSpawnRequest>();
        new EnemyProjectileSystem().Step(enemies, Vector2.Zero, 0.1f, request =>
        {
            requests.Add(request);
            return true;
        });
        Require(requests.Count > 0 && requests.All(request =>
                request.VisualSourceId == scarletSource),
            "Enemy projectile request discarded or replaced its content source.");
    }

    /// <summary>共享角色只采用本局实际启用来源，Boss 定义不得回看未启用的规范首作素材。</summary>
    private static void VerifySharedCharacterSource()
    {
        CharacterDefinition mima = CharacterCatalog.GetRequiredByDisplayName("魅魔");
        var selection = new ContentPackSelection(
            [ContentPackIds.StoryOfEasternWonderland]);
        var context = new RunContentContext(selection, new CharacterSelection(mima));
        string source = CharacterContentSourceResolver.Resolve(mima, context);
        Require(source == ContentPackIds.StoryOfEasternWonderland,
            "Shared character selected visual material from a disabled content pack.");
        Require(BossDefinitionFactory.Create(mima, source).RequiredContentPack == source,
            "Boss definition did not preserve the resolved active character source.");
        Require(CharacterContentSourceResolver.Resolve(
                CharacterCatalog.Default,
                new RunContentContext(ContentPackSelection.BaseOnly,
                    new CharacterSelection(CharacterCatalog.Default))) == ContentPackCatalog.Base.Id,
            "Base character did not retain Base visual ownership.");
    }

    /// <summary>取得指定内容包全部奥义映射，缺失映射留给调用方输出包级错误。</summary>
    private static InternalVisualDefinition[] GetSpellVisuals(
        InternalVisualCatalog visuals,
        string sourceId) => SpellCardCatalog.All
        .Where(card => card.SourcePackId == sourceId)
        .Select(card => visuals.TryGet(sourceId, InternalVisualCategory.SpellCard,
            card.FullName, out InternalVisualDefinition definition) ? definition : null)
        .Where(definition => definition is not null)
        .Cast<InternalVisualDefinition>()
        .ToArray();

    /// <summary>扫描最终切片的 Alpha，拒绝把透明留白当成可用的同语义素材。</summary>
    private static bool HasVisiblePixels(Image image, Rect2 region)
    {
        for (int y = (int)region.Position.Y; y < (int)region.End.Y; y++)
        for (int x = (int)region.Position.X; x < (int)region.End.X; x++)
            if (image.GetPixel(x, y).A > 0.01f) return true;
        return false;
    }

    /// <summary>把来源契约失败转换为携带内容包身份的测试异常。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
