using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.Issue30183
{
    /*
     * Tests that is there is a trivia around variable declarator where one field is overridden, it is correctly processed.
     */

    public class TestAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Fields.OfName( "E" ).Single() ).Override( nameof(PropertyTemplate) );
            builder.With( builder.Target.Fields.OfName( "G" ).Single() ).Override( nameof(PropertyTemplate) );
            builder.With( builder.Target.Fields.OfName( "H" ).Single() ).Override( nameof(PropertyTemplate) );
            builder.With( builder.Target.Fields.OfName( "I" ).Single() ).Override( nameof(PropertyTemplate) );
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
        // Comment before first list (no override).
        /// <summary>
        /// Doc comment for A, B, C.
        /// </summary>
        public // Comment after keyword.
            // Comment before variable declarator.
            int A, B, C // Comment after variable declarator
            ;           // Comment after first list.

        // Comment before first list (one overridden).
        /// <summary>
        /// Doc comment for D, E, F.
        /// </summary>
        public // Comment after keyword.
            // Comment before variable declarator.
            int D, E, F // Comment after variable declarator
            ;           // Comment after first list.

        // Comment before first list (all overridden).
        /// <summary>
        /// Doc comment for G, H, I.
        /// </summary>
        public // Comment after keyword.
            // Comment before variable declarator.
            int G, H, I // Comment after variable declarator
            ;           // Comment after first list.
    }
}