#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using Metalama.Framework.Code;

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.PrimaryConstructorParameter
{
    /*
     * Tests that overriding of fields with initializers that reference primary constructor parameter correctly retains the initializer.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var field in builder.Target.Fields)
            {
                builder.With( field ).Override( nameof(OverrideTemplate) );
            }
        }

        [Template]
        public dynamic? OverrideTemplate
        {
            get
            {
                Console.WriteLine( "This is the overridden getter." );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "This is the overridden setter." );
                meta.Proceed();
            }
        }
    }

    // <target>
    [Override]
    internal class TargetClass( int x )
    {
        public int Foo()
        {
            return x++;
        }
    }
}

#endif