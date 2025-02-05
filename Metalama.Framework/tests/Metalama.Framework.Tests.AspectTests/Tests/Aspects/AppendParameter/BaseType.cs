using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.BaseType;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            builder.With( constructor ).IntroduceParameter( "p", typeof(int), TypedConstant.Create( 15 ) );
        }
    }
}

// <target>
[MyAspect]
public class A
{
    public A( int x )
    {
        X = x;
    }

    public int X { get; set; }
}

// <target>
public class C : A
{
    public C( int x ) : base( 42 )
    {
        Y = x;
    }

    public int Y { get; }
}