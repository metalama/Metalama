// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @IgnoredDiagnostic(CS1998)
#endif

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Declarative_Async
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public async void IntroducedMethod_Void()
        {
            Console.WriteLine( "This is introduced method." );
            await Task.Yield();
            await meta.ProceedAsync();
        }

        [Introduce]
        public async Task IntroducedMethod_TaskVoid()
        {
            Console.WriteLine( "This is introduced method." );
            await Task.Yield();
            await meta.ProceedAsync();
        }

        [Introduce]
        public async Task<int> IntroducedMethod_TaskInt()
        {
            Console.WriteLine( "This is introduced method." );
            await Task.Yield();

            return await meta.ProceedAsync();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}