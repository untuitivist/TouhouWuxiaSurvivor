using System.Collections;

namespace Rebirth.Core;

public delegate bool ComponentPredicate<T>(in T component);

public sealed class ComponentStore<T>(int capacity) : IReadOnlyList<T>
{
    private T[] components = new T[Math.Max(4, capacity)];
    public int Count { get; private set; }
    public ref T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            return ref components[index];
        }
    }
    T IReadOnlyList<T>.this[int index] => this[index];
    public Span<T> Active => components.AsSpan(0, Count);

    public void Add(T component)
    {
        if (Count == components.Length) Array.Resize(ref components, components.Length * 2);
        components[Count++] = component;
    }

    public void Clear()
    {
        Array.Clear(components, 0, Count);
        Count = 0;
    }

    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
        Array.Copy(components, index + 1, components, index, Count - index - 1);
        components[--Count] = default!;
    }

    public int RemoveAll(Predicate<T> predicate)
    {
        var destination = 0;
        while (destination < Count && !predicate(components[destination])) destination++;
        if (destination == Count) return 0;
        for (var source = destination + 1; source < Count; source++)
            if (!predicate(components[source])) components[destination++] = components[source];
        var removed = Count - destination;
        Array.Clear(components, destination, removed);
        Count = destination;
        return removed;
    }

    public int RemoveWhere(ComponentPredicate<T> predicate)
    {
        var destination = 0;
        while (destination < Count && !predicate(in components[destination])) destination++;
        if (destination == Count) return 0;
        for (var source = destination + 1; source < Count; source++)
            if (!predicate(in components[source])) components[destination++] = components[source];
        var removed = Count - destination;
        Array.Clear(components, destination, removed);
        Count = destination;
        return removed;
    }

    public int FindIndex(Predicate<T> predicate)
    {
        for (var index = 0; index < Count; index++)
            if (predicate(components[index])) return index;
        return -1;
    }

    public T? Find(Predicate<T> predicate)
    {
        var index = FindIndex(predicate);
        return index >= 0 ? components[index] : default;
    }

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(ComponentStore<T> store) : IEnumerator<T>
    {
        private int index = -1;
        public T Current => store.components[index];
        object? IEnumerator.Current => Current;
        public bool MoveNext() => ++index < store.Count;
        public void Reset() => index = -1;
        public void Dispose() { }
    }
}
