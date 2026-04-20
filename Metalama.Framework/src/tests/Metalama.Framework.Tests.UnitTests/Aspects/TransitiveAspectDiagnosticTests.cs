// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Aspects;
using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

public sealed class TransitiveAspectDiagnosticTests
{
    [Fact]
    public void ToAspectInstance_ReturnsNull_WhenAspectClassNotFound()
    {
        // Create a SerializableTransitiveAspectInstance with a bogus aspect class name
        // that won't be found by the resolver.
        var instance = new SerializableTransitiveAspectInstance(
            new DummyAspect(),
            "NonExistent.AspectClass",
            null,
            null!,
            0 );

        var resolver = new EmptyAspectClassResolver();

        // ToAspectInstance should return null when the aspect class is not found.
        // This is the code path that triggers LAMA0072 in TransitiveAspectPipelineExtension.
        var result = instance.ToAspectInstance( resolver );

        Assert.Null( result );
    }

    [Fact]
    public void AspectClassName_IsPreserved()
    {
        const string expectedClassName = "Some.Old.Version.TransitiveAspect";

        var instance = new SerializableTransitiveAspectInstance(
            new DummyAspect(),
            expectedClassName,
            null,
            null!,
            0 );

        Assert.Equal( expectedClassName, instance.AspectClassName );
    }

    private class EmptyAspectClassResolver : IAspectClassResolver
    {
        public bool TryGetAspectClass( Type aspectType, [NotNullWhen( true )] out IAspectClass? aspectClass )
        {
            aspectClass = null;

            return false;
        }

        public bool TryGetAspectClass( string fullName, [NotNullWhen( true )] out IAspectClass? aspectClass )
        {
            aspectClass = null;

            return false;
        }
    }

    private class DummyAspect : IAspect;
}