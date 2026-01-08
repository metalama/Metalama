// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.SameVariableNameInDifferentBranches
{
    [CompileTime]
    internal class Aspect
    {
        // Test: When the same variable name is used in multiple if/else if/else branches,
        // the hoisting mechanism should handle them correctly as different symbols.
        [TestTemplate]
        private dynamic? Template()
        {
            if ( meta.Target.Method.Name.Length < 0 )
            {
                return null;
            }

            if ( meta.Target.Method.Name.Length < 5 )
            {
                var result = meta.CompileTime( "short" );
                meta.InsertComment( result );
                return null;
            }
            else if ( meta.Target.Method.Name.Length > 10 )
            {
                var result = meta.CompileTime( "long" );
                meta.InsertComment( result );
                return null;
            }
            else
            {
                var result = meta.CompileTime( "medium" );
                meta.InsertComment( result );
            }

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method()
        {
            return null;
        }
    }
}
