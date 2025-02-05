using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.PartialAspectClass
{
    internal partial class MyAspect
    {
        [Template]
        public dynamic? MethodTemplate() => meta.Proceed();
    }
}