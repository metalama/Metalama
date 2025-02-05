using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Tests.AspectTests.Tests.Formatting.CallStaticMethod.ChildNs;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.CallStaticMethod
{
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            // Static property.
            var x = StaticClass.Now;

            // Static void method.
            StaticClass.Method();

            return meta.Proceed();
        }
    }

    namespace ChildNs
    {
        internal static class StaticClass
        {
            public static DateTime Now => DateTime.Now;

            public static void Method() { }
        }
    }

    internal class TargetCode
    {
        [Aspect]
        private void M( int a ) { }
    }
}