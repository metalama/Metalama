using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.Body_LocalFunction
{
    /*
     * Tests that overriding a method with local function correctly transforms the mehtod and leaves the local function intact.
     */

    public class OverrideAttribute : OverrideMethodAspect
    {
        // NOTE: It's important that the template is non trivial.

        public override dynamic? OverrideMethod()
        {
            var result = meta.Proceed();
            Console.WriteLine( "This is the overriding method." );

            return result;
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public int Simple()
        {
            return Foo();

            int Foo()
            {
                return 42;
            }
        }

        [Override]
        public int Simple_Static()
        {
            return Foo();

            static int Foo()
            {
                return 42;
            }
        }

        [Override]
        public int ParameterCapture( int x )
        {
            return Foo();

            int Foo()
            {
                return x + 1;
            }
        }

        [Override]
        public int LocalCapture( int x )
        {
            var y = x + 1;

            return Foo();

            int Foo()
            {
                return y;
            }
        }
    }
}