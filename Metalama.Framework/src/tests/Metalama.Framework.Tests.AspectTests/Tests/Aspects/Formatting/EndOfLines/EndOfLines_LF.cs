// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @ExpectedEndOfLine(LF)
// @Skipped
#endif

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.EndOfLines_LF;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(TestAspect1), typeof(TestAspect2) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.EndOfLines_LF
{
    public class TestAspect1 : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            meta.InsertStatement( "Console.WriteLine(\"Hello!\");\r" );

            return meta.Proceed();
        }
    }

    public class TestAspect2 : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            meta.InsertStatement( "Console.WriteLine(\"Hello!\");\r\n" );

            return meta.Proceed();
        }
    }

    // <target>
    public class Target
    {
        [TestAspect1]
        [TestAspect2]
        private static int Add( int a, int b )
        {
            Console.WriteLine( "Thinking..." );

            return a + b;
        }
    }
}