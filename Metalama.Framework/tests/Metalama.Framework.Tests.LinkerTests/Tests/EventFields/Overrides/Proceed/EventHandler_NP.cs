using System;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.LinkerTests.Tests.EventFields.Overrides.Proceed.EventHandler_NP
{
    // <target>
    class Target
    {
        event EventHandler? Foo;

        [PseudoOverride(nameof(Foo), "TestAspect")]
        event EventHandler Foo_Override1
        {
            add
            {
                Console.WriteLine("Override");
            }
            remove
            {
                Console.WriteLine("Override");
            }
        }
    }
}
