// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using FluentAssertions;
using Metalama.Patterns.Observability.UnitTests.Assets.Core;
using Metalama.Patterns.Observability.UnitTests.Assets.IgnoreAutoChangePropertyAttribute;
using Xunit;

namespace Metalama.Patterns.Observability.UnitTests;

public sealed class IgnoreAutoChangeNotificationAttributeTests : InpcTestsBase
{
    [Fact]
    public void ChangeIsIgnored()
    {
        var v = new SimpleWithIgnoreAutoChangeProperty();

        this.SubscribeTo( v );

        this.EventsFrom( () => v.P1 = 1 )
            .Should()
            .Equal( "P1" );

        this.EventsFrom( () => v.P2 = 1 )
            .Should()
            .BeEmpty();

        this.EventsFrom( () => v.P3 = new Simple() )
            .Should()
            .BeEmpty();

        this.EventsFrom( () => v.P3.S1 = 1 )
            .Should()
            .BeEmpty();
    }
}