using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.StaticConstructor;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.With( builder.Target.StaticConstructor! ).IntroduceParameter( "p", typeof(int), TypedConstant.Create( 15 ) );
    }
}

// <target>
[MyAspect]
public class C
{
    static C() { }
}