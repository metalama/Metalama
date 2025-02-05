using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.MethodOverride;

#pragma warning disable CS0162

[assembly: AspectOrder(AspectOrderDirection.RunTime, typeof(Aspect1), typeof(Aspect2))]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.MethodOverride
{
    public class Aspect1 : MethodAspect
    {
        public override void BuildAspect(IAspectBuilder<IMethod> builder)
        {
            builder.Advice.Override(builder.Target, nameof(Override));
        }

        [Template]
        public dynamic? Override()
        {
            meta.InsertComment("Comment before Aspect1.");
            Console.WriteLine(nameof(Aspect1));
            meta.InsertComment("Comment mid Aspect1.");

            return meta.Proceed();

            meta.InsertComment("Comment after Aspect1.");
        }
    }

    public class Aspect2 : MethodAspect
    {
        public override void BuildAspect(IAspectBuilder<IMethod> builder)
        {
            builder.Advice.Override(builder.Target, nameof(Override));
        }

        [Template]
        public dynamic? Override()
        {
            meta.InsertComment("Comment before Aspect2.");
            Console.WriteLine(nameof(Aspect2));
            meta.InsertComment("Comment mid Aspect2.");

            return meta.Proceed();

            meta.InsertComment("Comment after Aspect2.");
        }
    }

    // <target>
    public class Target
    {
        // Comment before Foo.
        [Aspect1]
        [Aspect2]
        public void Foo()
        // Comment before Foo opening brace.
        { // Comment after Foo opening brace.
            // Comment inside Foo 1.
            Console.WriteLine("Foo"); // Comment inside Foo 2.
            // Comment before Foo closing brace.
        } // Comment after Foo closing brace.
        // Comment after Foo.

        // Comment before Bar.
        [Aspect1]
        [Aspect2]
        public int Bar()
        // Comment before Bar opening brace.
        { // Comment after Bar opening brace.
            // Comment inside Bar 1.
            Console.WriteLine("Bar"); // Comment inside Bar 2.
            // Comment inside Bar 3.
            return 42; // Comment inside Bar 4.
            // Comment before Bar closing brace.
        } // Comment after Bar closing brace.
        // Comment after Bar.
    }
}
