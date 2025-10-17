// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.RunTime.Events;
using System;

namespace Metalama.Framework.Tests.UnitTests.RunTime;

public partial class EventBrokerTests
{
    // TODO: Make compatible with static types.
    public class TestClassStatic
    {
#pragma warning disable IDE0044
        private static volatile EventBroker<EventHandler, TestClassStatic?, (object? Sender, EventArgs Args)>? _broker;
#pragma warning restore IDE0044
        private static EventHandler? _originalEvent;

        static TestClassStatic()
        {
            EventBroker<EventHandler, TestClassStatic?, (object? Sender, EventArgs Args)>.EnsureInitialized(
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
                ref _broker,
#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
                null,
                new DelegateEventAdapter<EventHandler, TestClassStatic?, (object? Sender, EventArgs Args)>(
                    ( EventHandler h, TestClassStatic? i, ref (object? Sender, EventArgs Args) args ) => OnEventViaBroker( h, args ),
                    broker => ( sender, args ) => broker.Invoke( (sender, args) ),
                    ( h, _ ) => _originalEvent += h,
                    ( h, _ ) => _originalEvent -= h ) );
        }

        public static event EventHandler Event
        {
            add => _broker?.AddHandler( value );
            remove => _broker?.RemoveHandler( value );
        }

        public static void OnEvent( EventArgs args ) => _originalEvent?.Invoke( null, args );

        private static void OnEventViaBroker( EventHandler handler, (object? Sender, EventArgs Args) args )
        {
            handler.Invoke( args.Sender, args.Args );
        }
    }
}