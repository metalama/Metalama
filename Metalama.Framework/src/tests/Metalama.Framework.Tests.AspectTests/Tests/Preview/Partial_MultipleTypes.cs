// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @TestScenario(Preview)
// @TargetSyntaxTreeSuffix(YetAnother)
#endif

using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Preview.Partial_MultipleTypes;

internal class TestAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Transformed" );

        return meta.Proceed();
    }
}

internal partial class TargetClass
{
    partial class NestedClass1
    {
        [TestAspect]
        public void Foo() { }
    }
}