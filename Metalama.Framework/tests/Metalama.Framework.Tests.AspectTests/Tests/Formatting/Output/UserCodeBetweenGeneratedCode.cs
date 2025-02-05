using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.UserCodeBetweenGeneratedCode
{
    internal class LogAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine($"Invoking {meta.Target.Method.ToDisplayString()}");

            return meta.Proceed();
        }
    }

    internal class Foo
    {
        [Log]
        public void Method()
        {
            Console.WriteLine("InstanceMethod");
        }

        [Log]
        public void ExpressionMethod() => Console.WriteLine("InstanceMethod");
    }
}
