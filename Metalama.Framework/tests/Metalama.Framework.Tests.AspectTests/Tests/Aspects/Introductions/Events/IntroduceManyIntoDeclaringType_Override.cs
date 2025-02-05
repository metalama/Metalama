using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Introductions.Events.IntroduceManyIntoDeclaringType_Override
{
    internal class Aspect : MethodAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        private event Action Event
        {
            add
            {
                Console.WriteLine( $"{meta.AspectInstance.TargetDeclaration} says hello." );
                meta.Proceed();
            }

            remove
            {
                meta.Proceed();
            }
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private void M() { }

        [Aspect]
        private void M2() { }
    }
}