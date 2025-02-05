using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Layers
{
    [Layers( "Second" )]
    internal class MyAspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            builder.Override( nameof(OverrideMethod), args: new { layerName = builder.Layer } );
        }

        [Template]
        public dynamic? OverrideMethod( [CompileTime] string? layerName )
        {
            Console.WriteLine( "Layer: " + layerName );

            return meta.Proceed();
        }
    }

    // <target>
    internal class C
    {
        [MyAspect]
        public void M() { }
    }
}