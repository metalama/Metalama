// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.RunTime.Events;
using System;

namespace Metalama.Framework.Tests.UnitTests.RunTime;

#pragma warning disable CS0420, IDE0044

public partial class EventBrokerTests
{
    public delegate string? RefnessDelegate( in int inParam, out string? outParam, ref DateTime refParam );

    public class TestClassRefness
    {
        private readonly Action _onBrokerInvoke;

        private volatile EventBroker<RefnessDelegate, (int InParam, string? OutParam, DateTime RefParam, string? ReturnValue), TestClassRefness>? _broker;
        private RefnessDelegate? _originalEvent;

        public TestClassRefness( Action onBrokerInvoke )
        {
            this._onBrokerInvoke = onBrokerInvoke;

            var adapter = new DelegateEventAdapter<RefnessDelegate, (int InParam, string? OutParam, DateTime RefParam, string? ReturnValue), TestClassRefness>(
                ( h, ref args, i )
                    => i.OnEventViaBroker( h, ref args ),
                broker => ( in inParam, out outParam, ref refParam ) =>
                {
                    var args = (inParam, outParam: default(string?), refParam, ReturnValue: (string?) null);
                    broker.InvokeByRef( ref args );
                    outParam = args.outParam;

                    return args.ReturnValue;
                },
                ( h, i ) => i._originalEvent += h,
                ( h, i ) => i._originalEvent -= h );

            EventBroker.EnsureInitialized(
                ref this._broker,
                adapter,
                this );
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