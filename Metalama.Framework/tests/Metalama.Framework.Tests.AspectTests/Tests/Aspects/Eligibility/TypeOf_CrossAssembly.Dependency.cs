using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Eligibility;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Eligibility.TypeOf_CrossAssembly;

public class TestAspect : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder ) { }

    public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        var runTimeClass = typeof(RunTimeClass);

        builder.MustSatisfy(
            method => method.Attributes.Any(
                a => a.ConstructorArguments is { Length: 1 } && a.ConstructorArguments.Single().Value as string == runTimeClass.Name ),
            method => $"{method} must have a an attribute with {runTimeClass} argument" );

        var runTimeOrCompileTimeClass = typeof(RunTimeOrCompileTimeClass);

        builder.MustSatisfy(
            method => method.Attributes.Any(
                a => a.ConstructorArguments is { Length: 1 } && a.ConstructorArguments.Single().Value as string == runTimeOrCompileTimeClass.Name ),
            method => $"{method} must have a an attribute with {runTimeOrCompileTimeClass} argument" );

        var compileTimeClass = typeof(CompileTimeClass);

        builder.MustSatisfy(
            method => method.Attributes.Any(
                a => a.ConstructorArguments is { Length: 1 } && a.ConstructorArguments.Single().Value as string == compileTimeClass.Name ),
            method => $"{method} must have a an attribute with {compileTimeClass} argument" );
    }
}

public class RunTimeClass { }

[RunTimeOrCompileTime]
public class RunTimeOrCompileTimeClass { }

[CompileTime]
public class CompileTimeClass { }