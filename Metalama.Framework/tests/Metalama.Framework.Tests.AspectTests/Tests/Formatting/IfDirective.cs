using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.IfDirective
{
    public class SomeClass
    {
        void TestMain()
        {
#if METALAMA
            Console.WriteLine("This should be included.");
#else
            Console.WriteLine( "This should NOT be included." );
#endif

#if METALAMA
            Console.WriteLine("This should be included.");
#elif NOTHING
            Console.WriteLine( "This should NOT be included." );
#endif
            
        }
        
    }
}