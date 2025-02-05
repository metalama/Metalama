using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Inliners.MethodInvocation_NotInvocation
{
    // <target>
    class Target
    {
        void Foo()
        {
            Console.WriteLine( "Original");
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        void Foo_Override()
        {
            Console.WriteLine( "Before");
            var action = (Action)link( _this.Foo, inline);
            Console.WriteLine( "After");
        }
    }
}
