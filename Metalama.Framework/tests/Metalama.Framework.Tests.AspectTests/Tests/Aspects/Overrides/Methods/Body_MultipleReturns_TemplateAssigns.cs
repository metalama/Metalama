using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.Body_MultipleReturns_TemplateAssigns
{
    // Tests overrides where the target method contains multiple returns.
    // Template assigns the result to a variable.

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
            while (x > 0)
            {
                if (x == 42)
                {
                    return;
                }

                x--;
            }

            if (x > 0)
            {
                return;
            }
        }

        [Override]
        public int Method( int x )
        {
            while (x > 0)
            {
                if (x == 42)
                {
                    return 42;
                }

                x--;
            }

            if (x > 0)
            {
                return -1;
            }

            return 0;
        }

        [Override]
        public T? GenericMethod<T>( T? x )
        {
            var z = 42;

            {
                while (z > 0)
                {
                    if (z == 42)
                    {
                        return x;
                    }

                    z--;
                }

                if (z > 0)
                {
                    return x;
                }

                return default;
            }
        }
    }
}