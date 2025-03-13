// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER) - Default interface members need to be supported by the runtime.
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.InterfaceMembers
{
    /*
     * Tests overriding of interface non-abstract methods.
     */

    internal class OverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Override." );

            return meta.Proceed();
        }
    }

    // <target>
    public interface Interface
    {
#if NET5_0_OR_GREATER
        [Override]
        private int PrivateMethod()
        {
            Console.WriteLine("Original implementation");
            return 42;
        }

        [Override]
        public static int StaticMethod()
        {
            Console.WriteLine("Original implementation");
            return 42;
        }
#endif
    }

    // <target>
    public class TargetClass : Interface { }
}