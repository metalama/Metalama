using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Override
{
    public class OverrideAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine("Generated code.");

            try
            {
                return meta.Proceed();
            }
            catch (Exception)
            {
                Console.WriteLine("Oops!");

                throw;
            }
        }
    }

    
    public class TargetCode
    {
        [OverrideAspect]
        public int Method()
        {
            Console.WriteLine("User code.");

            return 1;
        }
        
    }
}