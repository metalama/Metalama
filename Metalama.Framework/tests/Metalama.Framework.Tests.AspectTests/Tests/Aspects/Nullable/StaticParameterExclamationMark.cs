using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Nullable.StaticParameterExclamationMark
{
    internal class Aspect : TypeAspect
    {
        [Introduce]
        private string? Introduced( string? a ) => a!.ToString();
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private class NullableTarget { }

#nullable disable

        [Aspect]
        private class NonNullableTarget { }
    }
}