using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Visibility
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( Accessibility = Accessibility.Private )]
        public int Private()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Accessibility = Accessibility.ProtectedInternal )]
        public int ProtectedInternal()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Accessibility = Accessibility.PrivateProtected )]
        public int PrivateProtected()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Accessibility = Accessibility.Internal )]
        public int Internal()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Accessibility = Accessibility.Protected )]
        public int Protected()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Accessibility = Accessibility.Public )]
        public int Public()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}