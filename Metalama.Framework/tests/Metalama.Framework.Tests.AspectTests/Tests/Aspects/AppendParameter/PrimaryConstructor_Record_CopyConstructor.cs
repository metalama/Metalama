using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.PrimaryConstructor_Record_CopyConstructor;

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

public record A( int x )
{
    public int X { get; set; } = x;
}

// <target>
[MyAspect]
public record C( int x ) : A( 42 )
{
    public int Y { get; } = x;
}