using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.LocalFunctions.InsideCompileTimeBlock;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        if (true)
        {
            {
                var local = (Action<TargetCode>)Local;

                void Local( TargetCode x ) { }
            }
        }

        {
            var local = (Action<TargetCode>)Local;

            void Local( TargetCode x ) { }
        }

        return default;
    }
}

internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}