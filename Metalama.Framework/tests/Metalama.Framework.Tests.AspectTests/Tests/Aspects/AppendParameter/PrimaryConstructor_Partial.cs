#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.PrimaryConstructor_Partial;

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

public class A( int x )
{
    public int X { get; set; } = x;
}

// <target>
[MyAspect]
public partial class C { }

// <target>
public partial class C( int x ) : A( 42 )
{
    public int Y { get; } = x;
}

// <target>
public partial class C { }

#endif