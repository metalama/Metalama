// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.Override
{
    public class OverrideAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine("Generated code.");

            try
            {
                return meta.Proceed();
            }
            catch (Exception)
            {
                Console.WriteLine("Oops!");

                throw;
            }
        }
    }

    
    public class TargetCode
    {
        [OverrideAspect]
        public int Method()
        {
            Console.WriteLine("User code.");

            return 1;
        }
        
    }
}