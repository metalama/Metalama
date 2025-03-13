// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.NameClashCompileTimeIf
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var n = meta.Target.Parameters.Count;        // build-time
            object? y = meta.Target.Parameters[0].Value; // run-time

            if (n == 1)
            {
                var x = 0;
                Console.WriteLine( x );
            }

            if (y == null)
            {
                var x = 1;
                Console.WriteLine( x );
            }

            if (n == 1)
            {
                var x = 2;
                Console.WriteLine( x );
            }

            if (y == null)
            {
                var x = 3;
                Console.WriteLine( x );
            }

            return meta.Proceed();
        }
    }

    internal class TargetCode
    {
        private int Method( int a )
        {
            return a;
        }
    }
}