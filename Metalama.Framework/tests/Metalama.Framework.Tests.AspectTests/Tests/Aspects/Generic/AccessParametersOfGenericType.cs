using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Generic.AccessParametersOfGenericType
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            foreach (var parameter in meta.Target.Parameters)
            {
                var v = parameter.Value;
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