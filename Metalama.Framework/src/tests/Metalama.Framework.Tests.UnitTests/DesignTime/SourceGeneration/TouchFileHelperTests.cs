// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.SourceGeneration;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.SourceGeneration;

public sealed class TouchFileHelperTests
{
    [Fact]
    public void MarkerFullTypeName_Format()
    {
        Assert.Equal( "Metalama.Internal", TouchFileHelper.MarkerNamespace );
        Assert.Equal( "MetalamaSourceGeneratorMarker", TouchFileHelper.MarkerTypeName );
        Assert.Equal( "Metalama.Internal.MetalamaSourceGeneratorMarker", TouchFileHelper.MarkerFullTypeName );
        Assert.Equal( "TouchId", TouchFileHelper.MarkerFieldName );
    }
}
