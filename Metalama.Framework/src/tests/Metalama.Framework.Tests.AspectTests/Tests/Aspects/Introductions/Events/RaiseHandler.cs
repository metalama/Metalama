// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.RaiseHandler
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceEvent(
                "EventFromAccessors",
                nameof( AddEventTemplate ),
                nameof( RemoveEventTemplate ),
                nameof( InvokeEventTemplate ),
                buildEvent: e => e.Accessibility = Accessibility.Public );
        }

        [Template]
        public void AddEventTemplate( EventHandler value )
        {
            Console.WriteLine( "Add" );
            meta.Proceed();
        }

        [Template]
        public void RemoveEventTemplate( EventHandler value )
        {
            Console.WriteLine( "Remove" );
            meta.Proceed();
        }

        [Template]
        public void InvokeEventTemplate( EventHandler value )
        {
            Console.WriteLine( "Remove" );
            meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}