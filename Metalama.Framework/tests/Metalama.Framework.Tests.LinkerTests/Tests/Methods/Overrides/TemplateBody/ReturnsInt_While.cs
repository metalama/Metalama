using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TemplateBody.ReturnsInt_While
{
    // <target>
    class Target
    {
        int Foo(int x)
        {
            Console.WriteLine( "Original");
            return x;
        }

        [PseudoOverride( nameof(Foo),"TestAspect")]
        int Foo_Override(int x)
        {
            Console.WriteLine( "Before");
            int i = 0;
            int k = 0;
            while (i < 0)
            {
                int result;
                result = link( _this.Foo, inline)(x);
                k += result;
                i++;
            }

            Console.WriteLine( "After");
            return k;
        }
    }
}
