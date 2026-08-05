// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Xunit;

namespace Metalama.Testing.Hooks.Tests;

/// <summary>
/// Tests <see cref="TestFaultInjector"/>: that an injection point that has not been armed is a no-op, that an armed
/// one throws the expected exception, and that disarming restores the no-op behaviour.
/// </summary>
public sealed class TestFaultInjectorTests
{
    private const string _injectionPointName = "ComponentUnderTest.Operation:Entry";

    [Fact]
    public void UnarmedInjectionPointDoesNotThrow()
    {
        var injector = new TestFaultInjector();

        injector.InjectFault( _injectionPointName );
    }

    [Fact]
    public void ArmedInjectionPointThrowsDefaultException()
    {
        var injector = new TestFaultInjector();
        injector.ArmFault( _injectionPointName );

        var exception = Assert.Throws<InvalidOperationException>( () => injector.InjectFault( _injectionPointName ) );

        Assert.Contains( _injectionPointName, exception.Message, StringComparison.Ordinal );
    }

    [Fact]
    public void ArmedInjectionPointThrowsTheGivenException()
    {
        var injector = new TestFaultInjector();
        injector.ArmFault( _injectionPointName, () => new FormatException( "Test." ) );

        Assert.Throws<FormatException>( () => injector.InjectFault( _injectionPointName ) );
    }

    [Fact]
    public void OtherInjectionPointsAreNotAffected()
    {
        var injector = new TestFaultInjector();
        injector.ArmFault( _injectionPointName );

        injector.InjectFault( "ComponentUnderTest.Operation:Exit" );
    }

    [Fact]
    public void DisarmedInjectionPointDoesNotThrow()
    {
        var injector = new TestFaultInjector();
        injector.ArmFault( _injectionPointName );
        injector.DisarmFault( _injectionPointName );

        injector.InjectFault( _injectionPointName );
    }
}
