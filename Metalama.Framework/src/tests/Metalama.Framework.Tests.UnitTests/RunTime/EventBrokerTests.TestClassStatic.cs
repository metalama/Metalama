// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.RunTime.Events;
using System;

namespace Metalama.Framework.Tests.UnitTests.RunTime;

#pragma warning disable CS0420, IDE0044

public partial class EventBrokerTests
{
    public static class TestClassStatic
    {
        private static volatile EventBroker<EventHandler, (object? Sender, EventArgs Args), None>? _broker;
        private static EventHandler? _originalEvent;

        static TestClassStatic()
        {
            var adapter = new DelegateEventAdapter<EventHandler, (object? Sender, EventArgs Args), None>(
                ( h, ref args, _ ) => OnEventViaBroker( h, args ),
                broker => ( sender, args ) => broker.Invoke( (sender, args) ),
                ( h, _ ) => _originalEvent += h,
                ( h, _ ) => _originalEvent -= h );

            EventBroker.EnsureInitialized( ref _broker, adapter );
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