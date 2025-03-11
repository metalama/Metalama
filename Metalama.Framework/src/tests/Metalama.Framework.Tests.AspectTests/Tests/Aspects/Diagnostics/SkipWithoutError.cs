// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Diagnostics.SkipWithoutError
{
    public class SkippedAttribute : OverrideMethodAspect
    {
        public override void BuildAspect( IAspectBuilder<IMethod> builder )
        {
            base.BuildAspect( builder );

            builder.SkipAspect();
        }

        public override dynamic? OverrideMethod()
        {
            throw new NotImplementedException( "This code should not be emitted." );
        }
    }

    // <target>
    internal class TargetClass
    {
        [Skipped]
        public static int Add( int a, int b )
        {
            if (a == 0)
            {
                throw new ArgumentOutOfRangeException( nameof(a) );
            }

            return a + b;
        }
    }
}