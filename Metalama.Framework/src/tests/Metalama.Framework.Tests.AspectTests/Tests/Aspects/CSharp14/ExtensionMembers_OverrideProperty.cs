// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_OverrideProperty;

internal class TheAspect : OverrideFieldOrPropertyAspect
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
internal static class C
{
    extension( TestClass test )
    {
        [TheAspect]
        public int Property
        {
            get
            {
                Console.WriteLine( "Original." );
                return 42;
            }
            set
            {
                Console.WriteLine( "Original." );
            }
        }

        [TheAspect]
        public static int StaticProperty
        {
            get
            {
                Console.WriteLine( "Original." );
                return 42;
            }
            set
            {
                Console.WriteLine( "Original." );
            }
        }
    }
}

// <target>
internal class Test
{
    public void Foo()
    {
        var test = new TestClass();
        test.Property += 1;
        TestClass.StaticProperty += 1;
    }
}

internal class TestClass
{
}

#endif