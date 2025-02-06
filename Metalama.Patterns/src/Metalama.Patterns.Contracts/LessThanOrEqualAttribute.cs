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
/// is greater than a given value.
/// </summary>
/// <remarks>
///     <para>Null values are accepted and do not throw an exception.</para>
/// <para>Error message can use additional argument <value>{4}</value> to refer to the minimum value used.</para>
/// </remarks>
/// <seealso href="@contract-types"/>
[PublicAPI]
public class LessThanOrEqualAttribute : RangeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualAttribute"/> class specifying a maximum value of type <see cref="long"/>.
    /// </summary>
    /// <param name="max">The maximum allowed value.</param>
    /// <param name="decimalPlaces">When non-zero, interprets the <paramref name="max"/> number as <see cref="decimal"/> instead
    /// of <see cref="long"/> by adding a decimal point at the specified position. For instance, if <paramref name="max"/> is set to 1234 and <paramref name="decimalPlaces"/>
    /// is set to 3, the <paramref name="max"/> parameter will be reinterpreted as <c>1.234m</c>.</param> 
    public LessThanOrEqualAttribute( long max, int decimalPlaces = 0 )
        : base( default, NumericBound.Create( max, decimalPlaces: decimalPlaces ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualAttribute"/> class specifying a maximum value of type <see cref="int"/>.
    /// </summary>
    /// <param name="max">The maximum allowed value.</param>
    /// <param name="decimalPlaces">When non-zero, interprets the <paramref name="max"/> number as <see cref="decimal"/> instead
    /// of <see cref="long"/> by adding a decimal point at the specified position. For instance, if <paramref name="max"/> is set to 1234 and <paramref name="decimalPlaces"/>
    /// is set to 3, the <paramref name="max"/> parameter will be reinterpreted as <c>1.234m</c>.</param> 
    public LessThanOrEqualAttribute( int max, int decimalPlaces = 0 )
        : base( default, NumericBound.Create( max, decimalPlaces: decimalPlaces ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualAttribute"/> class specifying a maximum value of type <see cref="short"/>.
    /// </summary>
    /// <param name="max">The maximum allowed value.</param>
    public LessThanOrEqualAttribute( short max )
        : base( default, NumericBound.Create( max ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualAttribute"/> class specifying a maximum value of type <see cref="sbyte"/>.
    /// </summary>
    /// <param name="max">The maximum allowed value.</param>
    public LessThanOrEqualAttribute( sbyte max )
        : base( default, NumericBound.Create( max ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualAttribute"/> class specifying a maximum value of type <see cref="ulong"/>.
    /// </summary>
    /// <param name="max">The maximum allowed value.</param>
    public LessThanOrEqualAttribute( ulong max )
        : base( default, NumericBound.Create( max ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualAttribute"/> class specifying a maximum value of type <see cref="uint"/>.
    /// </summary>
    /// <param name="max">The maximum allowed value.</param>
    public LessThanOrEqualAttribute( uint max )
        : base( default, NumericBound.Create( max ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualAttribute"/> class specifying a maximum value of type <see cref="ushort"/>.
    /// </summary>
    /// <param name="max">The maximum allowed value.</param>
    public LessThanOrEqualAttribute( ushort max )
        : base( default, NumericBound.Create( max ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualAttribute"/> class specifying a maximum value of type <see cref="byte"/>.
    /// </summary>
    /// <param name="max">The maximum allowed value.</param>
    public LessThanOrEqualAttribute( byte max )
        : base( default, NumericBound.Create( max ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualAttribute"/> class specifying a maximum value of type <see cref="double"/>.
    /// </summary>
    /// <param name="max">The maximum allowed value.</param>
    public LessThanOrEqualAttribute( double max )
        : base( default, NumericBound.Create( max ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualAttribute"/> class specifying a maximum value of type <see cref="float"/>.
    /// </summary>
    /// <param name="max">The maximum allowed value.</param>
    public LessThanOrEqualAttribute( float max )
        : base( default, NumericBound.Create( max ) ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualAttribute"/> class specifying a maximum value of type <see cref="decimal"/>.
    /// </summary>
    /// <param name="max">The maximum allowed value.</param>
    public LessThanOrEqualAttribute( decimal max )
        : base( default, NumericBound.Create( max ) ) { }

    protected override void OnContractViolated( dynamic? value, [CompileTime] NumericRange range, ContractContext context )
    {
        context.Options.Templates!.OnLessThanOrEqualToContractViolated( value, range.MaxValue!.ObjectValue, context );
    }
}