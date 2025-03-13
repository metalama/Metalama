// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.MultipleAspects;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OuterOverrideAttribute), typeof(InnerOverrideAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.MultipleAspects
{
    /*
     * Tests that multiple aspects overriding an operator work correctly.
     */

    public class InnerOverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "This is the inner overriding template method." );

            return meta.Proceed();
        }
    }

    public class OuterOverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "This is the outer overriding template method." );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [InnerOverride]
        [OuterOverride]
        public static TargetClass operator +( TargetClass a, TargetClass b )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }

        [InnerOverride]
        [OuterOverride]
        public static TargetClass operator -( TargetClass a )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }

        [InnerOverride]
        [OuterOverride]
        public static explicit operator TargetClass( int x )
        {
            Console.WriteLine( $"This is the original operator." );

            return new TargetClass();
        }
    }
}