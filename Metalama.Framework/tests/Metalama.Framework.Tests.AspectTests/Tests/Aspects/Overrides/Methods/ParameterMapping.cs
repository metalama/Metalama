using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.ParameterMapping
{
    /*
     * Verifies that template parameters are correctly mapped by name.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Methods.OfName( "Method_InvertedParameters" ).Single() ).Override( nameof(InvertedParameters) );

            builder.With( builder.Target.Methods.OfName( "Method_SelectFirstParameter" ).Single() ).Override( nameof(SelectFirstParameter) );

            builder.With( builder.Target.Methods.OfName( "Method_SelectSecondParameter" ).Single() ).Override( nameof(SelectSecondParameter) );
        }

        [Template]
        public int InvertedParameters( int y, string x )
        {
            var z = meta.Proceed();

            return x.Length + y;
        }

        [Template]
        public int SelectFirstParameter( string x )
        {
            var z = meta.Proceed();

            return x.Length;
        }

        [Template]
        public int SelectSecondParameter( int y )
        {
            var z = meta.Proceed();

            return y;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public int Method_InvertedParameters( string x, int y )
        {
            return x.Length + y;
        }

        public int Method_SelectFirstParameter( string x, int y )
        {
            return x.Length + y;
        }

        public int Method_SelectSecondParameter( string x, int y )
        {
            return x.Length + y;
        }
    }
}