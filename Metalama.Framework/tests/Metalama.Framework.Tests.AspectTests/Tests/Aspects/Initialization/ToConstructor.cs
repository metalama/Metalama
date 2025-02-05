using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.ToConstructor;

#pragma warning disable CS0414

public class MyAspect : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.AddInitializer( nameof(Initialize) );
    }

    [Introduce]
    private int _f;

    [Template]
    private void Initialize()
    {
        _f = 5;
    }
}

// CS0414 is restored when the aspect is transformed, so we need to suppress it again.
#pragma warning disable CS0414

// <target>
public class C
{
    [MyAspect]
    public C() { }

    // The initializer should not be added here.
    public C( int c ) { }
}