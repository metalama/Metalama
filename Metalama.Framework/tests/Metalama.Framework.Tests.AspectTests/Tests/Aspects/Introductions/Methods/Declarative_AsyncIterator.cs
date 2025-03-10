// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER)
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Declarative_AsyncIterator
{
    public class IntroductionAttribute : TypeAspect
    {
#if NET5_0_OR_GREATER
        [Introduce]
        public async IAsyncEnumerable<int> IntroducedMethod_AsyncEnumerable()
        {
            Console.WriteLine( "This is introduced method." );
            await Task.Yield();
            yield return 42;
            await foreach (var x in meta.ProceedAsyncEnumerable())
            {
                yield return x;
            }
        }

        [Introduce]
        public async IAsyncEnumerator<int> IntroducedMethod_AsyncEnumerator()
        {
            Console.WriteLine("This is introduced method.");
            await Task.Yield();
            yield return 42;
            var enumerator = meta.ProceedAsyncEnumerator();
            while( await enumerator.MoveNextAsync() )
            {
                yield return enumerator.Current;
            }
        }
#endif
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}