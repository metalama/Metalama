// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using FluentAssertions;
using Metalama.Patterns.Observability.UnitTests.Assets.Core;
using Metalama.Patterns.Observability.UnitTests.Assets.Inheritance;
using Xunit;

namespace Metalama.Patterns.Observability.UnitTests;

public sealed class InheritanceTests : InpcTestsBase
{
    private void Test( Simple v )
    {
        v.S1.Should().Be( 0 );
        v.S2.Should().Be( 0 );
        v.S3.Should().Be( 0 );

        this.SubscribeTo( v );

        this.EventsFrom( () => v.S1 = 0 )
            .Should()
            .BeEmpty();

        this.EventsFrom( () => v.S2 = 0 )
            .Should()
            .BeEmpty();

        this.EventsFrom( () => v.S3 = 0 )
            .Should()
            .BeEmpty();

        this.EventsFrom( () => v.S1 = 1 )
            .Should()
            .Equal( "S1" );

        this.EventsFrom( () => v.S2 = 1 )
            .Should()
            .Equal( "S2" );

        this.EventsFrom( () => v.S3 = 1 )
            .Should()
            .Equal( "S3" );
    }

    private void Test( C2 v )
    {
        v.C2P1.Should().Be( 0 );

        this.Test( (Simple) v );

        this.EventsFrom( () => v.C2P1 = 1 )
            .Should()
            .Equal( "C2P1" );
    }

    private void Test( C3 v )
    {
        v.C3P1.Should().Be( 0 );

        this.Test( (C2) v );

        this.EventsFrom( () => v.C3P1 = 1 )
            .Should()
            .Equal( "C3P1" );
    }

    private void Test( C4 v )
    {
        v.C4P1.Should().Be( 0 );

        this.Test( (C3) v );

        this.EventsFrom( () => v.C4P1 = 1 )
            .Should()
            .Equal( "C4P1" );
    }

    private void Test( C5 v )
    {
        v.C5P1.Should().Be( 0 );

        this.Test( (C4) v );

        this.EventsFrom( () => v.C5P1 = 1 )
            .Should()
            .Equal( "C5P1" );
    }

    [Fact]
    public void Trivial()
    {
        this.Test( new C1() );
    }

    [Fact]
    public void Depth2()
    {
        this.Test( new C2() );
    }

    [Fact]
    public void Depth3()
    {
        this.Test( new C3() );
    }

    [Fact]
    public void Depth4()
    {
        this.Test( new C4() );
    }

    [Fact]
    public void Depth5()
    {
        this.Test( new C5() );
    }

    [Fact]
    public void RefToBaseProperty()
    {
        var v = new C6();

        v.S1.Should().Be( 0 );
        v.C6P1.Should().Be( 0 );
        v.S1 = -1;
        v.C6P1.Should().Be( -1 );

        this.SubscribeTo( v );

        this.EventsFrom( () => v.S1 = 1 )
            .Should()
            .Equal( "C6P1", "S1" );

        this.EventsFrom( () => v.S2 = 1 )
            .Should()
            .Equal( "S2" );

        this.EventsFrom( () => v.S3 = 1 )
            .Should()
            .Equal( "S3" );
    }

    [Fact]
    public void RefToBaseBaseProperty()
    {
        var v = new C7();

        v.S1.Should().Be( 0 );
        v.C6P1.Should().Be( 0 );
        v.C7P1.Should().Be( 0 );
        v.S1 = -1;
        v.C6P1.Should().Be( -1 );
        v.C7P1.Should().Be( -1 );

        this.SubscribeTo( v );

        this.EventsFrom( () => v.S1 = 1 )
            .Should()
            .Equal( "C7P1", "C6P1", "S1" );

        this.EventsFrom( () => v.S2 = 1 )
            .Should()
            .Equal( "S2" );

        this.EventsFrom( () => v.S3 = 1 )
            .Should()
            .Equal( "S3" );
    }

    [Fact]
    public void DerivedReferencesUnreferencedChildOfBaseInpcProperty()
    {
        var v = new C16();

        var sv = this.SubscribeTo( v );

        var a = new Simple();

        var sa = this.SubscribeTo( a );

        this.EventsFrom( () => v.R1 = a )
            .Should()
            .Equal( (sv, "A2"), (sv, "C16P1"), (sv, "R1") );

        this.EventsFrom( () => a.S1 = 1 )
            .Should()
            .Equal( (sa, "S1"), (sv, "A2") );

        this.EventsFrom( () => a.S2 = 1 )
            .Should()
            .Equal( (sa, "S2"), (sv, "C16P1") );

        this.EventsFrom( () => v.R2 = new Simple() )
            .Should()
            .Equal( (sv, "R2") );
    }

