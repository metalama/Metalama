using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Params;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            builder.With( constructor ).IntroduceParameter( "ip1", typeof(int), TypedConstant.Create( 13 ) );
            builder.With( constructor ).IntroduceParameter( "ip2", typeof(int), TypedConstant.Create( 42 ) );
        }
    }
}

// <target>
[MyAspect]
public class C
{
    public C( params int[] p0 ) { }

    public C( int p0, params string[] p1 ) { }

    public C( int p0, int p1 = 0, params string[] p2 ) { }
}