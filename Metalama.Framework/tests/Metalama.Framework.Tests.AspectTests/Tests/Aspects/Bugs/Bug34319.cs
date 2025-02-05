using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug34319;

public class IntroduceParametersAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        for (var i = 0; i < 3; i++)
        {
            builder.With( builder.Target.Constructors.Single() ).IntroduceParameter( $"p{i}", typeof(int), TypedConstant.Create( 0 ) );
        }
    }
}

// <target>
[IntroduceParameters]
internal class TargetWithoutConstructor { }

// <target>
[IntroduceParameters]
internal class TargetWithConstructor
{
    public TargetWithConstructor( string s ) { }
}