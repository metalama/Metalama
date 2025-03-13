// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.CompileTimeVariableInRunTimeConditionalBlock_Error;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        var a = 0;
        var i = meta.CompileTime( 0 );

        a++;
        i++;

        if (meta.Target.Parameters[0].Value > 0)
        {
            var b = 0;
            var j = meta.CompileTime( 0 );

            a++;
            b++;
            i++;
            j++;

            if (meta.Target.Parameters[1].Value > 0)
            {
                var c = 0;
                var k = meta.CompileTime( 0 );

                a++;
                b++;
                c++;
                i++;
                j++;
                k++;
            }
        }

        return meta.Proceed();
    }
}

internal class TargetCode
{
    private int Method( int a, int b )
    {
        return a + b;
    }
}