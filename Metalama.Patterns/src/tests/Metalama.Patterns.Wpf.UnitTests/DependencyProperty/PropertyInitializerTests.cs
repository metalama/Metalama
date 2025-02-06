// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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