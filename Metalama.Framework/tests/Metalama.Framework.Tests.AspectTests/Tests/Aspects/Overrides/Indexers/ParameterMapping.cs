using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Indexers.ParameterMapping
{
    /*
     * Verifies that template parameter is correctly mapped by index.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Indexers.Single() )
                .OverrideAccessors(
                    nameof(RenamedParameterGetTemplate),
                    nameof(RenamedParameterSetTemplate) );
        }

        [Template]
        public int RenamedParameterGetTemplate( int y, string x )
        {
            var q = y + x.ToString().Length;

            return meta.Proceed();
        }

        [Template]
        public void RenamedParameterSetTemplate( int y, string x, int z )
        {
            var q = y + x.ToString().Length + z;
            meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int this[ int x, string y ]
        {
            get
            {
                return x + y.ToString().Length;
            }

            set
            {
                var z = x + y.ToString() + value;
            }
        }
    }
}