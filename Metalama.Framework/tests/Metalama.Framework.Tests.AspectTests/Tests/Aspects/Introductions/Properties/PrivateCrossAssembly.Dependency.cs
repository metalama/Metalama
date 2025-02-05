using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Properties.PrivateCrossAssembly;

public class MyAspect : TypeAspect
{
    [Introduce]
    private InternalClass Introduced { get; } = new();
}

internal class InternalClass { }