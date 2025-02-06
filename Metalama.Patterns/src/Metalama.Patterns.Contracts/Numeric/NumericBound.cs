// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Patterns.Contracts.Numeric;

/// <summary>
/// Represents a single bound of a <see cref="NumericRange"/>.
/// </summary>
[RunTimeOrCompileTime]
public abstract record NumericBound : ICompileTimeSerializable
{
    protected internal NumericBound( bool isAllowed )
    {
        this.IsAllowed = isAllowed;
    }

    public static NumericBound Create( long value, bool isAllowed = true, int decimalPlaces = 0 )
    {
        return decimalPlaces switch
        {
            0 => new Int64Bound( value, isAllowed ),
            1 => new DecimalBound( value / 10m, isAllowed ),
            2 => new DecimalBound( value / 100m, isAllowed ),
            3 => new DecimalBound( value / 1_000m, isAllowed ),
            4 => new DecimalBound( value / 10_000m, isAllowed ),
            5 => new DecimalBound( value / 100_000m, isAllowed ),
            6 => new DecimalBound( value / 1000_000m, isAllowed ),
            _ => throw new ArgumentOutOfRangeException( nameof(decimalPlaces), "The number of decimal places must be between 0 and 6." )
        };
    }

    public static NumericBound Create( ulong value, bool isAllowed = true ) => new UInt64Bound( value, isAllowed );

    public static NumericBound Create( decimal value, bool isAllowed = true ) => new DecimalBound( value, isAllowed );

    public static NumericBound Create( double value, bool isAllowed = true ) => new DoubleBound( value, isAllowed );

    [PublicAPI]
    public bool IsAllowed { get; init; }

    [PublicAPI]
    public abstract object ObjectValue { get; }

    internal abstract bool TryConvertToByte( out byte value, out ConversionResult conversionResult );

    internal abstract bool TryConvertToSByte( out sbyte value, out ConversionResult conversionResult );

    internal abstract bool TryConvertToInt16( out short value, out ConversionResult conversionResult );

    internal abstract bool TryConvertToUInt16( out ushort value, out ConversionResult conversionResult );

    internal abstract bool TryConvertToInt32( out int value, out ConversionResult conversionResult );

    internal abstract bool TryConvertToUInt32( out uint value, out ConversionResult conversionResult );

    internal abstract bool TryConvertToInt64( out long value, out ConversionResult conversionResult );

    internal abstract bool TryConvertToUInt64( out ulong value, out ConversionResult conversionResult );

    internal abstract bool TryConvertToDecimal( out decimal value, out ConversionResult conversionResult );

    internal abstract bool TryConvertToDouble( out double value, out ConversionResult conversionResult );

    internal abstract bool TryConvertToSingle( out float value, out ConversionResult conversionResult );

    internal abstract void AppendValueToExpression( ExpressionBuilder expressionBuilder );

    // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
    public override string ToString() => this.ObjectValue.ToString() ?? "null";

    // ReSharper disable once UnusedMember.Global
    internal void AppendToExpression( ExpressionBuilder expressionBuilder )
    {
        expressionBuilder.AppendTypeName( typeof(NumericBound) );
        expressionBuilder.AppendVerbatim( "." );
        expressionBuilder.AppendVerbatim( nameof(Create) );
        expressionBuilder.AppendVerbatim( "(" );
        this.AppendValueToExpression( expressionBuilder );

        if ( !this.IsAllowed )
        {
            expressionBuilder.AppendVerbatim( ", false" );
        }

        expressionBuilder.AppendVerbatim( ")" );
    }

