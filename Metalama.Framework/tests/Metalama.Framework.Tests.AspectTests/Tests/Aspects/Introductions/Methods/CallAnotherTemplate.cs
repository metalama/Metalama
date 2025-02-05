using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Methods.CallAnotherTemplate
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public void IntroducedMethod()
        {
            Console.WriteLine( "This is introduced method." );
            AnotherMethod();
            meta.Proceed();
        }

        [Introduce]
        public static void IntroducedMethod_Static()
        {
            Console.WriteLine( "This is introduced method." );
            AnotherMethod_Static();
            meta.Proceed();
        }

        [Introduce]
        public void AnotherMethod()
        {
            Console.WriteLine( "This is another method." );
            meta.Proceed();
        }

        [Introduce]
        public static void AnotherMethod_Static()
        {
            Console.WriteLine( "This is another method." );
            meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}