// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Patterns.Contracts.Numeric;

#pragma warning disable SA1124

[RunTimeOrCompileTime]
internal sealed record UInt64Bound : NumericBound
{
    private readonly ulong _value;

    public UInt64Bound( ulong value, bool isAllowed ) : base( isAllowed )
    {
        this._value = value;
    }

    public override object ObjectValue => this._value;

    #region Conversions

    internal override bool TryConvertToByte( out byte value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case byte.MinValue:
                value = (byte) this._value;
                conversionResult = ConversionResult.ExactlyMinValue;

                return true;

            case byte.MaxValue:
                value = (byte) this._value;
                conversionResult = ConversionResult.ExactlyMaxValue;

                return true;

            case > byte.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (byte) this._value;
                conversionResult = ConversionResult.WithinRange;

                return true;
        }
    }

    internal override bool TryConvertToSByte( out sbyte value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case (ulong) sbyte.MaxValue:
                value = (sbyte) this._value;
                conversionResult = ConversionResult.ExactlyMaxValue;

                return true;

            case > (ulong) sbyte.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (sbyte) this._value;
                conversionResult = ConversionResult.WithinRange;

                return true;
        }
    }

    internal override bool TryConvertToInt16( out short value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case (ulong) short.MaxValue:
                value = (short) this._value;
                conversionResult = ConversionResult.ExactlyMaxValue;

                return true;

            case > (ulong) short.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (short) this._value;
                conversionResult = ConversionResult.WithinRange;

                return true;
        }
    }

    internal override bool TryConvertToUInt16( out ushort value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case ushort.MinValue:
                value = (ushort) this._value;
                conversionResult = ConversionResult.ExactlyMinValue;

                return true;

            case ushort.MaxValue:
                value = (ushort) this._value;
                conversionResult = ConversionResult.ExactlyMaxValue;

                return true;

            case > ushort.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (ushort) this._value;
                conversionResult = ConversionResult.WithinRange;

                return true;
        }
    }

    internal override bool TryConvertToInt32( out int value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case int.MaxValue:
                value = (int) this._value;
                conversionResult = ConversionResult.ExactlyMaxValue;

                return true;

            case > int.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (int) this._value;
                conversionResult = ConversionResult.WithinRange;

                return true;
        }
    }

    internal override bool TryConvertToUInt32( out uint value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case uint.MinValue:
                value = (uint) this._value;
                conversionResult = ConversionResult.ExactlyMinValue;

                return true;

            case uint.MaxValue:
                value = (uint) this._value;
                conversionResult = ConversionResult.ExactlyMaxValue;

                return true;

            case > uint.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (uint) this._value;
                conversionResult = ConversionResult.WithinRange;

                return true;
        }
    }

    internal override bool TryConvertToInt64( out long value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case long.MaxValue:
                value = (long) this._value;
                conversionResult = ConversionResult.ExactlyMaxValue;

                return true;

            case > long.MaxValue:
                value = 0;
                conversionResult = ConversionResult.TooLarge;

                return false;

            default:
                value = (uint) this._value;
                conversionResult = ConversionResult.WithinRange;

                return true;
        }
    }

    internal override bool TryConvertToUInt64( out ulong value, out ConversionResult conversionResult )
    {
        switch ( this._value )
        {
            case ulong.MinValue:
                value = this._value;
                conversionResult = ConversionResult.ExactlyMinValue;

                return true;

            case ulong.MaxValue:
                value = this._value;
                conversionResult = ConversionResult.ExactlyMaxValue;

                return true;

            default:
                value = this._value;
                conversionResult = ConversionResult.WithinRange;

                return true;
        }
    }

    internal override bool TryConvertToDecimal( out decimal value, out ConversionResult conversionResult )
    {
        value = this._value;
        conversionResult = ConversionResult.WithinRange;

        return true;
    }

    internal override bool TryConvertToDouble( out double value, out ConversionResult conversionResult )
    {
        value = this._value;
        conversionResult = ConversionResult.WithinRange;

        return true;
    }

    internal override bool TryConvertToSingle( out float value, out ConversionResult conversionResult )
    {
        value = this._value;
        conversionResult = ConversionResult.WithinRange;

        return true;
    }

    #endregion

    internal override void AppendValueToExpression( ExpressionBuilder expressionBuilder ) => expressionBuilder.AppendLiteral( this._value );
}