// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.EventFields.AsMethod_Raise
{
    public class OverrideAttribute : EventAspect
    {
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.With( builder.Target.RaiseMethod ).Override( nameof( RaiseEventTemplate ) );
        }

        [Template]
        public void RaiseEventTemplate( EventHandler value )
        {
            Console.WriteLine( "Overridden raise" );
            meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public event EventHandler? Event;
    }
}