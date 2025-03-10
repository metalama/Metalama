// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Layers.Introductions
{
    [Layers( "Second" )]
    internal class MyAspect : TypeAspect
    {
        [Introduce]
        public void IntroducedInFirstLayer() { }

        [Introduce( Layer = "Second" )]
        public void IntroducedInSecondLayer() { }

        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var method in builder.Target.Methods)
            {
                builder.With( method ).Override( nameof(OverrideMethod), args: new { layerName = builder.Layer ?? "First" } );
            }
        }

        [Template]
        public dynamic? OverrideMethod( [CompileTime] string? layerName )
        {
            Console.WriteLine( "Overridden in Layer " + layerName );

            return meta.Proceed();
        }
    }

    // <target>
    [MyAspect]
    internal class C
    {
        public void InSourceCode() { }
    }
}