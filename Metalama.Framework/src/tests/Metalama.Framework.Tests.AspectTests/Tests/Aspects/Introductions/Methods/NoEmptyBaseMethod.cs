// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.NoEmptyBaseMethod
{
    /*
     * Tests that when meta.Target.Method.Invoke() is called in an introduced method with no base
     * implementation, the linker does not generate an empty base method. Instead:
     * - When the invocation is used as a statement, the statement should be suppressed.
     * - When the invocation is used as an expression, it should be replaced with the default value.
     */

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public void IntroduceVoid()
        {
            Console.WriteLine( "Before" );
            meta.Target.Method.Invoke();
            Console.WriteLine( "After" );
        }

        [Introduce]
        public int IntroduceInt()
        {
            Console.WriteLine( "Before" );

            return meta.Target.Method.Invoke();
        }

        [Introduce]
        public string? IntroduceString()
        {
            Console.WriteLine( "Before" );

            return meta.Target.Method.Invoke();
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass { }
}
