using Godot;

namespace TouhouWuxiaSurvivor.Ecs.Combat;

/// <summary>批量处理灵息呼吸、吸附、合并和经验交付。</summary>
public sealed class SpiritSystem
{
    /// <summary>把进入吸引范围的灵息移动到玩家并交付累计经验。</summary>
    public void Step(
        List<SpiritComponent> items,
        Vector2 playerPosition,
        float attractionRange,
        float delta,
        Action<int> collected)
    {
        for (int index = items.Count - 1; index >= 0; index--)
        {
            SpiritComponent spirit = items[index];
            spirit.BeginPhysicsStep();
            spirit.PulseTime += delta;
            float distance = spirit.Position.DistanceTo(playerPosition);
            if (distance <= 11.0f)
            {
                collected(spirit.Value);
                items.RemoveAt(index);
                continue;
            }

            if (distance <= attractionRange)
            {
                float speed = Mathf.Lerp(230.0f, 120.0f,
                    Mathf.Clamp(distance / Math.Max(1.0f, attractionRange), 0.0f, 1.0f));
                spirit.Position = spirit.Position.MoveToward(playerPosition, speed * delta);
            }

            items[index] = spirit;
        }
    }
}
