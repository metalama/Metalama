using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.CrossAssemblyChildAspect
{
    public class C : I
    {
        public void M()
        {
            Console.WriteLine( "Hello, world." );
        }
    }
}