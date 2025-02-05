using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33671;

public class TestAspect : TypeAspect, IFace
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) { }

    public string? ProfileName { get; init; }
}

// <target>
[TestAspect]
internal class Target { }