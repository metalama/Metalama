using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.Pull;

public class MyAspect : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.IntroduceParameter(
            "p",
            typeof(int),
            TypedConstant.Create( 15 ),
            ( parameter, constructor ) => PullAction.IntroduceParameterAndPull( parameter.Name, parameter.Type, TypedConstant.Create( 20 ) ) );
    }
}

// <target>
public class C
{
    [MyAspect]
    public C() { }

    public C( string s ) : this() { }
}

// <target>
public class D : C { }