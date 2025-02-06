// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Reflection;
using Xunit.Sdk;

namespace Metalama.Patterns.Contracts.UnitTests;

/// <summary>
/// Test data used by <see cref="NumberComparerTests"/>.
/// </summary>
internal sealed class ConversionTestDataAttribute<TBound, TValue> : DataAttribute
{
    private readonly TValue _value;
    private readonly TBound _bound;
    private readonly bool? _expectedResult;
    private readonly object? _tag;

    public ConversionTestDataAttribute( TValue value, TBound bound, bool expectedResult, object? tag = null )
    {
        this._value = value;
        this._bound = bound;
        this._expectedResult = expectedResult;
        this._tag = tag;
    }

    public ConversionTestDataAttribute( TValue value, TBound bound, object? tag = null )
    {
        this._value = value;
        this._bound = bound;
        this._tag = tag;
        this._expectedResult = null;
    }

    public bool ForgiveRoundingError { get; set; }

    public override IEnumerable<object?[]> GetData( MethodInfo testMethod )
        => [[this._value, this._bound, this.ForgiveRoundingError ? !this._expectedResult : this._expectedResult, this._tag]];
}