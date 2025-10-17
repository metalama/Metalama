// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.RunTime.Events;
using System;

namespace Metalama.Framework.Tests.UnitTests.RunTime;

public partial class EventBrokerTests
{
    public class TestClassFunc
    {
        private readonly Action _onBrokerInvoke;

#pragma warning disable IDE0044
        private volatile EventBroker<Func<int, string?>, TestClassFunc, (int Input, string? ReturnValue)>? _broker;
#pragma warning restore IDE0044
        private Func<int, string?>? _originalEvent;

        public TestClassFunc( Action onBrokerInvoke )
        {
            this._onBrokerInvoke = onBrokerInvoke;

            EventBroker<Func<int, string?>, TestClassFunc, (int Input, string? ReturnValue)>.EnsureInitialized(
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
                ref this._broker,
#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
                this,
                new DelegateEventAdapter<Func<int, string?>, TestClassFunc, (int Input, string? ReturnValue)>(
                    ( Func<int, string?> h, TestClassFunc i, ref (int Input, string? ReturnValue) args ) => i.OnEventViaBroker( h, ref args ),
                    broker => input =>
                    {
                        var args = (input, ReturnValue: (string?) null);
                        broker.InvokeByRef( ref args );

                        return args.ReturnValue;
                    },
                    ( h, i ) => i._originalEvent += h,
                    ( h, i ) => i._originalEvent -= h ) );
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