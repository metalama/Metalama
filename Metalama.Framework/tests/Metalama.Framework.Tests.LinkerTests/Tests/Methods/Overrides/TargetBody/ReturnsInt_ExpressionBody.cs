using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TargetBody.ReturnsInt_ExpressionBody
{
    // <target>
    class Target
    {
        int Foo() => 42;

        [PseudoOverride( nameof(Foo), "TestAspect")]

        int Foo_Override()
        {
            Console.WriteLine("Aspect");
            return link(_this.Foo, inline)();
        }
    }
}
