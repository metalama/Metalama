// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
// @Skipped(https://github.com/metalama/Metalama/issues/1113)
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.PartialEvent_Override;

public class TheAspect : OverrideEventAspect
{
    public override void OverrideAdd( dynamic handler ) => Console.WriteLine( "Add" );

    public override void OverrideRemove( dynamic handler ) => Console.WriteLine( "Remove" );

    public override dynamic? OverrideInvoke( dynamic handler )
    {
        Console.WriteLine( "Invoke" );

        return meta.Proceed();
    }
}

internal partial class C
{
#if TESTRUNNER
    [TheAspect]
    public partial event Action E;
#endif
}

#endif