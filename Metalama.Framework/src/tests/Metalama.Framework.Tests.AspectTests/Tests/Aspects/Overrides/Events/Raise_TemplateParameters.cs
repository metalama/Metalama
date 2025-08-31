// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_TemplateParameters
{
    public class OverrideAttribute : EventAspect
    {
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            if (builder.Target.Name == "Event1")
                builder.OverrideAccessors( null, null, nameof( InvokeEventTemplate1 ) );
            else
                builder.OverrideAccessors( null, null, nameof( InvokeEventTemplate2 ) );
        }

        [Template]
        public void InvokeEventTemplate1(EventHandler handler)
        {
            Console.WriteLine( "Invoke" );
            handler?.Invoke( meta.This, EventArgs.Empty );
        }

        [Template]
        public void InvokeEventTemplate2( EventHandler handler, object sender, EventArgs args )
        {
            Console.WriteLine( "Invoke" );
            Console.WriteLine( $"{sender}" );
            Console.WriteLine( $"{args}" );
        }
    }

    // <target>
    internal class TargetClass 
    {
        private EventHandler? _handler1;
        private EventHandler? _handler2;

        [Override]
        public event EventHandler Event1
        {
            add
            {
                this._handler1 = value;
            }

            remove
            {
                this._handler1 = null;
            }
        }

        [Override]
        public event EventHandler Event2
        {
            add
            {
                this._handler2 = value;
            }

            remove
            {
                this._handler2 = null;
            }
        }
    }
}