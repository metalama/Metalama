// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.TypeFromTemplate
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceEvent(
                "IntroducedEvent",
                nameof(Template),
                nameof(Template),
                args: new { x = "42" } );
        }

        [Template]
        public void Template( [CompileTime] string x, EventHandler y )
        {
            y.Invoke( null, new EventArgs() );
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}