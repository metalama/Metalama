using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.LambdaStatement_DelegateCreation;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        object? result = null;

        // The object creation is redundant and should be simplified.
        var action = meta.RunTime( new Action( () => { result = meta.Proceed(); } ) );

        return result;
    }
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}