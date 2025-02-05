using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOfInLambda;

internal class TestAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder ) { }

    public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        builder.MustSatisfy(
            method => method.Attributes.Any(
                a => a.ConstructorArguments is { Length: 1 } && a.ConstructorArguments.Single().Value as string == typeof(RunTimeClass).Name ),
            method => $"{method} must have a an attribute with {typeof(RunTimeClass)} argument" );

        builder.MustSatisfy(
            method => method.Attributes.Any(
                a => a.ConstructorArguments is { Length: 1 } && a.ConstructorArguments.Single().Value as string == typeof(RunTimeOrCompileTimeClass).Name ),
            method => $"{method} must have a an attribute with {typeof(RunTimeOrCompileTimeClass)} argument" );

        builder.MustSatisfy(
            method => method.Attributes.Any(
                a => a.ConstructorArguments is { Length: 1 } && a.ConstructorArguments.Single().Value as string == typeof(CompileTimeClass).Name ),
            method => $"{method} must have a an attribute with {typeof(CompileTimeClass)} argument" );
    }
}

internal class RunTimeClass { }

[RunTimeOrCompileTime]
internal class RunTimeOrCompileTimeClass { }

[CompileTime]
internal class CompileTimeClass { }

[AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
internal class TypeAttribute : Attribute
{
    public TypeAttribute( string type ) { }
}

// <target>
public partial class TargetClass
{
    [TestAspect]
    [Type( nameof(RunTimeClass) )]
    [Type( nameof(RunTimeOrCompileTimeClass) )]
    [Type( nameof(CompileTimeClass) )]
    private void M1() { }

    [TestAspect]
    [Type( nameof(RunTimeClass) )]
    [Type( nameof(RunTimeOrCompileTimeClass) )]
    private void M2() { }

    [TestAspect]
    [Type( nameof(RunTimeClass) )]
    private void M3() { }

    [TestAspect]
    private void M4() { }
}