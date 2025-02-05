using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

#pragma warning disable CS0168

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.Variance;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var covariantInterface = builder.IntroduceInterface(
            "ITestCovariant",
            buildType: b =>
            {
                var t = b.AddTypeParameter("T");
                t.Variance = VarianceKind.Out;
            });

        var contravariantInterface = builder.IntroduceInterface(
            "ITestContravariant",
            buildType: b =>
            {
                var t = b.AddTypeParameter("T");
                t.Variance = VarianceKind.In;
            });

        var testUsage = builder.IntroduceClass("TestUsage");

        testUsage.IntroduceMethod(
            nameof(TestVariance),
            args: new
            {
                T = covariantInterface.Declaration.MakeGenericInstance(typeof(object)),
                U = covariantInterface.Declaration.MakeGenericInstance(typeof(string)),
            },
            buildMethod: b => { b.Name = "TestCovariance"; });

        testUsage.IntroduceMethod(
            nameof(TestVariance),
            args: new
            {
                T = contravariantInterface.Declaration.MakeGenericInstance(typeof(string)),
                U = contravariantInterface.Declaration.MakeGenericInstance(typeof(object)),
            },
            buildMethod: b => { b.Name = "TestContravariance"; });
    }

    [Template]
    public void TestVariance<[CompileTime] T, [CompileTime] U>( U p )
        where U : T
    {
        T t;

        ExpressionBuilder e = new ExpressionBuilder();

        e.AppendVerbatim("t = p");

        meta.InsertStatement(e.ToExpression());
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }