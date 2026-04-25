// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.RunTime.Events;
using System;

namespace Metalama.Framework.Tests.UnitTests.RunTime;

#pragma warning disable CS0420, IDE0044

public sealed partial class EventBrokerTests
{
    public sealed class TestClassFunc
    {
        private readonly Action _onBrokerInvoke;

        private volatile EventBroker<Func<int, string?>, (int Input, string? ReturnValue), TestClassFunc>? _broker;
        private Func<int, string?>? _originalEvent;

        public TestClassFunc( Action onBrokerInvoke )
        {
            this._onBrokerInvoke = onBrokerInvoke;

            var adapter = new DelegateEventAdapter<Func<int, string?>, (int Input, string? ReturnValue), TestClassFunc>(
                ( h, ref args, i ) => i.OnEventViaBroker( h, ref args ),
                broker => input =>
                {
                    var args = (input, ReturnValue: (string?) null);
                    broker.InvokeByRef( ref args );

                    return args.ReturnValue;
                },
                ( h, i ) => i._originalEvent += h,
                ( h, i ) => i._originalEvent -= h );

            EventBroker.EnsureInitialized(
                ref this._broker,
                adapter,
                this );
        }

        public event Func<int, string?> Event
        {
            add => this._broker?.AddHandler( value );
            remove => this._broker?.RemoveHandler( value );
        }

        public string? OnEvent( int input ) => this._originalEvent?.Invoke( input );

        private void OnEventViaBroker( Func<int, string?> handler, ref (int Input, string? ReturnValue) args )
        {
            this._onBrokerInvoke();
            args.ReturnValue = handler.Invoke( args.Input );
        }
    }
}