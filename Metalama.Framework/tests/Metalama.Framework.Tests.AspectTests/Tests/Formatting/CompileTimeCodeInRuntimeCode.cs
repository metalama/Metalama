using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.TestInputs.Highlighting.Declarations.CompileTimeCodeInRuntimeCode
{
    class Aspect : IAspect
    {
        [Template]
        dynamic? Template()
        {
            Console.WriteLine( $"Invoking {meta.Target.Method.ToDisplayString()}" );

            return meta.Proceed();
        }
    }
}
