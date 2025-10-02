// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.RunTime;
using System;
using Xunit;

// ReSharper disable ConvertToLocalFunction

#pragma warning disable IDE0039 // Use local function

namespace Metalama.Framework.Tests.UnitTests.RunTime;

public class ActionEventBrokerTests
{
    [Fact]
    public void AddDelegate()
    {
        var brokerInvocations = 0;
        var test = new TestClass( () => brokerInvocations++ );

        var handlerInvocations = 0;
        EventHandler handler = ( _, _ ) => handlerInvocations++;

        test.Event += handler;

        test.OnEvent( EventArgs.Empty );

        Assert.Equal( 1, brokerInvocations );
        Assert.Equal( 1, handlerInvocations );
    }

    [Fact]
    public void AddTwoDelegates()
    {
        var brokerInvocations = 0;
        var test = new TestClass( () => brokerInvocations++ );

        var handler1Invocations = 0;
        var handler2Invocations = 0;
        EventHandler handler1 = ( _, _ ) => handler1Invocations++;
        EventHandler handler2 = ( _, _ ) => handler2Invocations++;

        test.Event += handler1;
        test.Event += handler2;

        test.OnEvent( EventArgs.Empty );

        Assert.Equal( 2, brokerInvocations );
        Assert.Equal( 1, handler1Invocations );
        Assert.Equal( 1, handler2Invocations );
    }

    [Fact]
    public void AddOneDelegateTwice()
    {
        var brokerInvocations = 0;
        var test = new TestClass( () => brokerInvocations++ );

        var handlerInvocations = 0;
        EventHandler handler = ( _, _ ) => handlerInvocations++;

        test.Event += handler;
        test.Event += handler;

        test.OnEvent( EventArgs.Empty );

        Assert.Equal( 2, brokerInvocations );
        Assert.Equal( 2, handlerInvocations );
    }

    [Fact]
    public void AddAndRemoveSameDelegate()
    {
        var brokerInvocations = 0;
        var test = new TestClass( () => brokerInvocations++ );

        var handlerInvocations = 0;
        EventHandler handler = ( _, _ ) => handlerInvocations++;

        test.Event += handler;
        test.Event -= handler;

        test.OnEvent( EventArgs.Empty );

        Assert.Equal( 0, brokerInvocations );
        Assert.Equal( 0, handlerInvocations );
    }

    [Fact]
    public void AddTwoDelegatesAndRemoveOne()
    {
        var brokerInvocations = 0;
        var test = new TestClass( () => brokerInvocations++ );

        var handler1Invocations = 0;
        var handler2Invocations = 0;
        EventHandler handler1 = ( _, _ ) => handler1Invocations++;
        EventHandler handler2 = ( _, _ ) => handler2Invocations++;

        test.Event += handler1;
        test.Event += handler2;
        test.Event -= handler1;

        test.OnEvent( EventArgs.Empty );

        Assert.Equal( 1, brokerInvocations );
        Assert.Equal( 0, handler1Invocations );
        Assert.Equal( 1, handler2Invocations );
    }

    [Fact]
    public void AddSameDelegateTwiceAndRemoveOnce()
    {
        var brokerInvocations = 0;
        var test = new TestClass( () => brokerInvocations++ );

        var handlerInvocations = 0;
        EventHandler handler = ( _, _ ) => handlerInvocations++;

        test.Event += handler;
        test.Event += handler;
        test.Event -= handler;

        test.OnEvent( EventArgs.Empty );

        Assert.Equal( 1, brokerInvocations );
        Assert.Equal( 1, handlerInvocations );
    }

    [Fact]
    public void RemoveDifferentDelegate()
    {
        var brokerInvocations = 0;
        var test = new TestClass( () => brokerInvocations++ );

        var registeredHandlerInvocations = 0;
        var unregisteredHandlerInvocations = 0;
        EventHandler registeredHandler = ( _, _ ) => registeredHandlerInvocations++;
        EventHandler unregisteredHandler = ( _, _ ) => unregisteredHandlerInvocations++;

        test.Event += registeredHandler;
        test.Event -= unregisteredHandler; // Try to remove a different delegate

        test.OnEvent( EventArgs.Empty );

        Assert.Equal( 1, brokerInvocations );
        Assert.Equal( 1, registeredHandlerInvocations );
        Assert.Equal( 0, unregisteredHandlerInvocations );
    }

    [Fact]
    public void AddMulticastDelegate()
    {
        var brokerInvocations = 0;
        var test = new TestClass( () => brokerInvocations++ );

        var handler1Invocations = 0;
        var handler2Invocations = 0;
        var handler3Invocations = 0;
        EventHandler handler1 = ( _, _ ) => handler1Invocations++;
        EventHandler handler2 = ( _, _ ) => handler2Invocations++;
        EventHandler handler3 = ( _, _ ) => handler3Invocations++;

        var multicastHandler = handler1 + handler2 + handler3;

        test.Event += multicastHandler;

        test.OnEvent( EventArgs.Empty );

        Assert.Equal( 3, brokerInvocations );

        Assert.Equal( 1, handler1Invocations );
        Assert.Equal( 1, handler2Invocations );
        Assert.Equal( 1, handler3Invocations );
    }

    public class TestClass
    {
        private readonly Action _onBrokerInvoke;

#pragma warning disable IDE0044 // Add readonly modifier
        private volatile ActionEventBroker<EventHandler, (object? Sender, EventArgs Args)>? _broker;
#pragma warning restore IDE0044 // Add readonly modifier
        private EventHandler? _originalEvent;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public TestClass( Action onBrokerInvoke )
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            this._onBrokerInvoke = onBrokerInvoke;

#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
            ActionEventBroker<EventHandler, (object? Sender, EventArgs Args)>.EnsureInitialized(
                ref this._broker,
                this,
                new ActionEventBrokerCallbacks<EventHandler, (object? Sender, EventArgs Args)>(
                    ( h, i, args ) => ((TestClass) i).OnEventViaBroker( h, args ),
                    broker => ( sender, args ) => broker.Invoke( (sender, args) ),
                    ( h, i ) => ((TestClass) i)._originalEvent += h,
                    ( h, i ) => ((TestClass) i)._originalEvent -= h ) );
#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
        }

        public event EventHandler Event
        {
            add => this._broker?.AddHandler( value );
            remove => this._broker?.RemoveHandler( value );
        }

        public void OnEvent( EventArgs args ) => this._originalEvent?.Invoke( this, args );

        private void OnEventViaBroker( EventHandler handler, (object? Sender, EventArgs Args) args )
        {
            this._onBrokerInvoke();
            handler.Invoke( args.Sender, args.Args );
        }
    }
}