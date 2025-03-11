// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Collections;

public sealed class LinqExtensionsTests
{
    [Fact]
    public void ImmutableArrayMin()
    {
        Assert.Equal( 1, ImmutableArray.Create( 1, 2, 3 ).Min() );
        Assert.Equal( 1, ImmutableArray.Create( 1 ).Min() );
    }

    [Fact]
    public void ImmutableArrayMax()
    {
        Assert.Equal( 3, ImmutableArray.Create( 1, 2, 3 ).Max() );
        Assert.Equal( 1, ImmutableArray.Create( 1 ).Max() );
    }
}