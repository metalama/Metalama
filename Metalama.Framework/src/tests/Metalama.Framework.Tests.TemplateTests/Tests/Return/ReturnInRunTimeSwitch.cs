// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInRunTimeSwitch
{
    [CompileTime]
    internal class Aspect
    {
        // Return inside a run-time switch should NOT terminate compile-time flow.
        [TestTemplate]
        private dynamic? Template()
        {
            switch ( meta.Target.Parameters[0].Value )
            {
                case null:
                    return null;

                default:
                    break;
            }

            // This code SHOULD be reached at compile time because the switch is run-time.
            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method( object? a )
        {
            return null;
        }
    }
}
