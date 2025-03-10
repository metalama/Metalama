// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.CrossAssembly
{
    // <target>
    [Override]
    [Introduction]
    internal class TargetClass
    {
        public T ExistingMethod_Generic<T>(T x)
        {
            Console.WriteLine("Original");
            return x;
        }

        public int ExistingMethod_Expression(int x) => x;

        public async Task<int> ExistingMethod_TaskAsync()
        {
            Console.WriteLine("Original");
            await Task.Yield();
            return 42;
        }

        public async void ExistingMethod_VoidAsync()
        {
            Console.WriteLine("Original");
            await Task.Yield();
        }

        public IEnumerable<int> ExistingMethod_Iterator()
        {
            Console.WriteLine("Original");
            yield return 42;
        }
    }
}