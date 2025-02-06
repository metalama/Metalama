// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Patterns.Contracts.Numeric;

#pragma warning disable SA1124

[RunTimeOrCompileTime]
internal sealed record DoubleBound : NumericBound
{
    private readonly double _value;

    public DoubleBound( double value, bool isAllowed ) : base( isAllowed )
    {
        this._value = value;
    }

    public override object ObjectValue => this._value;

    #region Conversions

    internal override bool TryConvertToByte( out byte value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case < byte.MinValue:
                value = 0;
                conversionResult = ConversionResult.TooSmall;

                return false;

            case > byte.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (byte) this._value;

                // Check the bounds after conversion to the target type, as this is the converted
                // value that matters when generating the C# code with the condition.
                conversionResult = value switch
                {
                    byte.MinValue => ConversionResult.ExactlyMinValue,
                    byte.MaxValue => ConversionResult.ExactlyMaxValue,
                    _ => ConversionResult.WithinRange
                };

                return true;
        }
    }

    internal override bool TryConvertToSByte( out sbyte value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case < sbyte.MinValue:
                value = 0;
                conversionResult = ConversionResult.TooSmall;

                return false;

            case > sbyte.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (sbyte) this._value;

                // Check the bounds after conversion to the target type, as this is the converted
                // value that matters when generating the C# code with the condition.
                conversionResult = value switch
                {
                    sbyte.MinValue => ConversionResult.ExactlyMinValue,
                    sbyte.MaxValue => ConversionResult.ExactlyMaxValue,
                    _ => ConversionResult.WithinRange
                };

                return true;
        }
    }

    internal override bool TryConvertToInt16( out short value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case < short.MinValue:
                value = 0;
                conversionResult = ConversionResult.TooSmall;

                return false;

            case > short.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (short) this._value;

                // Check the bounds after conversion to the target type, as this is the converted
                // value that matters when generating the C# code with the condition.
                conversionResult = value switch
                {
                    short.MinValue => ConversionResult.ExactlyMinValue,
                    short.MaxValue => ConversionResult.ExactlyMaxValue,
                    _ => ConversionResult.WithinRange
                };

                return true;
        }
    }

    internal override bool TryConvertToUInt16( out ushort value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case < ushort.MinValue:
                value = 0;
                conversionResult = ConversionResult.TooSmall;

                return false;

            case > ushort.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (ushort) this._value;

                // Check the bounds after conversion to the target type, as this is the converted
                // value that matters when generating the C# code with the condition.
                conversionResult = value switch
                {
                    ushort.MinValue => ConversionResult.ExactlyMinValue,
                    ushort.MaxValue => ConversionResult.ExactlyMaxValue,
                    _ => ConversionResult.WithinRange
                };

                return true;
        }
    }

    internal override bool TryConvertToInt32( out int value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case < int.MinValue:
                value = 0;
                conversionResult = ConversionResult.TooSmall;

                return false;

            case > int.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (int) this._value;

                // Check the bounds after conversion to the target type, as this is the converted
                // value that matters when generating the C# code with the condition.
                conversionResult = value switch
                {
                    int.MinValue => ConversionResult.ExactlyMinValue,
                    int.MaxValue => ConversionResult.ExactlyMaxValue,
                    _ => ConversionResult.WithinRange
                };

                return true;
        }
    }

    internal override bool TryConvertToUInt32( out uint value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case < uint.MinValue:
                value = 0;
                conversionResult = ConversionResult.TooSmall;

                return false;

            case > uint.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (uint) this._value;

                // Check the bounds after conversion to the target type, as this is the converted
                // value that matters when generating the C# code with the condition.
                conversionResult = value switch
                {
                    uint.MinValue => ConversionResult.ExactlyMinValue,
                    uint.MaxValue => ConversionResult.ExactlyMaxValue,
                    _ => ConversionResult.WithinRange
                };

                return true;
        }
    }

    internal override bool TryConvertToInt64( out long value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case < long.MinValue:
                value = 0;
                conversionResult = ConversionResult.TooSmall;

                return false;

            case > long.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (long) this._value;

                // Check the bounds after conversion to the target type, as this is the converted
                // value that matters when generating the C# code with the condition.
                conversionResult = value switch
                {
                    long.MinValue => ConversionResult.ExactlyMinValue,
                    long.MaxValue => ConversionResult.ExactlyMaxValue,
                    _ => ConversionResult.WithinRange
                };

                return true;
        }
    }

    internal override bool TryConvertToUInt64( out ulong value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case < 0:
                value = 0;
                conversionResult = ConversionResult.TooSmall;

                return false;

            case > ulong.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (ulong) this._value;

                // Check the bounds after conversion to the target type, as this is the converted
                // value that matters when generating the C# code with the condition.
                conversionResult = value switch
                {
                    ulong.MinValue => ConversionResult.ExactlyMinValue,
                    ulong.MaxValue => ConversionResult.ExactlyMaxValue,
                    _ => ConversionResult.WithinRange
                };

                return true;
        }
    }

    internal override bool TryConvertToDecimal( out decimal value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case < (double) decimal.MinValue:
                value = 0;
                conversionResult = ConversionResult.TooSmall;

                return false;

            case > (double) decimal.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (decimal) this._value;

                // Check the bounds after conversion to the target type, as this is the converted
                // value that matters when generating the C# code with the condition.
                conversionResult = value switch
                {
                    decimal.MinValue => ConversionResult.ExactlyMinValue,
                    decimal.MaxValue => ConversionResult.ExactlyMaxValue,
                    _ => ConversionResult.WithinRange
                };

                return true;
        }
    }

    internal override bool TryConvertToDouble( out double value, out ConversionResult conversionResult )
    {
        value = this._value;
        conversionResult = ConversionResult.WithinRange;

        return true;
    }

    internal override bool TryConvertToSingle( out float value, out ConversionResult conversionResult )
    {
        value = (float) this._value;
        conversionResult = ConversionResult.WithinRange;

        return true;
    }

    #endregion

    internal override void AppendValueToExpression( ExpressionBuilder expressionBuilder ) => expressionBuilder.AppendLiteral( this._value );
}