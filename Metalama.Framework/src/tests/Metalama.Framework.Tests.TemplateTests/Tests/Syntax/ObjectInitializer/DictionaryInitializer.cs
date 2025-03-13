// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.CSharpSyntax.Misc.DictionaryInitializer
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Neutral.
            var x = new Dictionary<int, int> { [1] = 1, [2] = 2, [3] = 3 };

            // Compile-time.
            var y = new Dictionary<int, int> { [1] = meta.Target.Parameters.Count, [2] = 2, [3] = 3 };
            Console.WriteLine( string.Join( ", ", y ) );

            // Run-time.
            var z = new Dictionary<int, int> { [1] = meta.Target.Parameters[0].Value, [2] = meta.Target.Parameters.Count, [3] = 3 };

            // Run-time, other form.
            Dictionary<string, string> report =
                new()
                {
                    { "Title", meta.Target.Member.Name },
                    { "ID", Guid.NewGuid().ToString() },
                    { "HTTP result", "400" },
                    { "Exception type", meta.Target.Parameters[0].Value.ToString() }
                };

            return default;
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