using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Events.Inliners.RemoveAssignment
{
    // <target>
    public class Target
    {
        private EventHandler? field;

        public event EventHandler? Foo
        {
            add { }
            remove
            {
                Console.WriteLine("Original");
                this.field -= value;
            }
        }

        [PseudoOverride(nameof(Foo), "TestAspect")]
        public event EventHandler? Foo_Override
        {
            add { }
            remove
            {
                Console.WriteLine("Before");
                link[_this.Foo.remove, inline] -= value;
                Console.WriteLine("After");
            }
        }
    }
}
