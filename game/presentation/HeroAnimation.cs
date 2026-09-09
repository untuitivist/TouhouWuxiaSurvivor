namespace Rebirth.Presentation;

public sealed class HeroAnimation
{
    private float previousCooldown;
    private float castStarted = float.NegativeInfinity;
    private float previousTime = -1;

    public int Frame(float time, float cooldown, bool moving, bool beam, bool warming)
    {
        if (time < previousTime) Reset();
        if (cooldown > previousCooldown + 0.0001f) castStarted = time;
        previousCooldown = cooldown;
        previousTime = time;
        if (beam) return warming ? 5 : 6;
        var age = time - castStarted;
        if (age < 0.11f) return 5;
        if (age < 0.18f) return 6;
        if (age < 0.29f) return 7;
        return moving ? 1 + (int)(time * 9) % 4 : 0;
    }

    public void Reset()
    {
        previousCooldown = 0;
        castStarted = float.NegativeInfinity;
        previousTime = -1;
    }
}
