// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

public sealed class StrictlyNegativeTests : RangeContractTestsBase
{
    private static void MethodWithSByteParameter( [StrictlyNegative] sbyte a ) { }

    private static void MethodWithInt16Parameter( [StrictlyNegative] short a ) { }

    private static void MethodWithInt32Parameter( [StrictlyNegative] int a ) { }

    private static void MethodWithInt64Parameter( [StrictlyNegative] long a ) { }

    private static void MethodWithDecimalParameter( [StrictlyNegative] decimal a ) { }

    private static void MethodWithDoubleParameter( [StrictlyNegative] double a ) { }

    private static void MethodWithFloatParameter( [StrictlyNegative] double a ) { }

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

    [Fact]
    public void ZeroFails()
    {
        CallMethodsWithSignedParameter( 0, a => Assert.Throws<ArgumentOutOfRangeException>( a ) );
    }

    [Fact]
    public void OneFails()
    {
        CallMethodsWithSignedParameter( 1, a => Assert.Throws<ArgumentOutOfRangeException>( a ) );
    }

    [Fact]
    public void MinusOneSucceeds()
    {
        CallMethodsWithSignedParameter( -1 );
    }
}