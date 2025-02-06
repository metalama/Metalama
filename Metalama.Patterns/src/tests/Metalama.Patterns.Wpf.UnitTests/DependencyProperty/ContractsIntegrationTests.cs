// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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