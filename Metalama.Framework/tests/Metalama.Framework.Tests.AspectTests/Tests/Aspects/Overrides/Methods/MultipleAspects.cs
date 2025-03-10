// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.Simple_TwoOverrides;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(OuterOverrideAttribute), typeof(InnerOverrideAttribute) )]

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.Simple_TwoOverrides
{
    /*
     * Tests two OverrideMethod aspect with trivial template on methods with trivial bodies.
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
        public void VoidMethod()
        {
            Console.WriteLine( "This is the original method." );
        }

        [InnerOverride]
        [OuterOverride]
        public int Method( int x )
        {
            Console.WriteLine( $"This is the original method." );

            return x;
        }

        [InnerOverride]
        [OuterOverride]
        public T? GenericMethod<T>( T? x )
        {
            Console.WriteLine( "This is the original method." );

            return x;
        }
    }
}