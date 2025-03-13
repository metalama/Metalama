// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using FluentAssertions;
using Metalama.Patterns.Observability.UnitTests.Assets.Core;
using Metalama.Patterns.Observability.UnitTests.Assets.Initializers;
using Xunit;

namespace Metalama.Patterns.Observability.UnitTests;

public sealed class InitializerTests : InpcTestsBase
{
    [Fact]
    public void InpcAutoPropertyWithInitializer()
    {
        var v = new A();

        this.SubscribeTo( v );

        this.EventsFrom( () => v.A1.S1 = 1 ).Should().Equal( "RefA1S1" );

        this.EventsFrom( () => v.A1 = new Simple() ).Should().Equal( "RefA1S1", "A1" );

        this.EventsFrom( () => v.A1.S1 = 1 ).Should().Equal( "RefA1S1" );
    }
}