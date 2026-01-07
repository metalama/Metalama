// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.CompileTimeVariableInSwitch;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        switch ( meta.Target.Parameters["x"].Value )
        {
            case 42:
                var method = meta.Target.Method;
                method.Invoke( 0 );

                break;
        }

        return meta.Proceed();
    }
}

internal class TargetCode
{
    private void Method( int x ) { }
}