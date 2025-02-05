using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.LambdaStatement;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        object? result = null;
        RunTimeClass.Execute( () => { result = meta.Proceed(); } );

        return result;
    }
}

internal class RunTimeClass
{
    public static void Execute( Action action ) => action();
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}