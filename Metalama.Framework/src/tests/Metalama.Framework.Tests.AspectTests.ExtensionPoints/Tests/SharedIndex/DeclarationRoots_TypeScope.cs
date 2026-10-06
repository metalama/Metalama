// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.ExtensionPoints;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.SharedIndex.DeclarationRoots_TypeScope;

// The only consumer gives the target type as the declaration root, so the index covers only that type. The call in D is not indexed.

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder ) => builder.TestReportReferences( "F", restrictToTarget: true );
}

internal static class A
{
    public static int F() => 0;
}

[TheAspect]
internal class C
{
    public int Value = A.F();

    private void M() => A.F();
}

internal class D
{
    private void M() => A.F();
}
