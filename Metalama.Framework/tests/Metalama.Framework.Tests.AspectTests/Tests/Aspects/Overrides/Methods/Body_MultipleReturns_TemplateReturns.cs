// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Methods.Body_MultipleReturns_TemplateReturns
{
    // Tests overrides where the target method contains multiple returns and template returns the result directly.
    // Template returns the result directly.

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
            while (x > 0)
            {
                if (x == 42)
                {
                    return;
                }

                x--;
            }

            if (x > 0)
            {
                return;
            }
        }

        [Override]
        public int Method( int x )
        {
            while (x > 0)
            {
                if (x == 42)
                {
                    return 42;
                }

                x--;
            }

            if (x > 0)
            {
                return -1;
            }

            return 0;
        }

        [Override]
        public T? GenericMethod<T>( T? x )
        {
            var z = 42;

            {
                while (z > 0)
                {
                    if (z == 42)
                    {
                        return x;
                    }

                    z--;
                }

                if (z > 0)
                {
                    return x;
                }

                return default;
            }
        }
    }
}