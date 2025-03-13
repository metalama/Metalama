// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.Misc.AnonymousObject
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var x = new { A = meta.Target.Parameters[0].Value, B = meta.Target.Parameters[1].Value, Count = meta.Target.Parameters.Count };

            var y = new { Count = meta.Target.Parameters.Count };

            Console.WriteLine( x );
            Console.WriteLine( x.A );
            Console.WriteLine( x.Count );
            Console.WriteLine( y.Count );

            var result = meta.Proceed();

            return result;
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