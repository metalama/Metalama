// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.InnerBlock
{
    public class TestAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            if (BoolSource.Value)
            {
                return default;
            }
            else
            {
                var result = meta.Proceed();
                return result;
            }
        }
    }

    internal static class BoolSource
    {
        public static bool Value;
    }

    // <target>
    public class Target
    {
        [TestAspect]
        private static int Add(int a, int b)
        {
            Console.WriteLine("Thinking...");
            return a + b;
        }
    }
}
