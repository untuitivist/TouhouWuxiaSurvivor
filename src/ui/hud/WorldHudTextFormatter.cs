using Godot;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;

namespace TouhouWuxiaSurvivor.Ui.Hud;

/// <summary>
/// 把 HUD 快照分别格式化为紧凑战斗状态栏和 Minecraft 式详细调试文字。
/// </summary>
public static class WorldHudTextFormatter
{
    /// <summary>
    /// 生成一行常驻状态，只保留战斗过程中需要频繁观察的时间、击破、敌人和强化。
    /// </summary>
    public static string FormatStatus(WorldHudSnapshot snapshot)
    {
        TimeSpan elapsed = TimeSpan.FromSeconds(snapshot.ElapsedSeconds);
        string spellMode = snapshot.SpellCards.HasUnlockedCard ? "自动" : "未悟";
        return $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}" +
            $"  击破{snapshot.DefeatedEnemies}" +
            $"  敌人{snapshot.AliveEnemies}" +
            $"  强化{snapshot.ActiveBuffs}" +
            $"  奥义{spellMode}{snapshot.SpellCards.CurrentPower}/{snapshot.SpellCards.MaximumPower}";
    }

    /// <summary>
    /// 生成仅在调试层打开时显示的世界坐标、生成状态、内容包和性能信息。
    /// </summary>
    public static string FormatDebug(WorldHudSnapshot snapshot) =>
        $"FPS  {Engine.GetFramesPerSecond()}\n" +
        $"Seed  {snapshot.Seed}\n" +
        $"Tile  {snapshot.TileX}, {snapshot.TileY}\n" +
        $"Chunk  {snapshot.Chunk}\n" +
        $"Biome  {BiomeNames.GetChinese(snapshot.Biome)}\n" +
        $"Streaming  {snapshot.ActiveChunks} active / {snapshot.PendingChunks} pending\n" +
        $"Content  {snapshot.ActiveContent}\n" +
        $"Spell  {FormatSpellDetail(snapshot.SpellCards)}";

    /// <summary>
    /// 把已悟符卡、共享灵力和公共冷却格式化为属性提示与 F3 共用的紧凑诊断文字。
    /// </summary>
    public static string FormatSpellDetail(SpellCardRuntimeSnapshot snapshot)
    {
        string cards = snapshot.HasUnlockedCard
            ? string.Join("、", snapshot.UnlockedCards.Select(card => card.ShortName))
            : "尚未悟得";
        return $"{cards} · 灵力 {snapshot.CurrentPower}/{snapshot.MaximumPower}" +
            $" · 冷却 {snapshot.CooldownRemaining:0.0}秒";
    }
}
