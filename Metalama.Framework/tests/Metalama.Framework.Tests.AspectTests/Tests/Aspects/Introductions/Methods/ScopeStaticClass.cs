using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ScopeStaticClass
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( Scope = IntroductionScope.Default )]
        public static int DefaultScopeStatic()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Scope = IntroductionScope.Static )]
        public int StaticScope()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Scope = IntroductionScope.Static )]
        public static int StaticScopeStatic()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Scope = IntroductionScope.Target )]
        public int TargetScope()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }

        [Introduce( Scope = IntroductionScope.Target )]
        public static int TargetScopeStatic()
        {
            Console.WriteLine( "This is introduced method." );

            return 42;
        }
    }

    // <target>
    [Introduction]
    internal static class TargetClass { }
}