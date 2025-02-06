// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

#pragma warning disable LAMA5006 // Intentionally with redundant checks.

public sealed class PositiveTests : RangeContractTestsBase
{
    private static void MethodWithByteParameter( [Positive] byte a ) { }

    private static void MethodWithUInt16Parameter( [Positive] ushort a ) { }

    private static void MethodWithUInt32Parameter( [Positive] uint a ) { }

    private static void MethodWithUInt64Parameter( [Positive] ulong a ) { }

    private static void MethodWithSByteParameter( [Positive] sbyte a ) { }

    private static void MethodWithInt16Parameter( [Positive] short a ) { }

    private static void MethodWithInt32Parameter( [Positive] int a ) { }

    private static void MethodWithInt64Parameter( [Positive] long a ) { }

    private static void MethodWithDecimalParameter( [Positive] decimal a ) { }

    private static void MethodWithDoubleParameter( [Positive] double a ) { }

    private static void MethodWithFloatParameter( [Positive] double a ) { }

    private static void CallMethodsWithSignedParameter( sbyte value, Action<Action>? action = null )
    {
        action ??= a => a();

        action( () => MethodWithSByteParameter( value ) );
        action( () => MethodWithInt16Parameter( value ) );
        action( () => MethodWithInt32Parameter( value ) );
        action( () => MethodWithInt64Parameter( value ) );
        action( () => MethodWithDecimalParameter( value ) );
        action( () => MethodWithDoubleParameter( value ) );
        action( () => MethodWithFloatParameter( value ) );
    }

    private static void CallMethodsWithUnsignedParameters( byte value, Action<Action>? action = null )
    {
        action ??= a => a();

        action( () => MethodWithByteParameter( value ) );
        action( () => MethodWithUInt16Parameter( value ) );
        action( () => MethodWithUInt32Parameter( value ) );
        action( () => MethodWithUInt64Parameter( value ) );
    }

    [Fact]
    public void ZeroSucceeds()
    {
        CallMethodsWithSignedParameter( 0 );
        CallMethodsWithUnsignedParameters( 0 );
    }

    [Fact]
    public void OneSucceeds()
    {
        CallMethodsWithSignedParameter( 1 );
        CallMethodsWithUnsignedParameters( 1 );
    }

    [Fact]
    public void MinusOneFails()
    {
        CallMethodsWithSignedParameter( -1, a => Assert.Throws<ArgumentOutOfRangeException>( a ) );
    }
}