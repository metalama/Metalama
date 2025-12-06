// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.IntroducedMemberReference;

// This test verifies that when generating input HTML, the design-time partial file
// (containing introduced member declarations) is included in the compilation.
// Without the design-time partial, this would cause CS1061: 'C' does not contain a definition for 'IntroducedMethod'.

public class IntroduceMethodAspect : TypeAspect
{
    [Introduce]
    public void IntroducedMethod()
    {
        Console.WriteLine( "Introduced" );
    }
}

[IntroduceMethodAspect]
public partial class C
{
    public void ExistingMethod()
    {
        // Call to an introduced method - requires the design-time partial to compile.
#if TESTRUNNER
        this.IntroducedMethod();
#endif
    }
}