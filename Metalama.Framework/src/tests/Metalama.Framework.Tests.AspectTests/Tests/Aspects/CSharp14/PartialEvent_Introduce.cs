// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
// @Skipped(https://github.com/metalama/Metalama/issues/1112)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialEvent_Introduce;

public class TheAspect : TypeAspect
{
    [Introduce]
    public event Action E
    {
        add { Console.WriteLine( "Add" ); }
        remove { Console.WriteLine( "Remove" ); }
    }
}

[TheAspect]
internal partial class C
{
#if TESTRUNNER
    public partial event Action E;
#endif
}

#endif