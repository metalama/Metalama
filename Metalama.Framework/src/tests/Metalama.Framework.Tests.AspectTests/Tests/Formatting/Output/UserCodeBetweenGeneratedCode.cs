// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.UserCodeBetweenGeneratedCode
{
    internal class LogAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine($"Invoking {meta.Target.Method.ToDisplayString()}");

            return meta.Proceed();
        }
    }

    internal class Foo
    {
        [Log]
        public void Method()
        {
            Console.WriteLine("InstanceMethod");
        }

        [Log]
        public void ExpressionMethod() => Console.WriteLine("InstanceMethod");
    }
}
