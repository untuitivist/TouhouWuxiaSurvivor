using Godot;
using TouhouWuxiaSurvivor.Actors.Enemies;

namespace TouhouWuxiaSurvivor.Tests.Integration;

/// <summary>
/// 在正式敌人场景中验证图鉴动画接入、内容包映射、文字回退和战斗状态表现。
/// </summary>
public partial class GameplayEnemyVisualSmokeTest : Node
{
    private static readonly PackedScene EnemyScene =
        GD.Load<PackedScene>("res://src/actors/enemies/EnemyActor.tscn");

    /// <summary>
    /// 创建本体、红魔乡和未覆盖正作敌人，逐项检查运行时视觉契约后退出测试树。
    /// </summary>
    public override async void _Ready()
    {
        try
        {
            var target = new Node2D();
            AddChild(target);
            EnemyActor baseEnemy = CreateEnemy(FindEnemy("野妖精"), target);
            EnemyActor eosdEnemy = CreateEnemy(FindEnemy("湖上妖精"), target);
            EnemyDefinition unmapped = EnemyCatalog.All.First(definition =>
                definition.RequiredContentPack is not null and not "th06_eosd");
            EnemyActor fallbackEnemy = CreateEnemy(unmapped, target);

            await VerifyAnimatedBaseEnemy(baseEnemy);
            VerifyMappedContentEnemy(eosdEnemy);
            VerifyFallbackEnemy(fallbackEnemy, unmapped);
            VerifyCombatFeedback(baseEnemy);

            GD.Print("Gameplay enemy visual smoke test passed.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// 实例化真实敌人场景并在入树前注入定义，复现刷怪器采用的初始化顺序。
    /// </summary>
    private EnemyActor CreateEnemy(EnemyDefinition definition, Node2D target)
    {
        EnemyActor enemy = EnemyScene.Instantiate<EnemyActor>();
        enemy.Configure(definition, target);
        AddChild(enemy);
        return enemy;
    }

    /// <summary>
    /// 确认本体敌人启用 192×48 动画条、隐藏文字，并能换帧和水平翻转。
    /// </summary>
    private async Task VerifyAnimatedBaseEnemy(EnemyActor enemy)
    {
        var visual = enemy.GetNode<EnemyVisualController>("Visual");
        var sprite = visual.GetNode<Sprite2D>("Sprite");
        var label = visual.GetNode<Label>("FallbackLabel");
        Require(visual.UsesSprite && sprite.Visible && !label.Visible,
            "Base enemy did not replace its text label with the shared actor strip.");
        Require(sprite.Texture?.GetSize() == new Vector2(192.0f, 48.0f),
            "Base enemy actor strip has an unexpected texture size.");
        float initialFrameX = sprite.RegionRect.Position.X;
        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        Require(sprite.RegionRect.Position.X != initialFrameX,
            "Base enemy actor strip did not advance to another frame.");
        visual.SetFacing(-1.0f);
        Require(sprite.FlipH, "Base enemy sprite did not face left.");
    }

    /// <summary>
    /// 确认红魔乡敌人通过自身内容包 ID 命中图鉴映射，而不是错误复用本体同名资源。
    /// </summary>
    private static void VerifyMappedContentEnemy(EnemyActor enemy)
    {
        var visual = enemy.GetNode<EnemyVisualController>("Visual");
        Require(visual.UsesSprite && visual.GetNode<Sprite2D>("Sprite").Visible,
            "Mapped TH06 enemy did not use its internal actor strip in gameplay.");
    }

    /// <summary>
    /// 确认尚未制作图鉴素材的正作敌人继续显示中文名，保证增量内容可以逐步接入。
    /// </summary>
    private static void VerifyFallbackEnemy(EnemyActor enemy, EnemyDefinition definition)
    {
        var visual = enemy.GetNode<EnemyVisualController>("Visual");
        Label label = visual.GetNode<Label>("FallbackLabel");
        Require(!visual.UsesSprite && !visual.GetNode<Sprite2D>("Sprite").Visible &&
            label.Visible && label.Text == definition.DisplayName,
            "Unmapped official enemy did not preserve the Chinese text fallback.");
    }

    /// <summary>
    /// 确认受伤时精灵闪红，生命归零后隐藏纹理并切换到中文消散反馈。
    /// </summary>
    private static void VerifyCombatFeedback(EnemyActor enemy)
    {
        var visual = enemy.GetNode<EnemyVisualController>("Visual");
        var sprite = visual.GetNode<Sprite2D>("Sprite");
        visual.SetHurt(true);
        Require(sprite.Modulate.G < 0.5f, "Enemy sprite did not apply the hurt tint.");
        enemy.ReceiveDamage(999);
        Label label = visual.GetNode<Label>("FallbackLabel");
        Require(!sprite.Visible && label.Visible && label.Text == "消散",
            "Defeated enemy did not switch from sprite to Chinese feedback.");
    }

    /// <summary>
    /// 按唯一中文名取得敌人定义，使测试不依赖目录排序。
    /// </summary>
    private static EnemyDefinition FindEnemy(string displayName) =>
        EnemyCatalog.All.Single(definition => definition.DisplayName == displayName);

    /// <summary>
    /// 将视觉契约失败转换为包含具体原因的测试异常。
    /// </summary>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
