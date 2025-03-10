// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.Body_MiddleReturns_TemplateReturns
{
    /*
     * Tests override method attribute where target method body contains return from the middle of the method. which forces aspect linker to use jumps to inline the override.
     * Template returns the result directly.
     */

    public class OverrideAttribute : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "Override." );

            return meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass
    {
        [Override]
        public void VoidMethod( int x )
        {
            Console.WriteLine( "Begin target." );

            if (x == 0)
            {
                return;
            }

            Console.WriteLine( "End target." );
        }

        [Override]
        public int Method( int x )
        {
            Console.WriteLine( "Begin target." );

            if (x == 0)
            {
                return 42;
            }

            Console.WriteLine( "End target." );

            return x;
        }

        [Override]
        public T? GenericMethod<T>( T? x )
        {
            Console.WriteLine( "Begin target." );

            if (x?.Equals( default ) ?? false)
            {
                return x;
            }

            Console.WriteLine( "End target." );

            return x;
        }
    }
}