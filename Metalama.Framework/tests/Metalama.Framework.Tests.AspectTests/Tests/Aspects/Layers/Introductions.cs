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