// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;
using Xunit;

// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToLocalFunction

#pragma warning disable IDE0039, IDE0044

namespace Metalama.Framework.Tests.UnitTests.RunTime;

public partial class EventBrokerTests
{
    [Fact]
    public void AddDelegate()
    {
        var brokerInvocations = 0;
        var test = new TestClassEventHandler( () => brokerInvocations++ );

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
        var test = new TestClassEventHandler( () => brokerInvocations++ );

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
        var test = new TestClassEventHandler( () => brokerInvocations++ );

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
        var test = new TestClassEventHandler( () => brokerInvocations++ );

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
        var test = new TestClassEventHandler( () => brokerInvocations++ );

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
        var test = new TestClassEventHandler( () => brokerInvocations++ );

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
        var test = new TestClassEventHandler( () => brokerInvocations++ );

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
        var test = new TestClassEventHandler( () => brokerInvocations++ );

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

    [Fact]
    public void Func()
    {
        // This proofs the concept that we can use EventBroker with non-void delegates.

        var brokerInvocations = 0;
        var test = new TestClassFunc( () => brokerInvocations++ );
        test.Event += i => i.ToString( CultureInfo.InvariantCulture );
        var result = test.OnEvent( 5 );

        Assert.Equal( "5", result );
        Assert.Equal( 1, brokerInvocations );
    }

    [Fact]
    public void Refness()
    {
        // This proofs the concept that we can use EventBroker with out and ref parameters.

        var brokerInvocations = 0;
        var test = new TestClassRefness( () => brokerInvocations++ );

        test.Event += ( in param, out outParam, ref refParam ) =>
        {
            outParam = $"Ciao {param}";
            refParam = DateTime.Now;

            return "Basta";
        };

        var dt = DateTime.MinValue;
        var result = test.OnEvent( 5, out var o, ref dt );

        Assert.Equal( 1, brokerInvocations );
        Assert.Equal( "Basta", result );
        Assert.Equal( "Ciao 5", o );
    }
}