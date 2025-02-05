using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Finalizers.ExistingConflict_Override
{
    /*
     * Tests single introducing a finalizer into a class that already has one a using Override conflict behavior overrides the finalizer.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var introductionResult = builder.IntroduceFinalizer( nameof(IntroduceTemplate), whenExists: OverrideStrategy.Override );
        }

        [Template]
        public dynamic? IntroduceTemplate()
        {
            Console.WriteLine( "This is the introduction." );

            return meta.Proceed();
        }
    }

    // <target>
    [Override]
    internal class TargetClass
    {
        ~TargetClass()
        {
            Console.WriteLine( "This is the existing finalizer." );
        }
    }
}