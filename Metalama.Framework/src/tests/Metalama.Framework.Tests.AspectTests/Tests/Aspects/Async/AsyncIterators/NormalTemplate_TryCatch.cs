// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Async.AsyncIterators.NormalTemplate_TryCatch
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            try
            {
                var result = meta.Proceed();

                Console.WriteLine( "Success " + meta.Target.Method.Name );

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine( $"Caught {e.Message} in {meta.Target.Method.Name}" );

                throw;
            }
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        public async IAsyncEnumerable<int> AsyncEnumerable( int a )
        {
            Console.WriteLine( "Yield 1" );

            yield return 1;

            await Task.Yield();
            Console.WriteLine( "Yield 2" );

            yield return 2;
        }
    }
}
