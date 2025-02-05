using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Duplicate;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            builder.With( constructor ).IntroduceParameter( "p", typeof(int), TypedConstant.Create( 42 ) );
        }
    }
}

// <target>
[MyAspect]
public class C
{
    public C( int p ) { }
}