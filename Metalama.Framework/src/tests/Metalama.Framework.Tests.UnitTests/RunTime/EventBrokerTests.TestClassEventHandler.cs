// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.RunTime.Events;
using System;

namespace Metalama.Framework.Tests.UnitTests.RunTime;

#pragma warning disable CS0420, IDE0044

public partial class EventBrokerTests
{
    public class TestClassEventHandler
    {
        private readonly Action _onBrokerInvoke;

        private volatile EventBroker<EventHandler, (object? Sender, EventArgs Args), TestClassEventHandler>? _broker;
        private EventHandler? _originalEvent;

        public TestClassEventHandler( Action onBrokerInvoke )
        {
            this._onBrokerInvoke = onBrokerInvoke;

            var adapter = new DelegateEventAdapter<EventHandler, (object? Sender, EventArgs Args), TestClassEventHandler>(
                ( EventHandler h, ref (object? Sender, EventArgs Args) args, TestClassEventHandler i ) => i.OnEventViaBroker( h, args ),
                broker => ( sender, args ) => broker.Invoke( (sender, args) ),
                ( h, i ) => i._originalEvent += h,
                ( h, i ) => i._originalEvent -= h );

            EventBroker.EnsureInitialized(
                ref this._broker,
                adapter,
                this );
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