using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Fields.RunTimeOnlyType;

public class Aspect : TypeAspect
{
    [Introduce]
    internal RunTimeClass? Event;
}

// <target>
[Aspect]
internal class RunTimeClass { }