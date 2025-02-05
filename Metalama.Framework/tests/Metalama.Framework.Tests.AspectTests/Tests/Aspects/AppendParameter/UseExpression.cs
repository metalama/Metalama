using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.AppendParameter.UseExpression;

public class MyAspect : ConstructorAspect
{
    public override void BuildAspect( IAspectBuilder<IConstructor> builder )
    {
        builder.IntroduceParameter(
            "p",
            typeof(DateTime),
            TypedConstant.Default( typeof(DateTime) ),
            ( parameter, constructor ) => PullAction.UseExpression( ExpressionFactory.Parse( "System.DateTime.Now" ) ) );
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