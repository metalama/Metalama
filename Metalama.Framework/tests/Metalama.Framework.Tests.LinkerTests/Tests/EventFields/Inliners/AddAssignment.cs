using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.LinkerTests.Tests.EventFields.Inliners.AddAssignment
{
    // <target>
    class Target
    {
        event EventHandler? Foo;

        [PseudoOverride(nameof(Foo), "TestAspect")]
        event EventHandler Foo_Override
        {
            add
            {
                Console.WriteLine("Before");
                link[_this.Foo.add, inline] += value;
                Console.WriteLine("After");
            }
            remove { }
        }
    }
}
