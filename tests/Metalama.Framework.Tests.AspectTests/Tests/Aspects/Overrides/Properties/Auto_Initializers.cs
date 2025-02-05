using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Linq;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_Initializers;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Properties.Auto_Initializers
{
    /*
     * Tests a single OverrideProperty aspect on auto properties with initializers and that accesses in constructor bodies are properly rewritten to the backing field.
     */

    public class OverrideAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
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

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var p in builder.AdvisedTarget.Properties.Where( p => !p.IsImplicitlyDeclared ))
            {
                builder.With( p ).AddAspect( new OverrideAttribute() );
            }
        }

        [Introduce]
        public int IntroducedProperty { get; set; } = meta.ThisType.StaticProperty;

        [Introduce]
        public static int IntroducedStaticProperty { get; set; } = meta.ThisType.StaticProperty;
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int Property { get; set; } = 42;

        public static int StaticProperty { get; set; } = 42;
    }
}