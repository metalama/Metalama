// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.AsMethod_Add
{
    public class OverrideAttribute : EventAspect
    {
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.With( builder.Target.AddMethod ).Override( nameof( AddEventTemplate ) );
        }

        [Template]
        public void AddEventTemplate( EventHandler value )
        {
            Console.WriteLine( "Overridden add" );
            meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        private EventHandler? _handler;

        [Override]
        public event EventHandler Event
        {
            add
            {
                this._handler += value;
                Console.WriteLine( "Original add" );
            }

            remove
            {
                this._handler -= value;
                Console.WriteLine( "Original remove" );
            }
        }
    }
}