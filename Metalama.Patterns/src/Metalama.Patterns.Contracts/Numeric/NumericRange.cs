// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Serialization;
using System.Text;

namespace Metalama.Patterns.Contracts.Numeric;

#pragma warning disable IDE0032 // Intentionally not using auto-properties.

/// <summary>
/// Describes a numeric range. Used by <see cref="RangeAttribute"/>.
/// </summary>
/// <seealso cref="RangeAttribute"/>
[PublicAPI]
[RunTimeOrCompileTime]
public readonly struct NumericRange : ICompileTimeSerializable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NumericRange"/> struct.
    /// </summary>
    public NumericRange( NumericBound? min, NumericBound? max )
    {
        this.MinValue = min;
        this.MaxValue = max;
    }

    /// <summary>
    /// Gets the minimal value, or <c>null</c> if the range has no minimal value.
    /// </summary>
    public NumericBound? MinValue { get; }

    /// <summary>
    /// Gets the maximal value, or <c>null</c> if the range has no maximal value.
    /// </summary>
    public NumericBound? MaxValue { get; }

    internal NumericRange WithStrictness( InequalityStrictness strictness )
    {
        var minValue = this.MinValue != null ? this.MinValue with { IsAllowed = strictness == InequalityStrictness.NonStrict } : null;
        var maxValue = this.MaxValue != null ? this.MaxValue with { IsAllowed = strictness == InequalityStrictness.NonStrict } : null;

        return new NumericRange( minValue, maxValue );
    }

    /// <summary>
    /// Determines if a value of type <see cref="double"/> is in the current range.
    /// </summary>
    public bool IsInRange( double value )
    {
        if ( this.MinValue != null )
        {
            if ( this.MinValue.TryConvertToDouble( out var minValue, out _ ) )
            {
                if ( this.MinValue.IsAllowed )
                {
                    if ( value < minValue )
                    {
                        return false;
                    }
                }
                else
                {
                    if ( value <= minValue )
                    {
                        return false;
                    }
                }
            }
        }

        if ( this.MaxValue != null )
        {
            if ( this.MaxValue.TryConvertToDouble( out var maxValue, out _ ) )
            {
                if ( this.MaxValue.IsAllowed )
                {
                    if ( value > maxValue )
                    {
                        return false;
                    }
                }
                else
                {
                    if ( value >= maxValue )
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Determines if a value of type <see cref="decimal"/> is in the current range.
    /// </summary>
    public bool IsInRange( decimal value )
    {
        if ( this.MinValue != null )
        {
            if ( this.MinValue.TryConvertToDecimal( out var minValue, out _ ) )
            {
                if ( this.MinValue.IsAllowed )
                {
                    if ( value < minValue )
                    {
                        return false;
                    }
                }
                else
                {
                    if ( value <= minValue )
                    {
                        return false;
                    }
                }
            }
        }

        if ( this.MaxValue != null )
        {
            if ( this.MaxValue.TryConvertToDecimal( out var maxValue, out _ ) )
            {
                if ( this.MaxValue.IsAllowed )
                {
                    if ( value > maxValue )
                    {
                        return false;
                    }
                }
                else
                {
                    if ( value >= maxValue )
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Determines if a value of type <see cref="long"/> is in the current range.
    /// </summary>
    public bool IsInRange( long value )
    {
        if ( this.MinValue != null )
        {
            if ( this.MinValue.TryConvertToInt64( out var minValue, out _ ) )
            {
                if ( this.MinValue.IsAllowed )
                {
                    if ( value < minValue )
                    {
                        return false;
                    }
                }
                else
                {
                    if ( value <= minValue )
                    {
                        return false;
                    }
                }
            }
        }

        if ( this.MaxValue != null )
        {
            if ( this.MaxValue.TryConvertToInt64( out var maxValue, out _ ) )
            {
                if ( this.MaxValue.IsAllowed )
                {
                    if ( value > maxValue )
                    {
                        return false;
                    }
                }
                else
                {
                    if ( value >= maxValue )
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Determines if a value of type <see cref="ulong"/> is in the current range.
    /// </summary>
    public bool IsInRange( ulong value )
    {
        if ( this.MinValue != null )
        {
            if ( this.MinValue.TryConvertToUInt64( out var minValue, out _ ) )
            {
                if ( this.MinValue.IsAllowed )
                {
                    if ( value < minValue )
                    {
                        return false;
                    }
                }
                else
                {
                    if ( value <= minValue )
                    {
                        return false;
                    }
                }
            }
        }

        if ( this.MaxValue != null )
        {
            if ( this.MaxValue.TryConvertToUInt64( out var maxValue, out _ ) )
            {
                if ( this.MaxValue.IsAllowed )
                {
                    if ( value > maxValue )
                    {
                        return false;
                    }
                }
                else
                {
                    if ( value >= maxValue )
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Determines if a value of unknown type is in the current range. Returns <c>false</c> if the type
    /// is unsupported.
    /// </summary>
    public bool IsInRange( object? value )
    {
        switch ( value )
        {
            case null:
                return true;

            case float floatValue:
                return this.IsInRange( floatValue );

            case double doubleValue:
                return this.IsInRange( doubleValue );

            case decimal decimalValue:
                return this.IsInRange( decimalValue );

            case long longValue:
                return this.IsInRange( longValue );

            case ulong ulongValue:
                return this.IsInRange( ulongValue );

            case int intValue:
                return this.IsInRange( intValue );

            case uint uintValue:
                return this.IsInRange( uintValue );

            case short shortValue:
                return this.IsInRange( shortValue );

            case ushort ushortValue:
                return this.IsInRange( ushortValue );

            case byte byteValue:
                return this.IsInRange( byteValue );

            case sbyte sbyteValue:
                return this.IsInRange( sbyteValue );

            default:
                return false;
        }
    }

    internal bool GeneratePattern( IType valueType, ExpressionBuilder builder, IExpression value )
    {
        var range = this;

        var nonNullableType = valueType.ToNonNullable();
        var isObject = nonNullableType.SpecialType == SpecialType.Object;

        // Check if we have checks of min or max value.
        ConversionResult minConversionResult = default;
        ConversionResult maxConversionResult = default;
        var hasMinCheck = this.MinValue != null && this.MinValue.TryConvert( nonNullableType, out _, out minConversionResult );
        var hasMaxCheck = this.MaxValue != null && this.MaxValue.TryConvert( nonNullableType, out _, out maxConversionResult );

        // Eliminate redundant checks.
        if ( hasMinCheck && minConversionResult == ConversionResult.ExactlyMinValue && this.MinValue!.IsAllowed )
        {
            hasMinCheck = false;
        }

        if ( hasMaxCheck && maxConversionResult == ConversionResult.ExactlyMaxValue && this.MaxValue!.IsAllowed )
        {
            hasMaxCheck = false;
        }

        if ( !hasMaxCheck && !hasMinCheck )
        {
            // No condition necessary.
            return false;
        }

        if ( isObject )
        {
            if ( this.MinValue != null )
            {
                builder.AppendTypeName( typeof(NumberComparer) );
                builder.AppendVerbatim( "." );
                builder.AppendVerbatim( this.MinValue.IsAllowed ? nameof(NumberComparer.IsStrictlySmallerThan) : nameof(NumberComparer.IsGreaterThan) );
                builder.AppendVerbatim( "(" );
                builder.AppendExpression( value );
                builder.AppendVerbatim( ", " );
                this.MinValue.AppendValueToExpression( builder );
                builder.AppendVerbatim( ")" );
                builder.AppendVerbatim( " == true" );
            }

            if ( this.MaxValue != null )
            {
                if ( this.MinValue != null )
                {
                    builder.AppendVerbatim( " || " );
                }

                builder.AppendTypeName( typeof(NumberComparer) );
                builder.AppendVerbatim( "." );
                builder.AppendVerbatim( this.MaxValue.IsAllowed ? nameof(NumberComparer.IsStrictlyGreaterThan) : nameof(NumberComparer.IsGreaterThan) );
                builder.AppendVerbatim( "(" );
                builder.AppendExpression( value );
                builder.AppendVerbatim( ", " );
                this.MaxValue.AppendValueToExpression( builder );
                builder.AppendVerbatim( ")" );
                builder.AppendVerbatim( " == true" );
            }
        }
        else
        {
            void AppendMin()
            {
                if ( range.MinValue!.IsAllowed )
                {
                    builder.AppendVerbatim( " < " );
                }
                else
                {
                    builder.AppendVerbatim( " <= " );
                }

                range.MinValue.AppendConvertedValueToExpression( nonNullableType, builder );
            }

            void AppendMax()
            {
                if ( range.MaxValue!.IsAllowed )
                {
                    builder.AppendVerbatim( " > " );
                }
                else
                {
                    builder.AppendVerbatim( " >= " );
                }

                range.MaxValue.AppendConvertedValueToExpression( nonNullableType, builder );
            }

            if ( hasMinCheck && hasMaxCheck )
            {
                builder.AppendExpression( value );
                builder.AppendVerbatim( " is " );

                AppendMin();

                builder.AppendVerbatim( " or " );

                AppendMax();
            }
            else if ( hasMinCheck )
            {
                builder.AppendExpression( value );
             
                AppendMin();
            }
            else
            {
                builder.AppendExpression( value );
             
                AppendMax();
            }
        }

        return true;
    }

    internal NumericRangeTypeSupport IsTypeSupported( IType type )
    {
        switch ( type.ToNonNullable().SpecialType )
        {
            case SpecialType.Object:
                return NumericRangeTypeSupport.Supported;

            case SpecialType.Byte:
            case SpecialType.SByte:
            case SpecialType.Int16:
            case SpecialType.UInt16:
            case SpecialType.Int32:
            case SpecialType.UInt32:
            case SpecialType.Int64:
            case SpecialType.UInt64:
            case SpecialType.Decimal:
            case SpecialType.Single:
            case SpecialType.Double:
                var isRedundant = true;

                if ( this.MinValue != null )
                {
                    this.MinValue.TryConvert( type, out _, out var minConversionResult );

                    if ( minConversionResult is ConversionResult.TooLarge or ConversionResult.UnsupportedType )
                    {
                        return NumericRangeTypeSupport.NotSupported;
                    }

                    if ( minConversionResult == ConversionResult.ExactlyMaxValue && !this.MinValue.IsAllowed )
                    {
                        return NumericRangeTypeSupport.NotSupported;
                    }

                    if ( minConversionResult == ConversionResult.TooSmall ||
                         (minConversionResult == ConversionResult.ExactlyMinValue && this.MinValue.IsAllowed) )
                    {
                        // This check is redundant.
                    }
                    else
                    {
                        isRedundant = false;
                    }
                }

                if ( this.MaxValue != null )
                {
                    this.MaxValue.TryConvert( type, out _, out var maxConversionResult );

                    if ( maxConversionResult is ConversionResult.TooSmall or ConversionResult.UnsupportedType )
                    {
                        return NumericRangeTypeSupport.NotSupported;
                    }

                    if ( maxConversionResult == ConversionResult.ExactlyMinValue && !this.MaxValue.IsAllowed )
                    {
                        return NumericRangeTypeSupport.NotSupported;
                    }

                    if ( maxConversionResult == ConversionResult.TooLarge ||
                         (maxConversionResult == ConversionResult.ExactlyMaxValue && this.MaxValue.IsAllowed) )
                    {
                        // This check is redundant.
                    }
                    else
                    {
                        isRedundant = false;
                    }
                }

                if ( isRedundant )
                {
                    return NumericRangeTypeSupport.Redundant;
                }

                return NumericRangeTypeSupport.Supported;

            default:
                return NumericRangeTypeSupport.NotSupported;
        }
    }

    public static NumericRange Create( long? min, long? max, bool areBoundsAllowed = true )
    {
        var minBound = min == null ? null : NumericBound.Create( min.Value, areBoundsAllowed );
        var maxBound = max == null ? null : NumericBound.Create( max.Value, areBoundsAllowed );

        return new NumericRange( minBound, maxBound );
    }

    public static NumericRange Create( ulong? min, ulong? max, bool areBoundsAllowed = true )
    {
        var minBound = min == null ? null : NumericBound.Create( min.Value, areBoundsAllowed );
        var maxBound = max == null ? null : NumericBound.Create( max.Value, areBoundsAllowed );

        return new NumericRange( minBound, maxBound );
    }

    public static NumericRange Create( double? min, double? max, bool areBoundsAllowed = true )
    {
        var minBound = min == null ? null : NumericBound.Create( min.Value, areBoundsAllowed );
        var maxBound = max == null ? null : NumericBound.Create( max.Value, areBoundsAllowed );

        return new NumericRange( minBound, maxBound );
    }

    public static NumericRange Create( decimal? min, decimal? max, bool areBoundsAllowed = true )
    {
        var minBound = min == null ? null : NumericBound.Create( min.Value, areBoundsAllowed );
        var maxBound = max == null ? null : NumericBound.Create( max.Value, areBoundsAllowed );

        return new NumericRange( minBound, maxBound );
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();

        if ( this.MinValue != null )
        {
            stringBuilder.Append( this.MinValue.IsAllowed ? '[' : ']' );
            stringBuilder.Append( this.MinValue.ObjectValue );
        }
        else
        {
            stringBuilder.Append( "[-\u221e" ); // Add minus infinite symbol.
        }

        stringBuilder.Append( ", " );

        if ( this.MaxValue != null )
        {
            stringBuilder.Append( this.MaxValue.ObjectValue );
            stringBuilder.Append( this.MaxValue.IsAllowed ? ']' : '[' );
        }
        else
        {
            stringBuilder.Append( "\u221e]" ); // Add infinite symbol.
        }

        return stringBuilder.ToString();
    }
}