// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.NestedLocalFunctionWithCompileTimeSkipInInner
{
    [CompileTime]
    internal class Aspect
    {
        // Tests nested local function where the compile-time condition is inside the inner local function.
        [TestTemplate]
        private dynamic? Template()
        {
            return OuterFunc( meta.Proceed() );

            // Outer local function
            object? OuterFunc( object? input )
            {
                return InnerFunc( input );

                // Inner local function with compile-time condition inside
                object? InnerFunc( object? value )
                {
                    // Compile-time condition inside inner local function
                    if ( meta.Target.Method.Name == "Method" )
                    {
                        Console.WriteLine( "InnerFunc called" );

                        return value;
                    }

                    return value;
                }
            }
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method()
        {
            return "test";
        }
    }
}
