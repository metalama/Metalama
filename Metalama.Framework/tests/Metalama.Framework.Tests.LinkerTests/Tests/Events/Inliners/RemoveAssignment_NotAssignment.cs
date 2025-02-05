using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

#pragma warning disable CS0067, CS0649

namespace Metalama.Framework.Tests.LinkerTests.Tests.Events.Inliners.RemoveAssignment_NotAssignment
{
    // <target>
    public class Target
    {
        event EventHandler? Foo;

        [PseudoOverride(nameof(Foo), "TestAspect")]
        private event EventHandler Foo_Override
        {
            add
            {
            }
            remove
            {
                Console.WriteLine("Before");
                link[_this.Foo.raise, inline]?.Invoke(null, new EventArgs());
                Console.WriteLine("After");
            }
        }
    }
}
