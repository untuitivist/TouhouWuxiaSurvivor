using Godot;

namespace TouhouWuxiaSurvivor.Tests.Support;

/// <summary>
/// 为实例化真实 WorldDemo 的集成测试提供统一退出流程，隔离立即退出时的音频后端回收时序。
/// </summary>
public static class WorldDemoTestCleanup
{
    private const double AudioDrainSeconds = 0.25;

    /// <summary>
    /// 先停止世界和播放器，等待混音线程确认停播，再清空流、释放场景并等待资源回收。
    /// </summary>
    /// <param name="runner">仍位于场景树中的测试节点，用于等待帧信号。</param>
    /// <param name="world">测试创建且即将释放的真实游戏世界实例。</param>
    public static async Task FreeAsync(Node runner, Node world)
    {
        SceneTree tree = runner.GetTree();
        tree.Paused = false;
        world.ProcessMode = Node.ProcessModeEnum.Disabled;
        AudioStreamPlayer[] players = FindAudioPlayers(world);
        StopAudio(players);
        if (players.Length > 0)
        {
            await WaitForAudioDrainAsync(runner, tree);
            ClearAudioStreams(players);
        }

        world.QueueFree();
        await runner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (players.Length > 0)
        {
            await WaitForAudioDrainAsync(runner, tree);
        }
    }

    /// <summary>
    /// 递归收集 WorldAudio 下的全部播放器，包括测试期间动态创建且没有场景所有者的实例。
    /// </summary>
    private static AudioStreamPlayer[] FindAudioPlayers(Node world)
    {
        if (!world.HasNode("WorldAudio"))
        {
            return [];
        }

        Node audio = world.GetNode("WorldAudio");
        return audio.FindChildren("*", "AudioStreamPlayer", true, false)
            .OfType<AudioStreamPlayer>()
            .ToArray();
    }

    /// <summary>
    /// 在世界处理已禁用后停止全部有效播放器，使音频线程不再接收新的复音实例。
    /// </summary>
    private static void StopAudio(IEnumerable<AudioStreamPlayer> players)
    {
        foreach (AudioStreamPlayer player in players)
        {
            if (!GodotObject.IsInstanceValid(player))
            {
                continue;
            }

            player.Stop();
        }
    }

    /// <summary>
    /// 在停播已经跨过一个混音窗口后清空资源引用，避免 OGG 与 WAV 播放实例继续持有流。
    /// </summary>
    private static void ClearAudioStreams(IEnumerable<AudioStreamPlayer> players)
    {
        foreach (AudioStreamPlayer player in players)
        {
            if (!GodotObject.IsInstanceValid(player))
            {
                continue;
            }

            player.Stream = null;
        }
    }

    /// <summary>
    /// 等待覆盖多个音频混音周期的真实时间，让后端释放播放句柄而不依赖单个场景帧。
    /// </summary>
    private static async Task WaitForAudioDrainAsync(Node runner, SceneTree tree)
    {
        await runner.ToSignal(
            tree.CreateTimer(AudioDrainSeconds), SceneTreeTimer.SignalName.Timeout);
    }
}
