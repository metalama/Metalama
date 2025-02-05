using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.ExistingConflictOverrideBaseSealed
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public int ExistingMethod()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }
    }

    internal class BaseClass
    {
        public virtual int ExistingMethod()
        {
            return default;
        }
    }

    internal class DerivedClass : BaseClass
    {
        public sealed override int ExistingMethod()
        {
            return default;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : DerivedClass { }
}