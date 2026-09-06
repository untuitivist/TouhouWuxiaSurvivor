namespace Rebirth.Core;

public struct HitHistory
{
    private int count;
    private int first;
    private int second;
    private int third;
    private int fourth;
    private HashSet<int>? overflow;

    public readonly bool Contains(int identity)
        => (count > 0 && first == identity) || (count > 1 && second == identity)
        || (count > 2 && third == identity) || (count > 3 && fourth == identity)
        || overflow?.Contains(identity) == true;

    public void Add(int identity)
    {
        if (Contains(identity)) return;
        switch (count++)
        {
            case 0: first = identity; break;
            case 1: second = identity; break;
            case 2: third = identity; break;
            case 3: fourth = identity; break;
            default: (overflow ??= []).Add(identity); break;
        }
    }
}
