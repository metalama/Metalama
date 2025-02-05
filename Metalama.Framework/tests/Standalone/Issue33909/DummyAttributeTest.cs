// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.


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