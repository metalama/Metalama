using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Events.Inliners.RemoveAssignment_NotRemoveAssignment
{
    // <target>
    public class Target
    {
        private EventHandler? field;

        event EventHandler Foo
        {
            add { }
            remove
            {
                Console.WriteLine("Original");
                this.field -= value;
            }
        }

        [PseudoOverride(nameof(Foo), "TestAspect")]
        private event EventHandler Foo_Override
        {
            add { }
            remove
            {
                Console.WriteLine("Before");
                link[_this.Foo.add, inline] += null;
                Console.WriteLine("After");
            }
        }
    }
}
