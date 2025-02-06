// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Patterns.Contracts.Numeric;

#pragma warning disable IDE0004 // Remove Unnecessary Cast: in this problem domain, explicit casts add clarity.

// Resharper disable RedundantCast

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when added to a field, property or parameter, throws
/// an <see cref="ArgumentOutOfRangeException"/> if the target is assigned a value that
/// is smaller than a given value.
/// </summary>
/// <remarks>
///     <para>Null values are accepted and do not throw an exception.
/// </para>
/// </remarks>
/// <seealso href="@contract-types"/>
[PublicAPI]
public class GreaterThanOrEqualAttribute : RangeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualAttribute"/> class specifying a minimum value of type <see cref="long"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="decimalPlaces">When non-zero, interprets the <paramref name="min"/> number as <see cref="decimal"/> instead
    /// of <see cref="long"/> by adding a decimal point at the specified position. For instance, if <paramref name="min"/> is set to 1234 and <paramref name="decimalPlaces"/>
    /// is set to 3, the <paramref name="min"/> parameter will be reinterpreted as <c>1.234m</c>.</param> 
    public GreaterThanOrEqualAttribute( long min, int decimalPlaces = 0 )
        : base( NumericBound.Create( min, decimalPlaces: decimalPlaces ), default ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualAttribute"/> class specifying a minimum value of type <see cref="int"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="decimalPlaces">When non-zero, interprets the <paramref name="min"/> number as <see cref="decimal"/> instead
    /// of <see cref="long"/> by adding a decimal point at the specified position. For instance, if <paramref name="min"/> is set to 1234 and <paramref name="decimalPlaces"/>
    /// is set to 3, the <paramref name="min"/> parameter will be reinterpreted as <c>1.234m</c>.</param> 
    public GreaterThanOrEqualAttribute( int min, int decimalPlaces = 0 )
        : base( NumericBound.Create( min, decimalPlaces: decimalPlaces ), default ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualAttribute"/> class specifying a minimum value of type <see cref="short"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public GreaterThanOrEqualAttribute( short min )
        : base( NumericBound.Create( min ), default ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualAttribute"/> class specifying a minimum value of type <see cref="sbyte"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public GreaterThanOrEqualAttribute( sbyte min )
        : base( NumericBound.Create( min ), default ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualAttribute"/> class specifying a minimum value of type <see cref="ulong"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public GreaterThanOrEqualAttribute( ulong min )
        : base( NumericBound.Create( min ), default ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualAttribute"/> class specifying a minimum value of type <see cref="uint"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public GreaterThanOrEqualAttribute( uint min )
        : base( NumericBound.Create( min ), default ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualAttribute"/> class specifying a minimum value of type <see cref="ushort"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public GreaterThanOrEqualAttribute( ushort min )
        : base( NumericBound.Create( min ), default ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualAttribute"/> class specifying a minimum value of type <see cref="byte"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public GreaterThanOrEqualAttribute( byte min )
        : base( NumericBound.Create( min ), default ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualAttribute"/> class specifying a minimum value of type <see cref="double"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public GreaterThanOrEqualAttribute( double min )
        : base( NumericBound.Create( min ), default ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualAttribute"/> class specifying a minimum value of type <see cref="float"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public GreaterThanOrEqualAttribute( float min )
        : base( NumericBound.Create( min ), default ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualAttribute"/> class specifying a minimum value of type <see cref="decimal"/>.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    public GreaterThanOrEqualAttribute( decimal min )
        : base( NumericBound.Create( min ), default ) { }

    protected override void OnContractViolated( dynamic? value, [CompileTime] NumericRange range, ContractContext context )
    {
        context.Options.Templates!.OnGreaterThanOrEqualToContractViolated( value, range.MinValue!.ObjectValue, context );
    }
}