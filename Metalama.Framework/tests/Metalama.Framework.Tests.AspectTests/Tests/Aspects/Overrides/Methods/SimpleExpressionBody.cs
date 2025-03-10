// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.SimpleExpressionBody
{
    // Tests single OverrideMethod aspect with trivial template on methods with trivial bodies.

    public class OverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "This is the overriding method." );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public void TargetMethod_Void() => Console.WriteLine( "This is the original method." );

        [Override]
        public void TargetMethod_Void( int x, int y ) => Console.WriteLine( $"This is the original method {x} {y}." );

        [Override]
        public int TargetMethod_Int() => 42;

        [Override]
        public int TargetMethod_Int( int x, int y ) => x + y;

        [Override]
        public static void TargetMethod_Static() => Console.WriteLine( "This is the original static method." );

        [Override]
        public void TargetMethod_Out( out int x ) => x = 42;

        [Override]
        public void TargetMethod_Ref( ref int x ) => x = 42;

        [Override]
        public void TargetMethod_In( in DateTime x ) => Console.WriteLine( $"This is the original method {x}." );
    }
}