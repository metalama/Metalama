using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Attributes_Uninlineable;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OverrideAttribute), typeof(IntroductionAttribute) )]

#pragma warning disable CS0169
#pragma warning disable CS0414

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Overrides.Fields.Attributes_Uninlineable
{
    /*
     * Tests that overriding fields keeps all the existing attributes or moves them to the backing field where necessary.
     */

    public class OverrideAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            foreach (var field in builder.Target.Fields)
            {
                builder.With( field ).Override( nameof(Template) );
            }
        }

        [Template]
        public dynamic? Template
        {
            get
            {
                Console.WriteLine( "This is the overridden getter." );
                _ = meta.Proceed();

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "This is the overridden setter." );
                meta.Proceed();
                meta.Proceed();
            }
        }
    }

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        [FieldOnly]
        [FieldAndProperty]
        public int IntroducedField;
    }

    [AttributeUsage( AttributeTargets.Field )]
    public class FieldOnlyAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
    public class FieldAndPropertyAttribute : Attribute { }

    // <target>
    [Introduction]
    [Override]
    internal class TargetClass
    {
        [FieldOnly]
        [FieldAndProperty]
        public int Field;
    }
}