using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Generic.IntroduceGenericMethod
{
    internal class Aspect : TypeAspect
    {
        [Introduce]
        public T GenericMethod<T>( T a )
        {
            return a;
        }
    }

    // <target>
    [Aspect]
    internal class TargetCode { }
}