    // ReSharper disable once OutParameterValueIsAlwaysDiscarded.Global
    [CompileTime]
    internal bool TryConvert( IType valueType, [NotNullWhen( true )] out object? value, out ConversionResult conversionResult )
    {
        switch ( valueType.SpecialType )
        {
            case SpecialType.Object:
                value = this.ObjectValue;
                conversionResult = ConversionResult.WithinRange;

                return true;

            case SpecialType.SByte:
                if ( this.TryConvertToSByte( out var sbyteValue, out conversionResult ) )
                {
                    value = sbyteValue;

                    return true;
                }
                else
                {
                    value = null;

                    return false;
                }

            case SpecialType.Int16:
                if ( this.TryConvertToInt16( out var shortValue, out conversionResult ) )
                {
                    value = shortValue;

                    return true;
                }
                else
                {
                    value = null;

                    return false;
                }

            case SpecialType.Int32:
                if ( this.TryConvertToInt32( out var intValue, out conversionResult ) )
                {
                    value = intValue;

                    return true;
                }
                else
                {
                    value = null;

                    return false;
                }

            case SpecialType.Int64:
                if ( this.TryConvertToInt64( out var longValue, out conversionResult ) )
                {
                    value = longValue;

                    return true;
                }
                else
                {
                    value = null;

                    return false;
                }

            case SpecialType.Byte:
                if ( this.TryConvertToByte( out var byteValue, out conversionResult ) )
                {
                    value = byteValue;

                    return true;
                }
                else
                {
                    value = null;

                    return false;
                }

            case SpecialType.UInt16:
                if ( this.TryConvertToUInt16( out var ushortValue, out conversionResult ) )
                {
                    value = ushortValue;

                    return true;
                }
                else
                {
                    value = null;

                    return false;
                }

            case SpecialType.UInt32:
                if ( this.TryConvertToUInt32( out var uintValue, out conversionResult ) )
                {
                    value = uintValue;

                    return true;
                }
                else
                {
                    value = null;

                    return false;
                }

            case SpecialType.UInt64:
                if ( this.TryConvertToUInt64( out var ulongValue, out conversionResult ) )
                {
                    value = ulongValue;

                    return true;
                }
                else
                {
                    value = null;

                    return false;
                }

            case SpecialType.Decimal:
                if ( this.TryConvertToDecimal( out var decimalValue, out conversionResult ) )
                {
                    value = decimalValue;

                    return true;
                }
                else
                {
                    value = null;

                    return false;
                }

            case SpecialType.Single:
                if ( this.TryConvertToSingle( out var singleValue, out conversionResult ) )
                {
                    value = singleValue;

                    return true;
                }
                else
                {
                    value = null;

                    return false;
                }

            case SpecialType.Double:
                if ( this.TryConvertToDouble( out var doubleValue, out conversionResult ) )
                {
                    value = doubleValue;

                    return true;
                }
                else
                {
                    value = null;

                    return false;
                }

            default:
                conversionResult = ConversionResult.UnsupportedType;
                value = null;

                return false;
        }
    }

    internal void AppendConvertedValueToExpression( IType type, ExpressionBuilder builder )
    {
        switch ( type.SpecialType )
        {
            case SpecialType.SByte:
                if ( this.TryConvertToSByte( out var sbyteValue, out _ ) )
                {
                    builder.AppendLiteral( sbyteValue );
                }
                else
                {
                    throw new InvalidOperationException();
                }

                break;

            case SpecialType.Int16:
                if ( this.TryConvertToInt16( out var shortValue, out _ ) )
                {
                    builder.AppendLiteral( shortValue );
                }
                else
                {
                    throw new InvalidOperationException();
                }

                break;

            case SpecialType.Int32:
                if ( this.TryConvertToInt32( out var intValue, out _ ) )
                {
                    builder.AppendLiteral( intValue );
                }
                else
                {
                    throw new InvalidOperationException();
                }

                break;

            case SpecialType.Int64:
                if ( this.TryConvertToInt64( out var longValue, out _ ) )
                {
                    builder.AppendLiteral( longValue );
                }
                else
                {
                    throw new InvalidOperationException();
                }

                break;

            case SpecialType.Byte:
                if ( this.TryConvertToByte( out var byteValue, out _ ) )
                {
                    builder.AppendLiteral( byteValue );
                }
                else
                {
                    throw new InvalidOperationException();
                }

                break;

            case SpecialType.UInt16:
                if ( this.TryConvertToUInt16( out var ushortValue, out _ ) )
                {
                    builder.AppendLiteral( ushortValue );
                }
                else
                {
                    throw new InvalidOperationException();
                }

                break;

            case SpecialType.UInt32:
                if ( this.TryConvertToUInt32( out var uintValue, out _ ) )
                {
                    builder.AppendLiteral( uintValue );
                }
                else
                {
                    throw new InvalidOperationException();
                }

                break;

            case SpecialType.UInt64:
                if ( this.TryConvertToUInt64( out var ulongValue, out _ ) )
                {
                    builder.AppendLiteral( ulongValue );
                }
                else
                {
                    throw new InvalidOperationException();
                }

                break;

            case SpecialType.Decimal:
                if ( this.TryConvertToDecimal( out var decimalValue, out _ ) )
                {
                    builder.AppendLiteral( decimalValue );
                }
                else
                {
                    throw new InvalidOperationException();
                }

                break;

            case SpecialType.Single:
                if ( this.TryConvertToSingle( out var singleValue, out _ ) )
                {
                    builder.AppendLiteral( singleValue );
                }
                else
                {
                    throw new InvalidOperationException();
                }

                break;

            case SpecialType.Double:
                if ( this.TryConvertToDouble( out var doubleValue, out _ ) )
                {
                    builder.AppendLiteral( doubleValue );
                }
                else
                {
                    throw new InvalidOperationException();
                }

                break;

            default:
                throw new InvalidOperationException();
        }
    }
}