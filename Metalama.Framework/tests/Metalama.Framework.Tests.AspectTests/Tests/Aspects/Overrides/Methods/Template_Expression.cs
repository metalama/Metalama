using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.Template_Expression
{
    internal class TestAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod() => default;
    }

    // <target>
    public class Target
    {
        [Test]
        public void VoidMethod()
        {
            Console.WriteLine( "Original" );
        }

        [Test]
        public int Method()
        {
            Console.WriteLine( "Original" );

            return 42;
        }

        [Test]
        public T? Method<T>()
        {
            Console.WriteLine( "Original" );

            return default;
        }
    }
}