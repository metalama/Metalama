using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Declarative
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public void IntroducedMethod_Void()
        {
            Console.WriteLine( "This is introduced method." );
            meta.Proceed();
        }

        [Introduce]
        public int IntroducedMethod_Int()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        [Introduce]
        public int IntroducedMethod_Param( int x )
        {
            Console.WriteLine( $"This is introduced method, x = {x}." );

            return meta.Proceed();
        }

        [Introduce]
        public static int IntroducedMethod_StaticSignature()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        [Introduce( IsVirtual = true )]
        public int IntroducedMethod_VirtualExplicit()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}