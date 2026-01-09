// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.ForeachVariableAfterReturnInCompileTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Test: Foreach loop variables declared after an early return
        // should work correctly (though foreach variables have their own scope).
        [TestTemplate]
        private dynamic? Template()
        {
            if ( meta.Target.Method.Name.Length < 0 )
            {
                return null;
            }

            foreach ( var parameter in meta.Target.Method.Parameters )
            {
                meta.InsertComment( parameter.Name );
            }

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method( int x, string y )
        {
            return null;
        }
    }
}
