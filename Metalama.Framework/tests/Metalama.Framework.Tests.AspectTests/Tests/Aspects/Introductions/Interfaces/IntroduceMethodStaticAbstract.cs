#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceMethodStaticAbstract;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var @interface = builder.IntroduceInterface( "ITest");
        @interface.IntroduceMethod( nameof(TestMethod) );

        // Implementation type
        var implementation = builder.IntroduceClass("TestImplementation");
        implementation.ImplementInterface( @interface.Declaration);
        var implementationMethod = implementation.IntroduceMethod( nameof(TestMethodImplementation), buildMethod: b => { b.Name = nameof(TestMethod); });

        // Usage type
        var usage = builder.Advice.IntroduceClass(
            builder.Target,
            "TestUsage",
            buildType: b =>
            {
                var typeParam = b.AddTypeParameter("T");
                typeParam.AddTypeConstraint(@interface.Declaration);
            });

        // Method that uses the interface.
        builder.Advice.IntroduceMethod(
            usage.Declaration, 
            nameof(TestUsageMethod), 
            args: new 
            { 
                genericParameter = usage.Declaration.TypeParameters.Single(),
                interfaceMethod = @interface.Declaration.Methods.Single(),
                implementationMethod = implementationMethod.Declaration,
            });
    }

    [Template]
    public static extern void TestMethod();

    [Template]
    public static void TestMethodImplementation()
    {
        Console.WriteLine("Implementation");
    }

    [Template]
    public static void TestUsageMethod([CompileTime] ITypeParameter genericParameter, [CompileTime] IMethod interfaceMethod, [CompileTime] IMethod implementationMethod)
    {
        // Calling static methods of type parameters is not currently supported (generic parameter does not "gain" methods from constraints).
        ExpressionBuilder builder = new ExpressionBuilder();
        builder.AppendTypeName(genericParameter);
        builder.AppendVerbatim($".{interfaceMethod.Name}()");
        meta.InsertStatement(builder.ToExpression());

        implementationMethod.Invoke();
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif