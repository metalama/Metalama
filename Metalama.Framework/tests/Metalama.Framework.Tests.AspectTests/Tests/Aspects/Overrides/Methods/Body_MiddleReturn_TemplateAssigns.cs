using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.Body_MiddleReturn_TemplateAssigns
{
    /*
     * Tests override method attribute where target method body contains return from the middle of the method. which forces aspect linker to use jumps to inline the override.
     * Template stores the result into a variable.
     */

    public class OverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Begin override." );
            var result = meta.Proceed();
            Console.WriteLine( "End override." );

            return result;
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public void VoidMethod( int x )
        {
            Console.WriteLine( "Begin target." );

            if (x == 0)
            {
                return;
            }

            Console.WriteLine( "End target." );
        }

        [Override]
        public int Method( int x )
        {
            Console.WriteLine( "Begin target." );

            if (x == 0)
            {
                return 42;
            }

            Console.WriteLine( "End target." );

            return x;
        }

        [Override]
        public T? GenericMethod<T>( T? x )
        {
            Console.WriteLine( "Begin target." );

            if (x?.Equals( default ) ?? false)
            {
                return x;
            }

            Console.WriteLine( "End target." );

            return x;
        }
    }
}