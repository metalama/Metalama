using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.TypedConstantPullAction;

public class AddParameter : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceParameter(
            "arg",
            typeof(int),
            TypedConstant.Default( typeof(int) ),
            ( param, ctor ) => PullAction.UseExpression( TypedConstant.Create( 42 ) ) );
    }
}

// <target>
internal class TargetCode
{
    [AddParameter]
    private TargetCode( string s ) { }

    private TargetCode( int i ) : this( i.ToString() ) { }
}