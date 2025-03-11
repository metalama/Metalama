// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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