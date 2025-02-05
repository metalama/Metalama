using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Multiple;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            builder.With( constructor ).IntroduceParameter( "p1", typeof(int), TypedConstant.Create( 13 ) );
            builder.With( constructor ).IntroduceParameter( "p2", typeof(int), TypedConstant.Create( 42 ) );
        }
    }
}

// <target>
[MyAspect]
public class C
{
    public C() { }

    public C( int p0 ) { }

    public C( string p0 ) { }
}