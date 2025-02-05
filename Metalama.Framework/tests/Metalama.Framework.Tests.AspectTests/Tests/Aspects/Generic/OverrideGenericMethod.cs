using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Generic.OverrideGenericMethod
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
        private T Method<T>( T a )
        {
            return a;
        }
    }
}