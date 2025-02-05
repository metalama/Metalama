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