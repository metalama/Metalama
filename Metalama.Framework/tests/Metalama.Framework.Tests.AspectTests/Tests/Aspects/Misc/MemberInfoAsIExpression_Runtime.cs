using System.Reflection;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.MemberInfoAsIExpression_Runtime;

public sealed class TestAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        // typeof(RunTimeOrCompileTimeClass) is RuntimeType, not CompileTimeType, so GetMethod works on it (and returns RuntimeMethodInfo).
        var method = typeof(RunTimeOrCompileTimeClass).GetMethod( "M" );

        var arrayBuilder = new ArrayBuilder( typeof(MethodInfo) );
        arrayBuilder.Add( method.ToExpression() );

        var methodsInvalidatedByField = builder.IntroduceField(
            "methods",
            typeof(MethodInfo[]),
            buildField: b => { b.InitializerExpression = arrayBuilder.ToExpression(); } );
    }
}

// <target>
[TestAspect]
internal class TargetCode { }

[RunTimeOrCompileTime]
internal class RunTimeOrCompileTimeClass
{
    public void M() { }
}