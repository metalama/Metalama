// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using FluentAssertions;
using Metalama.Patterns.Observability.UnitTests.Assets.Generic;
using Xunit;

namespace Metalama.Patterns.Observability.UnitTests;

public sealed class GenericTests : InpcTestsBase
{
    [Fact]
    public void PropertyOfGenericTypeThatIsClassAndInpc()
    {
        var v = new AOfSimple();

        this.SubscribeTo( v );

        this.EventsFrom( () => v.A1 = new() )
            .Should()
            .Equal( "RefA1S1", "A1" );

        this.EventsFrom( () => v.A1.S1 = 1 )
            .Should()
            .Equal( "RefA1S1" );
    }

    [Fact]
    public void PropertyOfGenericTypeThatIsClassButNotInpc()
    {
        var v = new B<string>();

        this.SubscribeTo( v );

        this.EventsFrom( () => v.B1 = "hello" )
            .Should()
            .Equal( "B1" );
    }

    [Fact]
    public void PropertyOfGenericTypeThatIsStructButNotInpc()
    {
        var v = new C<int>();

        this.SubscribeTo( v );

        this.EventsFrom( () => v.C1 = 123 )
            .Should()
            .Equal( "C1" );
    }

    [Fact]
    public void PropertyOfGenericTypeThatIsClassAndInpcAndIFoo()
    {
        var v = new D<MyFoo>();

        this.SubscribeTo( v );

        this.EventsFrom( () => v.D1 = new MyFoo() )
            .Should()
            .Equal( "FooX", "D1" );

        this.EventsFrom( () => v.D1.X = 1 )
            .Should()
            .Equal( "FooX" );

        this.EventsFrom( () => v.D1.Y = 1 )
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void PropertyOfGenericTypeThatIsClassAndInpcAndIFooDepth2()
    {
        var v = new DD<MyFoo>();

        this.SubscribeTo( v );

        this.EventsFrom( () => v.D1 = new() )
            .Should()
            .Equal( "FooX", "FooY", "D1" );

        this.EventsFrom( () => v.D1.X = 1 )
            .Should()
            .Equal( "FooX" );

        this.EventsFrom( () => v.D1.Y = 1 )
            .Should()
            .Equal( "FooY" );
    }
}