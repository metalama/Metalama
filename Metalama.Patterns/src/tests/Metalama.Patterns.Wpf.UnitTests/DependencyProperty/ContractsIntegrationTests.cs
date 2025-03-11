// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using FluentAssertions;
using Metalama.Patterns.Wpf.UnitTests.Assets.DependencyPropertyNS;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Metalama.Patterns.Wpf.UnitTests.DependencyPropertyNS;

public sealed class ContractsIntegrationTests
{
    [Fact]
    public void TrimAndNotNull()
    {
        var t = new ContractsIntegrationTestClass();

        t.Operations.Should().BeEmpty();

        t.Name = "Tom";

        t.Operations.Should().Equal( "ValidateName|Tom", "OnNameChanged||Tom" );
        t.Name.Should().Be( "Tom" );
        t.Operations.Clear();

        t.Name = "  gael  ";
        t.Operations.Should().Equal( "ValidateName|gael", "OnNameChanged|Tom|gael" );
        t.Name.Should().Be( "gael" );
        t.Operations.Clear();

        t.Invoking( v => v.Name = null! ).Should().Throw<ArgumentNullException>();
        t.Operations.Should().BeEmpty();
        t.Name.Should().Be( "gael" );
    }
}