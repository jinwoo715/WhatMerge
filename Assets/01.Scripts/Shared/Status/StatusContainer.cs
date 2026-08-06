using System;
using System.Collections.Generic;

public interface IStatusReader
{
    bool HasStatus(ElementType elementType);
    int GetStackCount(ElementType elementType);
}

public interface IStatusModifier
{
    bool IsAddableStatus(ElementType elementType);
    void AddStatus(ElementType elementType);
    void RemoveStatus(ElementType elementType);
}

public class StatusContainer : IStatusReader, IStatusModifier
{
    public const int MaxAttributeStack = 5;

    private readonly Dictionary<ElementType, int> _elementCounts = new();

    public StatusContainer()
    {
        foreach (ElementType elementType in Enum.GetValues(typeof(ElementType)))
        {
            if (elementType != ElementType.None)
                _elementCounts.Add(elementType, 0);
        }
    }

    public bool HasStatus(ElementType elementType) => GetStackCount(elementType) > 0;

    public int GetStackCount(ElementType elementType)
    {
        return _elementCounts.TryGetValue(elementType, out int count) ? count : 0;
    }

    public bool IsAddableStatus(ElementType elementType)
    {
        return _elementCounts.TryGetValue(elementType, out int count)
            && count < MaxAttributeStack;
    }

    public void AddStatus(ElementType elementType)
    {
        if (!IsAddableStatus(elementType))
            return;

        _elementCounts[elementType]++;
    }
    public void RemoveStatus(ElementType elementType)
    {
        if (!_elementCounts.TryGetValue(elementType, out int count))
            return;

        _elementCounts[elementType] = Math.Max(0, count - 1);
    }

    public void Clear()
    {
        foreach (ElementType elementType in Enum.GetValues(typeof(ElementType)))
        {
            if (elementType != ElementType.None)
                _elementCounts[elementType] = 0;
        }
    }
}
