// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Extensibility;

/// <summary>
/// Tests of <see cref="EventDispatcher"/>: ordered asynchronous delivery, isolation of the subscribers, and the
/// observer.
/// </summary>
public sealed class EventDispatcherTests : TestsBase
{
    public EventDispatcherTests( ITestOutputHelper logger ) : base( logger ) { }

    private sealed record TestEvent( int Number );

    private sealed record UnobservedEvent;

    [Fact]
    public async Task EventsAreDeliveredInTheOrderOfPublication()
    {
        var received = new List<int>();
        using var subscription = this.EventDispatcher.Subscribe<TestEvent>( e => received.Add( e.Number ) );

        for ( var i = 0; i < 200; i++ )
        {
            this.EventDispatcher.Publish( new TestEvent( i ) );
        }

        await this.DrainEventsAsync();

        Assert.Equal( Enumerable.Range( 0, 200 ), received );
        Assert.Empty( this.EventObserver.Failures );
        Assert.Empty( this.EventObserver.UnhandledEvents );
    }

    [Fact]
    public async Task SubscriberIsNotInvokedOnTheStackOfThePublisher()
    {
        var invokedSynchronously = false;
        var invoked = false;
        using var subscription = this.EventDispatcher.Subscribe<TestEvent>( _ => invoked = true );

        this.EventDispatcher.Publish( new TestEvent( 1 ) );
        invokedSynchronously = invoked;

        await this.DrainEventsAsync();

        Assert.False( invokedSynchronously );
        Assert.True( invoked );
    }

    [Fact]
    public async Task FailingSubscriberIsReportedAndDoesNotStopDelivery()
    {
        var received = new List<int>();

        using var failing = this.EventDispatcher.Subscribe<TestEvent>( e => throw new InvalidOperationException( $"Subscriber failed on {e.Number}." ) );
        using var succeeding = this.EventDispatcher.Subscribe<TestEvent>( e => received.Add( e.Number ) );

        this.EventDispatcher.Publish( new TestEvent( 1 ) );
        this.EventDispatcher.Publish( new TestEvent( 2 ) );

        await this.DrainEventsAsync();

        Assert.Equal( [1, 2], received );
        Assert.Equal( 2, this.EventObserver.Failures.Count );
        Assert.All( this.EventObserver.Failures, f => Assert.IsType<InvalidOperationException>( f.Exception ) );
    }

    [Fact]
    public async Task UnhandledEventIsReported()
    {
        this.EventDispatcher.Publish( new UnobservedEvent() );

        await this.DrainEventsAsync();

        var unhandled = Assert.Single( this.EventObserver.UnhandledEvents );
        Assert.IsType<UnobservedEvent>( unhandled );
    }

    [Fact]
    public async Task DisposedSubscriptionNoLongerReceives()
    {
        var received = 0;
        var subscription = this.EventDispatcher.Subscribe<TestEvent>( _ => received++ );

        this.EventDispatcher.Publish( new TestEvent( 1 ) );
        await this.DrainEventsAsync();

        subscription.Dispose();

        this.EventDispatcher.Publish( new TestEvent( 2 ) );
        await this.DrainEventsAsync();

        Assert.Equal( 1, received );
        Assert.Single( this.EventObserver.UnhandledEvents );
    }
}
