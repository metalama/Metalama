using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Target_Record_DoNotPull;

public class MyAspect : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.IntroduceParameter( "p", typeof(int), TypedConstant.Create( 15 ) );
    }
}

// <target>
public record R
{
    [MyAspect]
    public R() { }

    public R( string s ) : this() { }
}

// <target>
public record S : R { }