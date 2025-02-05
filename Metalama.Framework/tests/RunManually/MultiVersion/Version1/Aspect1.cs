using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

[Inherited]
public class Aspect1 : TypeAspect
{
    
    [Introduce( WhenExists = OverrideStrategy.New )]
    public static void TheMethod()
    {
        meta.Proceed();

        var version = meta.CompileTime( typeof(IAspectBuilder).Assembly.GetName() );
        Console.WriteLine($"Method {meta.Target.Method} introduced by Metalama version {version}");
    }
}