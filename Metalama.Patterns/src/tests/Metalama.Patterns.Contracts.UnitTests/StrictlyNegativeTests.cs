// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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