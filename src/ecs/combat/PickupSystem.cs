using Godot;
using TouhouWuxiaSurvivor.Actors.Player;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>批量处理强化掉落物寿命、闪烁和玩家接触拾取。</summary>
public sealed class PickupSystem
{
    /// <summary>更新掉落物并把成功拾取交给玩家强化组件。</summary>
    public void Step(
        List<PickupComponent> items,
        Vector2 playerPosition,
        PlayerBuffController buffs,
        float delta,
        Action collected)
    {
        for (int index = items.Count - 1; index >= 0; index--)
        {
            PickupComponent pickup = items[index];
            pickup.Lifetime -= delta;
            pickup.BlinkTime += delta;
            if (pickup.Lifetime <= 0.0f)
            {
                items.RemoveAt(index);
                continue;
            }

            if (pickup.Position.DistanceSquaredTo(playerPosition) <= 14.0f * 14.0f)
            {
                buffs.Apply(pickup.Definition);
                collected();
                items.RemoveAt(index);
                continue;
            }

            items[index] = pickup;
        }
    }
}
