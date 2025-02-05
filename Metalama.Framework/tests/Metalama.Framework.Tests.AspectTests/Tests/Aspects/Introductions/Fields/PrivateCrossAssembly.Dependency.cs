using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Fields.PrivateCrossAssembly;

public class MyAspect : TypeAspect
{
    [Introduce]
    private InternalClass _introduced = new();
}

internal class InternalClass { }