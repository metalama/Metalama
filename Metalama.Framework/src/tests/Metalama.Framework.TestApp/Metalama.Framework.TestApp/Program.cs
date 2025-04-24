// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading;
using System.Threading.Tasks;
using Metalama.Framework.TestApp.Aspects;

namespace Metalama.Framework.TestApp
{
    [IntroduceSomeMethodAspect("Foo", "Bar")]
    internal partial class Program
    {
        [SuppressWarning]
        private static Task MethodAsync()
        {
            return Task.CompletedTask;
        }

        private static void Main()
        {

            Foo();
            new Program().SomeOtherIntroducedMethod();


            MethodWithTwoAspects();

            PrintDebugInfo();

            PrintArray();

            Cancel();
        }

        private void Method() { }

        [SwallowExceptionsAspect]
        [PrintDebugInfoAspect]
        public static void MethodWithTwoAspects()
        {
            Console.WriteLine("This is method with two aspects");
        }

        [PrintDebugInfoAspect]
        public static void SomeOtherMethod()
        {

        }

        [PrintDebugInfoAspect]
        private static void PrintDebugInfo() { }

        private static void PrintArray()
        {
            var a = new[] { 1, 2, 3, 4 };

            for (var i = 0; i < 10; i++)
            {
                PrintArrayAtIndex(a, i);
            }
        }

        [SwallowExceptionsAspect]
        private static void PrintArrayAtIndex(int[] a, int i)
        {
            Console.WriteLine(a[i]);
            Thread.Sleep(100);
        }

        private static void Cancel()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Cancellable0();
            Cancellable1(cts.Token);
        }

        [CancelAspect]
        private static void Cancellable0()
        {
            Console.WriteLine("Hello, Cancellable0.");
        }

        [CancelAspect]
        private static void Cancellable1(CancellationToken ct)
        {
            Console.WriteLine("Hello, Cancellable1.");
        }

        [CancelAspect]
        private static void Cancellable2(CancellationToken ct1, CancellationToken ct2)
        {
            Console.WriteLine("Hello, Cancellable2.");
        }
    }


}
