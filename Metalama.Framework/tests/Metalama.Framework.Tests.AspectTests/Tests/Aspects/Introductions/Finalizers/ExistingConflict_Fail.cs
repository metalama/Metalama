using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Finalizers.ExistingConflict_Fail
{
    /*
     * Tests single introducing a finalizer into a class that already has one a using Fail conflict behavior produced an error.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            var introductionResult = builder.IntroduceFinalizer( nameof(IntroduceTemplate), whenExists: OverrideStrategy.Fail );
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