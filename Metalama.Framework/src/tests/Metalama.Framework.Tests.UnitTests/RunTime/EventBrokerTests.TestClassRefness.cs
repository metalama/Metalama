// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.RunTime.Events;
using System;

namespace Metalama.Framework.Tests.UnitTests.RunTime;

public partial class EventBrokerTests
{
    public delegate string? RefnessDelegate( in int inParam, out string? outParam, ref DateTime refParam );

    public class TestClassRefness
    {
        private readonly Action _onBrokerInvoke;

#pragma warning disable IDE0044
        private volatile EventBroker<RefnessDelegate, TestClassRefness, (int InParam, string? OutParam, DateTime RefParam, string? ReturnValue)>? _broker;
#pragma warning restore IDE0044
        private RefnessDelegate? _originalEvent;

        public TestClassRefness( Action onBrokerInvoke )
        {
            this._onBrokerInvoke = onBrokerInvoke;

            EventBroker<RefnessDelegate, TestClassRefness, (int InParam, string? OutParam, DateTime RefParam, string? ReturnValue)>.EnsureInitialized(
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
                ref this._broker,
#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
                this,
                new DelegateEventAdapter<RefnessDelegate, TestClassRefness, (int InParam, string? OutParam, DateTime RefParam, string? ReturnValue)>(
                    ( RefnessDelegate h, TestClassRefness i, ref (int InParam, string? OutParam, DateTime RefParam, string? ReturnValue) args )
                        => i.OnEventViaBroker( h, ref args ),
                    broker => ( in int inParam, out string? outParam, ref DateTime refParam ) =>
                    {
                        var args = (inParam, outParam: default(string?), refParam, ReturnValue: (string?) null);
                        broker.InvokeByRef( ref args );
                        outParam = args.outParam;

                        return args.ReturnValue;
                    },
                    ( h, i ) => i._originalEvent += h,
                    ( h, i ) => i._originalEvent -= h ) );
        }

        public event RefnessDelegate Event
        {
            add => this._broker?.AddHandler( value );
            remove => this._broker?.RemoveHandler( value );
        }

        public string? OnEvent( in int inParam, out string? outParam, ref DateTime refParam )
        {
            if ( this._originalEvent != null )
            {
                return this._originalEvent.Invoke( in inParam, out outParam, ref refParam );
            }
            else
            {
                outParam = null;

                return null;
            }
        }

        private void OnEventViaBroker( RefnessDelegate handler, ref (int InParam, string? OutParam, DateTime RefParam, string? ReturnValue) args )
        {
            this._onBrokerInvoke();
            args.ReturnValue = handler.Invoke( in args.InParam, out args.OutParam, ref args.RefParam );
        }
    }
}