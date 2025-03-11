// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using FluentAssertions;
using Metalama.Patterns.Wpf.UnitTests.Assets.DependencyPropertyNS;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Metalama.Patterns.Wpf.UnitTests.DependencyPropertyNS;

public sealed class PropertyInitializerTests
{
    [Fact]
    public void DefaultConfiguration()
    {
        PropertyInitializerTestClass.DefaultConfigurationProperty.DefaultMetadata.DefaultValue
            .Should()
            .Be( 42 );

        PropertyInitializerTestClass.DefaultConfigurationInitializerCallCount.Should().Be( 1 );
        var instance = new PropertyInitializerTestClass();
        instance.DefaultConfiguration.Should().Be( 42 );
        
        PropertyInitializerTestClass.DefaultConfigurationInitializerCallCount.Should().Be( 2 );
    }
}