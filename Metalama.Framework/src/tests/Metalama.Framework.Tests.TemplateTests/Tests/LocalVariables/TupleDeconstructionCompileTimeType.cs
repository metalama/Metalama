// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.TemplateTests.LocalVariables.TupleDeconstructionCompileTimeType
{
    [CompileTime]
    internal class Aspect
    {
        // Test tuple deconstruction with compile-time types
        [TestTemplate]
        private dynamic? Template()
        {
            // The right-hand side is compile-time, types are explicit (compile-time types)
#pragma warning disable IDE0007 // Use 'var' instead of explicit type - explicit types are intentional in this test
            (IMethod method, int paramCount) = GetMethodInfo();
#pragma warning restore IDE0007

            Console.WriteLine( $"Method: {method.Name}, Parameters: {paramCount}" );

            return meta.Proceed();
        }

        [CompileTime]
        private static (IMethod, int) GetMethodInfo()
        {
            return (meta.Target.Method, meta.Target.Method.Parameters.Count);
        }
    }

    internal class TargetCode
    {
        // <target>
        private int Method( int a, int b )
        {
            return a + b;
        }
    }
}