    [Fact]
    public void DerivedReferencesReferencedChildOfBaseInpcProperty()
    {
        var v = new C17();

        var sv = this.SubscribeTo( v );

        var a = new Simple();

        var sa = this.SubscribeTo( a );

        this.EventsFrom( () => v.R1 = a )
            .Should()
            .Equal( (sv, "A2"), (sv, "C17P1"), (sv, "R1") );

        this.EventsFrom( () => a.S1 = 1 )
            .Should()
            .Equal( (sa, "S1"), (sv, "A2"), (sv, "C17P1") );

        this.EventsFrom( () => a.S2 = 1 )
            .Should()
            .Equal( (sa, "S2") );

        this.EventsFrom( () => v.R2 = new Simple() )
            .Should()
            .Equal( (sv, "R2") );
    }

    [Fact]
    public void DerivedReferencesBaseInpcProperty()
    {
        var v = new C18();

        var sv = this.SubscribeTo( v );

        var a = new Simple();

        var sa = this.SubscribeTo( a );

        this.EventsFrom( () => v.R1 = a )
            .Should()
            .Equal( (sv, "A2"), (sv, "R1") );

        this.EventsFrom( () => a.S1 = 1 )
            .Should()
            .Equal( (sa, "S1"), (sv, "A2") );

        this.EventsFrom( () => a.S2 = 1 )
            .Should()
            .Equal( (sa, "S2") );

        var b = new Simple();

        var sb = this.SubscribeTo( b );

        this.EventsFrom( () => v.R2 = b )
            .Should()
            .Equal( (sv, "C18P1"), (sv, "R2") );

        this.EventsFrom( () => b.S2 = 1 )
            .Should()
            .Equal( (sb, "S2") );
    }

    [Fact]
    public void InheritFromExistingInpcImpl()
    {
        var v = new C8();

        var sv = this.SubscribeTo( v );

        this.EventsFrom( () => v.C8P1 = 1 )
            .Should()
            .Equal( "C8P1" );

        this.EventsFrom( () => v.EX1 = 3 )
            .Should()
            .Equal( "C8P2", "EX1" );

        v.C8P2.Should().Be( 6 );

        var a = new Simple();

        var sa = this.SubscribeTo( a );

        this.EventsFrom( () => v.EX2 = a )
            .Should()
            .Equal( (sv, "C8P3"), (sv, "EX2") );

        this.EventsFrom( () => a.S1 = 6 )
            .Should()
            .Equal( (sa, "S1"), (sv, "C8P3") );

        v.C8P3.Should().Be( 18 );

        this.EventsFrom( () => a.S2 = 1 )
            .Should()
            .Equal( (sa, "S2") );

        this.EventsFrom( () => a.S3 = 1 )
            .Should()
            .Equal( (sa, "S3") );
    }

    [Fact]
    public void InheritFromExistingInpcImplDepth2()
    {
        var v = new C9();

        var sv = this.SubscribeTo( v );

        this.EventsFrom( () => v.C8P1 = 1 )
            .Should()
            .Equal( "C9P1", "C8P1" );

        this.EventsFrom( () => v.EX1 = 3 )
            .Should()
            .Equal( "C8P2", "EX1" );

        v.C8P2.Should().Be( 6 );

        var a = new Simple();

        var sa = this.SubscribeTo( a );

        this.EventsFrom( () => v.EX2 = a )
            .Should()
            .Equal( (sv, "C9P2"), (sv, "C8P3"), (sv, "EX2") );

        this.EventsFrom( () => a.S1 = 6 )
            .Should()
            .Equal( (sa, "S1"), (sv, "C8P3") );

        v.C8P3.Should().Be( 18 );

        this.EventsFrom( () => a.S2 = 42 )
            .Should()
            .Equal( (sa, "S2"), (sv, "C9P2") );

        v.C9P2.Should().Be( 42 );

        this.EventsFrom( () => a.S3 = 1 )
            .Should()
            .Equal( (sa, "S3") );
    }

    [Fact]
    public void InheritFromAbstractBase()
    {
        var v = new C11();

        this.SubscribeTo( v );

        this.EventsFrom( () => v.C10P1 = 1 )
            .Should()
            .Equal( "C11P1", "C10P1" );
    }

    [Fact]
    public void InheritFromExistingAbstractInpcImpl()
    {
        var v = new C12();

        this.SubscribeTo( v );

        this.EventsFrom( () => v.EX1 = 1 )
            .Should()
            .Equal( "C12P1", "EX1" );
    }

    [Fact]
    public void UserThenNpcThenUserThenNpc()
    {
        var v = new C15();

        var sv = this.SubscribeTo( v );

        this.EventsFrom( () => v.EX1 = 1 )
            .Should()
            .Equal( "C15P3", "EX1" );

        this.EventsFrom( () => v.C13P1 = 1 )
            .Should()
            .Equal( "C15P1", "C13P1" );

        this.EventsFrom( () => v.C14P1 = 1 )
            .Should()
            .Equal( "C15P2", "C14P1" );

        var a = new Simple();

        var sa = this.SubscribeTo( a );

        this.EventsFrom( () => v.EX2 = a )
            .Should()
            .Equal( (sv, "C15P4"), (sv, "EX2") );

        this.EventsFrom( () => a.S1 = 1 )
            .Should()
            .Equal( (sa, "S1"), (sv, "C15P4") );
    }
}