// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Events.NoProceed
{
    [AttributeUsage( AttributeTargets.Event, AllowMultiple = true )]
    public class OverrideAttribute : EventAspect
    {
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.OverrideAccessors( nameof(AccessorTemplate), nameof(AccessorTemplate), null );
        }

        [Template]
        public void AccessorTemplate()
        {
            Console.WriteLine( "This is the overridden accessor." );
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public event EventHandler Event
        {
            add
            {
                Console.WriteLine( "This is the original accessor." );
            }

            remove
            {
                Console.WriteLine( "This is the original accessor." );
            }
        }
    }
}