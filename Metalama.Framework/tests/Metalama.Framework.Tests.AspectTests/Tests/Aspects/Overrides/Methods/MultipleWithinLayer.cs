using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.MultipleWithinLayer
{
    // Tests single OverrideMethod aspect with trivial template on methods with trivial bodies.

    [Layers( "first", "second" )]
    public class OverrideAttribute : MethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            switch (builder.Layer)
            {
                case null:
                    builder.Override( nameof(OverrideMethod), args: new { layer = "default", order = 1 } );
                    builder.Override( nameof(OverrideMethod), args: new { layer = "default", order = 2 } );
                    builder.Override( nameof(OverrideMethod), args: new { layer = "default", order = 3 } );

                    break;

                case "first":
                    builder.Override( nameof(OverrideMethod), args: new { layer = "first", order = 1 } );
                    builder.Override( nameof(OverrideMethod), args: new { layer = "first", order = 2 } );
                    builder.Override( nameof(OverrideMethod), args: new { layer = "first", order = 3 } );

                    break;

                case "second":
                    builder.Override( nameof(OverrideMethod), args: new { layer = "second", order = 1 } );
                    builder.Override( nameof(OverrideMethod), args: new { layer = "second", order = 2 } );
                    builder.Override( nameof(OverrideMethod), args: new { layer = "second", order = 3 } );

                    break;
            }
        }

        [Template]
        public dynamic? OverrideMethod( [CompileTime] string layer, [CompileTime] int order )
        {
            Console.WriteLine( $"This is the overriding layer '{layer}', order {order}." );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public void TargetMethod()
        {
            Console.WriteLine( "This is the original method." );
        }
    }
}