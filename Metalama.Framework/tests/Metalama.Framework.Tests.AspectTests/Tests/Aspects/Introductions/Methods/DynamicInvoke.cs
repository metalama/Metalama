using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.DynamicInvoke
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public void IntroduceVoid()
        {
            Console.WriteLine( "Introduced" );
            meta.Target.Method.Invoke();
        }

        [Introduce]
        public int IntroduceInt()
        {
            Console.WriteLine( "Introduced" );

            return meta.Target.Method.Invoke();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int OverrideInt()
        {
            Console.WriteLine( "Introduced" );

            return meta.Target.Method.Invoke();
        }

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public void OverrideVoid()
        {
            Console.WriteLine( "Introduced" );

            meta.Target.Method.Invoke();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int OverrideInt()
        {
            return 1;
        }

        public void OverrideVoid() { }
    }
}