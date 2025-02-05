using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AspectState
{
    [Layers( "Second" )]
    internal class MyAspect : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            switch (builder.Layer)
            {
                case null:
                    builder.AspectState = new State { Value = 5 };

                    break;

                case "Second":
                    builder.Override( nameof(OverrideMethod), args: new { value = ( (State)builder.AspectState! ).Value } );

                    break;
            }
        }

        [Template]
        public dynamic? OverrideMethod( [CompileTime] int value )
        {
            Console.WriteLine( value );

            return meta.Proceed();
        }

        private class State : IAspectState
        {
            public int Value { get; set; }
        }
    }

    // <target>
    internal class C
    {
        [MyAspect]
        public void M() { }
    }
}