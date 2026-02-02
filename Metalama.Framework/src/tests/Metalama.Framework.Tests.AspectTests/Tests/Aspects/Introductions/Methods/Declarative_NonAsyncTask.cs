// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.Declarative_NonAsyncTask
{
    /// <summary>
    /// Tests that non-async methods returning Task/Task{T} generate the correct default return.
    /// These methods don't have the async modifier, so they should return default(Task{T}) not default(T).
    /// </summary>
    public class IntroductionAttribute : TypeAspect
    {
        // Non-async method returning Task<int> - should generate return default(Task<int>);
        [Introduce]
        public Task<int> IntroducedMethod_TaskInt()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }

        // Non-async method returning Task - should generate return default(Task);
        [Introduce]
        public Task IntroducedMethod_Task()
        {
            Console.WriteLine( "This is introduced method." );

            return meta.Proceed();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}
