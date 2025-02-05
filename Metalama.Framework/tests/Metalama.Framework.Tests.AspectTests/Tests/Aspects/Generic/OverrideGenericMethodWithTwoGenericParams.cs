using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Generic.OverrideGenericMethodWithTwoGenericParams
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
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