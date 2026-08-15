using Godot;
using TouhouWuxiaSurvivor.World.Biomes;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Runtime;
using TouhouWuxiaSurvivor.Gameplay.SpellCards.Definitions;

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
        int spellCapacity = SpellCardSlotPolicy.MaximumOffensiveSlots +
            SpellCardSlotPolicy.MaximumSupportSlots;
        return $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}" +
            $"  击破{snapshot.DefeatedEnemies}" +
            $"  敌人{snapshot.AliveEnemies}" +
            $"  强化{snapshot.ActiveBuffs}" +
            $"  奥义{snapshot.SpellCards.UnlockedCards.Count}/{spellCapacity}";
    }

    /// <summary>
    /// 生成仅在调试层打开时显示的世界坐标、生成状态、内容包和性能信息。
    /// </summary>
    public static string FormatDebug(WorldHudSnapshot snapshot) =>
        $"FPS  {Engine.GetFramesPerSecond()} · " +
        $"Limit {(Engine.MaxFps <= 0 ? "Off" : Engine.MaxFps)} · " +
        $"VSync {(DisplayServer.WindowGetVsyncMode() == DisplayServer.VSyncMode.Disabled ? "Off" : "On")}\n" +
        $"Seed  {snapshot.Seed}\n" +
        $"Tile  {snapshot.TileX}, {snapshot.TileY}\n" +
        $"Chunk  {snapshot.Chunk}\n" +
        $"Biome  {BiomeNames.GetChinese(snapshot.Biome)}\n" +
        $"Streaming  {snapshot.ActiveChunks} active / {snapshot.PendingChunks} pending\n" +
        $"Content  {snapshot.ActiveContent}\n" +
        $"Pacing  {snapshot.Pacing.PhaseName} · {snapshot.Pacing.TotalProgress:P0} · " +
        $"压制 {snapshot.Pacing.DominanceProgress:P0}\n" +
        $"Spell  {FormatSpellDetail(snapshot.SpellCards)}";

    /// <summary>
    /// 把已悟奥义与最近独立倒计时格式化为属性提示与 F3 共用的紧凑诊断文字。
    /// </summary>
    public static string FormatSpellDetail(SpellCardRuntimeSnapshot snapshot)
    {
        string cards = snapshot.HasUnlockedCard
            ? string.Join("、", snapshot.UnlockedCards.Select(card => card.ShortName))
            : "尚未悟得";
        string state = snapshot.NextCardIsWaitingForCondition
            ? "周天已就绪，等待被动条件"
            : $"{snapshot.NextCastRemaining:0.0}秒";
        return $"{cards} · 下一式 {snapshot.NextCardName} · {state}";
    }
}
