// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(NET5_0_OR_GREATER) - Default interface members need to be supported by the runtime.
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.InterfaceMembers
{
    /*
     * Tests overriding of interface non-abstract properties.
     */

    internal class OverrideAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( "Override." );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "Override." );
                meta.Proceed();
            }
        }
    }

    // <target>
    public interface Interface
    {
#if NET5_0_OR_GREATER
        [Override]
        private int PrivateProperty
        {
            get
            {
                Console.WriteLine("Original implementation");
                return 42;
            }

            set
            {
                Console.WriteLine("Original implementation");
            }
        }

        [Override]
        public static int StaticAutoProperty { get; set; }

        [Override]
        public static int StaticProperty
        {
            get
            {
                Console.WriteLine("Original implementation");
                return 42;
            }

            set
            {
                Console.WriteLine("Original implementation");
            }
        }
#endif
    }

    // <target>
    public class TargetClass : Interface { }
}