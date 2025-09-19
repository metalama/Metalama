// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.


using Metalama.Framework.Aspects;
using System.Collections.Immutable;

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32522;

[CompileTime]
internal class C
{
    private void M()
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        builder.Add( "");
        builder.ToImmutable();
    }
}