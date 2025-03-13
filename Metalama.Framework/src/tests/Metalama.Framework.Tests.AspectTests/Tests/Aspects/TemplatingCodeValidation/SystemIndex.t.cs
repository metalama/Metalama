using System.Collections.Generic;
namespace System;
// Stripped-down version of System.Index; tests that including it in the code base does not cause issues.
internal readonly struct Index : IEquatable<Index>
{
    private readonly int _value;
    public Index(int value, bool fromEnd = false)
    {
        if (value < 0)
        {
            ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
        }
        if (fromEnd)
            _value = ~value;
        else
            _value = value;
    }
    private Index(int value)
    {
        _value = value;
    }
    public static Index Start => new Index(0);
    public static Index End => new Index(~0);
    public static Index FromStart(int value)
    {
        if (value < 0)
        {
            ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
        }
        return new Index(value);
    }
    public static Index FromEnd(int value)
    {
        if (value < 0)
        {
            ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
        }
        return new Index(~value);
    }
    public int Value
    {
        get
        {
            if (_value < 0)
                return ~_value;
            else
                return _value;
        }
    }
    public bool IsFromEnd => _value < 0;
    public int GetOffset(int length)
    {
        int offset = _value;
        if (IsFromEnd)
        {
            offset += length + 1;
        }
        return offset;
    }
    public override bool Equals(object? value) => value is Index && _value == ((Index)value)._value;
    public bool Equals(Index other) => _value == other._value;
    public override int GetHashCode() => _value;
    public static implicit operator Index(int value) => FromStart(value);
    public override string ToString()
    {
        if (IsFromEnd)
            return ToStringFromEnd();
        return ((uint)Value).ToString();
    }
    private string ToStringFromEnd()
    {
        return '^' + Value.ToString();
    }
    private static class ThrowHelper
    {
        public static void ThrowValueArgumentOutOfRange_NeedNonNegNumException()
        {
            throw new ArgumentOutOfRangeException("value", "Non-negative number required.");
        }
    }
}