// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class NegativeTests : RangeContractTestsBase
{
    private static void MethodWithByteParameter( [Negative] byte a ) { }

    private static void MethodWithUInt16Parameter( [Negative] ushort a ) { }

    private static void MethodWithUInt32Parameter( [Negative] uint a ) { }

    private static void MethodWithUInt64Parameter( [Negative] ulong a ) { }

    private static void MethodWithSByteParameter( [Negative] sbyte a ) { }

    private static void MethodWithInt16Parameter( [Negative] short a ) { }

    private static void MethodWithInt32Parameter( [Negative] int a ) { }

    private static void MethodWithInt64Parameter( [Negative] long a ) { }

    private static void MethodWithDecimalParameter( [Negative] decimal a ) { }

    private static void MethodWithDoubleParameter( [Negative] double a ) { }

    private static void MethodWithFloatParameter( [Negative] double a ) { }

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
    public void OneFails()
    {
        CallMethodsWithSignedParameter( 1, a => Assert.Throws<ArgumentOutOfRangeException>( a ) );
        CallMethodsWithUnsignedParameters( 1, a => Assert.Throws<ArgumentOutOfRangeException>( a ) );
    }

    [Fact]
    public void MinusOneSucceeds()
    {
        CallMethodsWithSignedParameter( -1 );
    }
}