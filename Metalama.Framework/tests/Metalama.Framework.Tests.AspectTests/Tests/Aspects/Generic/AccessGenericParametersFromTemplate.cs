using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Generic.AccessGenericParametersFromTemplate
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            foreach (var genericParameter in meta.Target.Method.TypeParameters)
            {
                var v = meta.Default( genericParameter );
                var t = meta.RunTime( genericParameter.ToType() );
            }

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private T Method<T, S>( T a, S b )
        {
            return a;
        }
    }
}