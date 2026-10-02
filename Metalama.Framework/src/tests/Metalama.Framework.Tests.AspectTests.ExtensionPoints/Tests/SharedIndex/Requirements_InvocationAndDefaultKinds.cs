// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.SharedIndex.Requirements_InvocationAndDefaultKinds;

// The extension requests the invocations and the default references of the methods named F, and reads them from the shared index of the
// stage. The method group converted to a delegate is a default reference. The calls of G are not indexed.

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => builder.TestReportReferences( "F" );
}

internal static class A
{
    public static int F() => 0;

    public static int G() => 0;
}

// <target>
[TheAspect]
internal class C
{
    private void M()
    {
        A.F();
        A.G();
        Func<int> f = A.F;
    }
}

internal class D
{
    public int Value = A.F();
}
