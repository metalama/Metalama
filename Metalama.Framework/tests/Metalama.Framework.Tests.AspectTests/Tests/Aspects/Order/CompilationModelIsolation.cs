using System;
using System.Collections.Generic;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Order.IntroductionAndOverride.CompilationModelIsolation;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(Aspect1), typeof(Aspect2) )]

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Aspects.Order.IntroductionAndOverride.CompilationModelIsolation;

internal class Aspect1 : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var m in builder.Target.Methods)
        {
            builder.With( m ).Override( nameof(Override) );
        }
    }

    [Introduce]
    public static void IntroducedMethod1()
    {
        Console.WriteLine( "Method introduced by Aspect1." );
    }

    [Template]
    private dynamic? Override()
    {
        // The cast to IEnumerable is to avoid referencing the LinqExtensions in the engine assembly.

        Console.WriteLine(
            $"Executing Aspect1 on {meta.Target.Method.Name}. Methods present before applying Aspect1: "
            + string.Join( ", ", ( (IEnumerable<IMethod>)meta.Target.Type.Methods ).Select( m => m.Name ).OrderBy( m => m ).ToArray() ) );

        return meta.Proceed();
    }
}

internal class Aspect2 : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var m in builder.Target.Methods)
        {
            builder.With( m ).Override( nameof(Override) );
        }
    }

    [Introduce]
    public static void IntroducedMethod2()
    {
        Console.WriteLine( "Method introduced by Aspect2." );
    }

    [Template]
    private dynamic? Override()
    {
        // The cast to IEnumerable is to avoid referencing the LinqExtensions in the engine assembly.

        Console.WriteLine(
            $"Executing Aspect2 on {meta.Target.Method.Name}. Methods present before applying Aspect2: "
            + string.Join( ", ", ( (IEnumerable<IMethod>)meta.Target.Type.Methods ).Select( m => m.Name ).OrderBy( m => m ).ToArray() ) );

        return meta.Proceed();
    }
}

// <target>
[Aspect1]
[Aspect2]
internal class Foo
{
    public static void SourceMethod()
    {
        Console.WriteLine( "Method defined in source code." );
    }
}