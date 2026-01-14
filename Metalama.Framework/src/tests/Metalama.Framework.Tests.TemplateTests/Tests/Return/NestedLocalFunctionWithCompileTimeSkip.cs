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

namespace Metalama.Framework.Tests.TemplateTests.Return.NestedLocalFunctionWithCompileTimeSkip
{
    [CompileTime]
    internal class Aspect
    {
        // Tests truly nested local function with compile-time skip.
        // InnerFunc is defined inside OuterFunc - both should be generated.
        [TestTemplate]
        private dynamic? Template()
        {
            // Compile-time condition that is always true.
            if ( meta.Target.Method.Name == "Method" )
            {
                return OuterFunc( meta.Proceed() );
            }

            return null;

            // Outer local function defined after return in compile-time if
            object? OuterFunc( object? input )
            {
                // Inner local function NESTED inside OuterFunc
                object? InnerFunc( object? value )
                {
                    Console.WriteLine( "InnerFunc called" );

                    return value;
                }

                return InnerFunc( input );
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