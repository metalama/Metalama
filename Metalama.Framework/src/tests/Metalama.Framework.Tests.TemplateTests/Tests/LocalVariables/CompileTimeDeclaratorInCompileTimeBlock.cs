// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.CompileTimeDeclaratorInCompileTimeBlock
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            if (meta.Target.Parameters.Count > 0)
            {
                var x = meta.CompileTime( 0 );
                Console.WriteLine( x );
            }

            if (meta.Target.Parameters.Count > 1)
            {
                var x = meta.CompileTime( 1 );
                Console.WriteLine( x );
            }

            foreach (var p in meta.Target.Parameters)
            {
                var y = meta.CompileTime( 0 );
                Console.WriteLine( y );
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
}