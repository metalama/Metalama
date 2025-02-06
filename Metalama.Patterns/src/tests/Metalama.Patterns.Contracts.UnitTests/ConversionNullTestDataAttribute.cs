// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Reflection;
using Xunit.Sdk;

namespace Metalama.Patterns.Contracts.UnitTests;

/// <summary>
/// Test data used by <see cref="NumberComparerTests"/> that represents a <c>null</c> result.
/// </summary>
internal sealed class ConversionNullTestDataAttribute<TBound, TValue> : DataAttribute
{
    private readonly TValue _value;
    private readonly TBound _bound;
    private readonly object? _tag;

    public ConversionNullTestDataAttribute( TValue value, TBound bound, object? tag = null )
    {
        this._value = value;
        this._bound = bound;
        this._tag = tag;
    }

    public override IEnumerable<object?[]> GetData( MethodInfo testMethod ) => [[this._value, this._bound, null, this._tag]];
}