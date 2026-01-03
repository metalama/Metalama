// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.LocalFunctionAfterNestedCompileTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        // Local function defined after nested compile-time if with return should be generated.
        [TestTemplate]
        private dynamic? Template()
        {
            if ( meta.Target.Method.Name.Length > 0 )
            {
                if ( meta.Target.Method.Name == "Method" )
                {
                    return LocalFunc( meta.Proceed() );
                }
            }

            return null;

            object? LocalFunc( object? input )
            {
                Console.WriteLine( "LocalFunc called" );
                return input;
            }
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
