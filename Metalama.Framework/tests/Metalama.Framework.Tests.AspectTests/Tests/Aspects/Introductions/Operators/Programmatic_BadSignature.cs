using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.Programmatic_BadSignature
{
    public class IntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.IntroduceUnaryOperator( nameof(UnaryOperatorTemplate), builder.Target, builder.Target, OperatorKind.UnaryNegation );
        }

        [Template]
        public dynamic? UnaryOperatorTemplate( dynamic? x, dynamic? y )
        {
            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}