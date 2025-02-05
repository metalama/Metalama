#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_4_8_0_OR_GREATER)
#endif

#if ROSLYN_4_8_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.PrimaryConstructor_BaseType_Default_Pull;

public class MyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var constructor in builder.Target.Constructors)
        {
            builder.With( constructor )
                .IntroduceParameter(
                    "p",
                    typeof(int),
                    TypedConstant.Create( 15 ),
                    ( p, c ) => PullAction.UseExpression( TypedConstant.Create( 51 ) ) );
        }
    }
}

// <target>
[MyAspect]
public class A() { }

// <target>
public class C( int x ) : A
{
    public int Y { get; } = x;
}

#endif