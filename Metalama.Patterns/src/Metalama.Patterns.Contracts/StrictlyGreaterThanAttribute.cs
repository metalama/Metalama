// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Patterns.Contracts.Numeric;

// Resharper disable RedundantCast

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when added to a field, property or parameter, throws
/// an <see cref="ArgumentOutOfRangeException"/> if the target is assigned a value that
/// is smaller than or equal to a given value.
/// </summary>
/// <remarks>
///     <para>Null values are accepted and do not throw an exception.
/// </para>
/// <para>
///     Floating-point values are tested to be greater than or equal to the minimum value
///     plus a tolerance value. The tolerance value is equal to the distance
///     of the value closest to the minimum value according to the precision
///     of the respective floating-point numerical data type.
/// </para>
/// </remarks>
/// <seealso href="@contract-types"/>
[PublicAPI]
public class StrictlyGreaterThanAttribute : RangeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyGreaterThanAttribute"/> class specifying a minimum value of type <see cref="long"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="decimalPlaces">When non-zero, interprets the <paramref name="min"/> number as <see cref="decimal"/> instead
    /// of <see cref="long"/> by adding a decimal point at the specified position. For instance, if <paramref name="min"/> is set to 1234 and <paramref name="decimalPlaces"/>
    /// is set to 3, the <paramref name="min"/> parameter will be reinterpreted as <c>1.234m</c>.</param> 
    public StrictlyGreaterThanAttribute( long min, int decimalPlaces = 0 )
        : base( NumericBound.Create( min, false, decimalPlaces ), null ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyGreaterThanAttribute"/> class specifying a minimum value of type <see cref="int"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="decimalPlaces">When non-zero, interprets the <paramref name="min"/> number <see cref="decimal"/> instead
    /// of <see cref="long"/> by adding a decimal point at the specified position. For instance, if <paramref name="min"/> is set to 1234 and <paramref name="decimalPlaces"/>
    /// is set to 3, the <paramref name="min"/> parameter will be reinterpreted as <c>1.234m</c>.</param> 
    public StrictlyGreaterThanAttribute( int min, int decimalPlaces = 0 )
        : base( NumericBound.Create( min, false ), null ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyGreaterThanAttribute"/> class specifying a minimum value of type <see cref="short"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public StrictlyGreaterThanAttribute( short min )
        : base( NumericBound.Create( min, false ), null ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyGreaterThanAttribute"/> class specifying a minimum value of type <see cref="sbyte"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public StrictlyGreaterThanAttribute( sbyte min )
        : base( NumericBound.Create( min, false ), null ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyGreaterThanAttribute"/> class specifying a minimum value of type <see cref="ulong"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public StrictlyGreaterThanAttribute( ulong min )
        : base( NumericBound.Create( min, false ), null ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyGreaterThanAttribute"/> class specifying a minimum value of type <see cref="uint"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public StrictlyGreaterThanAttribute( uint min )
        : base( NumericBound.Create( min, false ), null ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyGreaterThanAttribute"/> class specifying a minimum value of type <see cref="ushort"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public StrictlyGreaterThanAttribute( ushort min )
        : base( NumericBound.Create( min, false ), null ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyGreaterThanAttribute"/> class specifying a minimum value of type <see cref="byte"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public StrictlyGreaterThanAttribute( byte min )
        : base( NumericBound.Create( min, false ), null ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyGreaterThanAttribute"/> class specifying a minimum value of type <see cref="double"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public StrictlyGreaterThanAttribute( double min )
        : base( NumericBound.Create( min, false ), null ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyGreaterThanAttribute"/> class specifying a minimum value of type <see cref="decimal"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public StrictlyGreaterThanAttribute( decimal min )
        : base( NumericBound.Create( min, false ), null ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrictlyGreaterThanAttribute"/> class specifying a minimum value of type <see cref="float"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public StrictlyGreaterThanAttribute( float min )
        : base( NumericBound.Create( min, false ), null ) { }

    protected override void OnContractViolated( dynamic? value, [CompileTime] NumericRange range, ContractContext context )
    {
        context.Options.Templates!.OnStrictlyGreaterThanContractViolated( value, range.MinValue!.ObjectValue, context );
    }
}