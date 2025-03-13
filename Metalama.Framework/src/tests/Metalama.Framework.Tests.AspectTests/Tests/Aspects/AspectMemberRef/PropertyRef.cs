// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.AspectMemberRef.PropertyRef
{
    public class RetryAttribute : OverrideMethodAspect
    {
        public int Property { get; set; } = 5;

        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( Property );

            return meta.Proceed();
        }
    }

    internal class Program
    {
        // <target>
        [Retry( Property = 10 )]
        private static int Foo()
        {
            return 0;
        }
    }
}