using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AddAspect.Bug33751;

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        _ = builder.Target.BaseType!.Enhancements().HasAspect<TheAspect>();
    }
}

internal class D<T> { }

// <target>
[TheAspect]
internal class DD<T> : D<T> { }