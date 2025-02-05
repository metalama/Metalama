using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplateParameters.IntroduceMethod_Optional_CrossAssembly;

public class Aspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        builder.IntroduceMethod(nameof(Method), args: new { T = typeof(int), a = 3, b = 5 }, buildMethod: m => { m.Name = "Method1"; });
        builder.IntroduceMethod(nameof(Method), args: new { T = typeof(int), a = 3, b = 5, d = 7 }, buildMethod: m => { m.Name = "Method2"; });
        builder.IntroduceMethod(nameof(Method), args: new { T = typeof(int), a = 3, b = 5, e = 7 }, buildMethod: m => { m.Name = "Method3"; });
        builder.IntroduceMethod(nameof(Method), args: new { T = typeof(int), a = 3, b = 5, d = 7, e = 11 }, buildMethod: m => { m.Name = "Method4"; });
    }

    [Template]
    private void Method<[CompileTime] T>( [CompileTime] int a, [CompileTime] int b, int c, [CompileTime] int d = 0, [CompileTime] int e = 0, int f = 0 )
    {
        Console.WriteLine(typeof(T));
        Console.WriteLine(a);
        Console.WriteLine(b);
        Console.WriteLine(c);
        Console.WriteLine(d);
        Console.WriteLine(e);
        Console.WriteLine(f);
    }
}