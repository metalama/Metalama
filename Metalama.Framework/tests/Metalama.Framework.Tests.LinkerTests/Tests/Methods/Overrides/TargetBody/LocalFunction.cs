using System;
using static Metalama.Framework.Tests.LinkerTests.Tests.Api;

namespace Metalama.Framework.Tests.LinkerTests.Tests.Methods.Overrides.TargetBody.LocalFunction
{
    // <target>
    class TargetClass
    {
        int IntMethod()
        {
            if (new Random().Next() == 0)
            {
                return 0;
            }

            Foo();
            var x = Bar();

            Console.WriteLine( "Original");
            return x; 
            
            void Foo()
            {
                return;
            }

            int Bar()
            {
                int Quz() => 42;

                return Quz();
            }
        }

        [PseudoOverride( nameof(IntMethod), "TestAspect")]
        int IntMethod_Override()
        {
            Console.WriteLine( "Before");

            var y = link(_this.IntMethod, inline)();

            Console.WriteLine( "After");

            return y;
        }


        void VoidMethod()
        {
            if (new Random().Next() == 0)
            {
                return;
            }

            Foo();
            _ = Bar();

            Console.WriteLine("Original");

            void Foo()
            {
                return;
            }

            int Bar()
            {
                int Quz() => 42;

                return Quz();
            }
        }

        [PseudoOverride(nameof(VoidMethod), "TestAspect")]
        void VoidMethod_Override()
        {
            Console.WriteLine("Before");

            link(_this.VoidMethod, inline)();

            Console.WriteLine("After");
        }
    }
}
