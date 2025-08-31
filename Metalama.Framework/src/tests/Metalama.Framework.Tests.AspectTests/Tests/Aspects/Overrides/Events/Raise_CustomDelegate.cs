// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.Raise_CustomDelegate
{
    public delegate void MyEventHandler( object sender, int args1, int arg2 );

    public class OverrideAttribute : EventAspect
    {
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.OverrideAccessors( null, null, nameof( InvokeEventTemplate ));
        }

        [Template]
        public void InvokeEventTemplate()
        {
            Console.WriteLine( "Invoke" );
            meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass 
    {
        private MyEventHandler? _handler;

        [Override]
        public event MyEventHandler Event
        {
            add
            {
                this._handler = value;
            }

            remove
            {
                this._handler = null;
            }
        }
    }
}