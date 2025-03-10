// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RemoveOutputCode
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32522;

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System.Collections.Immutable;


[CompileTime]
class C
{
    void M()
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        builder.Add( "");
        builder.ToImmutable();
    }
}