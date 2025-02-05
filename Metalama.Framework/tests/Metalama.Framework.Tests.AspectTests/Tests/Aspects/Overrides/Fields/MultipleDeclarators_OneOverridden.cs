using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.MultipleDeclarators_OnePromoted
{
    public class TestAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Fields.OfName( "B" ).Single() ).Override( nameof(PropertyTemplate) );
        }

        [Template]
        public dynamic? PropertyTemplate
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

    // <target>
    [Test]
    internal class TargetClass
    {
        // Comment before.
        public int A, B, C;

        // Comment after.
    }
}