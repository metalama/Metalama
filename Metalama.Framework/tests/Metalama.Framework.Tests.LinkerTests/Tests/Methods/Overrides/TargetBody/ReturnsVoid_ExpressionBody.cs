using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TargetBody.ReturnsVoid_ExpressionBody
{
    // <target>
    class Target
    {
        void Foo() => Console.WriteLine( "Original");

        [PseudoOverride( nameof(Foo), "TestAspect")]

        void Foo_Override()
        {
            Console.WriteLine("Aspect");
            link(_this.Foo, inline)();
        }
    }
}
