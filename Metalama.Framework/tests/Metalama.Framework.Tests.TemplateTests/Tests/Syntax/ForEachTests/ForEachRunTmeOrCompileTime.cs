using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Syntax.ForEachTests.ForEachRunTmeOrCompileTime;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private void Template()
    {
        foreach (var i in new[] { 42 })
        {
            Console.WriteLine( i );
        }
    }
}

internal class TargetCode
{
    private void Method() { }
}