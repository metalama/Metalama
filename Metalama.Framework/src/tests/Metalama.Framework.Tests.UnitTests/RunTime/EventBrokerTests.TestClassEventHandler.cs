// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.RunTime.Events;
using System;

namespace Metalama.Framework.Tests.UnitTests.RunTime;

public partial class EventBrokerTests
{
    public class TestClassEventHandler
    {
        private readonly Action _onBrokerInvoke;

#pragma warning disable IDE0044
        private volatile EventBroker<EventHandler, TestClassEventHandler, (object? Sender, EventArgs Args)>? _broker;
#pragma warning restore IDE0044
        private EventHandler? _originalEvent;

        public TestClassEventHandler( Action onBrokerInvoke )
        {
            this._onBrokerInvoke = onBrokerInvoke;

            EventBroker<EventHandler, TestClassEventHandler, (object? Sender, EventArgs Args)>.EnsureInitialized(
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
                ref this._broker,
#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
                this,
                new DelegateEventAdapter<EventHandler, TestClassEventHandler, (object? Sender, EventArgs Args)>(
                    ( EventHandler h, TestClassEventHandler i, ref (object? Sender, EventArgs Args) args ) => i.OnEventViaBroker( h, args ),
                    broker => ( sender, args ) => broker.Invoke( (sender, args) ),
                    ( h, i ) => i._originalEvent += h,
                    ( h, i ) => i._originalEvent -= h ) );
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