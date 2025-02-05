using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Introduced;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(TestAttribute), typeof(IntroductionAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Introduced
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public int Field;

        [Introduce( WhenExists = OverrideStrategy.New )]
        public int NewField;
    }

    public class TestAttribute : TypeAspect
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
                Console.WriteLine( "This is aspect code." );

                return meta.Proceed();
            }
            set
            {
                Console.WriteLine( "This is aspect code." );
                meta.Proceed();
            }
        }
    }

    internal class BaseClass
    {
        public int NewField;
    }

    // <target>
    [Introduction]
    [Test]
    internal class TargetClass : BaseClass { }
}