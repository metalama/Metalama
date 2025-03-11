// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Patterns.Contracts.UnitTests;

#pragma warning disable LAMA5006 // Intentionally with redundant checks.

public sealed class StrictlyPositiveTests : RangeContractTestsBase
{
    private static void MethodWithByteParameter( [StrictlyPositive] byte a ) { }

    private static void MethodWithUInt16Parameter( [StrictlyPositive] ushort a ) { }

    private static void MethodWithUInt32Parameter( [StrictlyPositive] uint a ) { }

    private static void MethodWithUInt64Parameter( [StrictlyPositive] ulong a ) { }

    private static void MethodWithSByteParameter( [StrictlyPositive] sbyte a ) { }

    private static void MethodWithInt16Parameter( [StrictlyPositive] short a ) { }

    private static void MethodWithInt32Parameter( [StrictlyPositive] int a ) { }

    private static void MethodWithInt64Parameter( [StrictlyPositive] long a ) { }

    private static void MethodWithDecimalParameter( [StrictlyPositive] decimal a ) { }

    private static void MethodWithDoubleParameter( [StrictlyPositive] double a ) { }

    private static void MethodWithFloatParameter( [StrictlyPositive] double a ) { }

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
    public void ZeroFails()
    {
        CallMethodsWithSignedParameter( 0, a => Assert.Throws<ArgumentOutOfRangeException>( a ) );
        CallMethodsWithUnsignedParameters( 0, a => Assert.Throws<ArgumentOutOfRangeException>( a ) );
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