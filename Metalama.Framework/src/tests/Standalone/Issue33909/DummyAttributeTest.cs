// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.


using Metalama.Framework.Aspects;

namespace Issue33909.EmitErrorAttributeTests
{
    public class DummyAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            return meta.Proceed();
        }
    }

    internal class DummyAttributeTest
    {
        // <target>
        [Dummy]
        public static void MyMethod()
        {
            Console.WriteLine("Hello, world");
        }
    }
}