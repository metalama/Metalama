using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.ParameterMapping
{
    /*
     * Verifies that template parameters are correctly mapped by index.
     */

    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.With( builder.Target.Methods.Single( m => m.OperatorKind == OperatorKind.Addition ) ).Override( nameof(InvertedParameters) );

            builder.With( builder.Target.Methods.Single( m => m.OperatorKind == OperatorKind.ExplicitConversion ) )
                .Override( nameof(DifferentlyNamedParameter) );
        }

        [Template]
        public int InvertedParameters( dynamic y, int x )
        {
            var z = meta.Proceed();

            return y.ToString().Length + x;
        }

        [Template]
        public int DifferentlyNamedParameter( dynamic y )
        {
            var z = meta.Proceed();

            return y.ToString().Length + 42;
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass
    {
        public static int operator +( TargetClass x, int y )
        {
            return x.ToString()!.Length + y;
        }

        public static explicit operator int( TargetClass x )
        {
            return x.ToString()!.Length + 42;
        }
    }
}