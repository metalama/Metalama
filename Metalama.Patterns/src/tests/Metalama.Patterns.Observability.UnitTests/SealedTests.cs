// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using FluentAssertions;
using Metalama.Patterns.Observability.UnitTests.Assets.Core;
using Metalama.Patterns.Observability.UnitTests.Assets.Sealed;
using Xunit;

namespace Metalama.Patterns.Observability.UnitTests;

public sealed class SealedTests : InpcTestsBase
{
    [Fact]
    public void SealedWithNoBase()
    {
        var v = new C1();

        var sv = this.SubscribeTo( v );

        this.EventsFrom( () => v.C1P1 = 1 )
            .Should()
            .Equal( "C1P1" );

        var a = new Simple();

        var sa = this.SubscribeTo( a );

        this.EventsFrom( () => v.C1P2 = a )
            .Should()
            .Equal( (sv, "C1P3"), (sv, "C1P2") );

        this.EventsFrom( () => a.S1 = 1 )
            .Should()
            .Equal( (sa, "S1"), (sv, "C1P3") );
    }

    [Fact]
    public void SealedInheritsFromSimple()
    {
        var v = new C2();

        this.SubscribeTo( v );

        this.EventsFrom( () => v.S1 = 1 )
            .Should()
            .Equal( "C2P3", "S1" );

        this.EventsFrom( () => v.S2 = 1 )
            .Should()
            .Equal( "S2" );

        this.EventsFrom( () => v.C2P1 = 1 )
            .Should()
            .Equal( "C2P1" );
    }
}