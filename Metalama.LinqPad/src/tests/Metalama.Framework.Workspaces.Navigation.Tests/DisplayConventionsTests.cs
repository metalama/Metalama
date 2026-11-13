// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Workspaces.Navigation.Facade;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Workspaces.Navigation.Tests;

public sealed class DisplayConventionsTests : UnitTestClass
{
    [Fact]
    public void HiddenInterfacePropertiesAreNotShown()
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilation( "class C { void M() {} }" );

        var compilationType = FacadeTypeFactory.Default.GetFacadeType( compilation.GetType() );
        Assert.DoesNotContain( nameof(ICompilation.Comparers), compilationType.PropertyNames );
        Assert.DoesNotContain( nameof(ICompilation.Cache), compilationType.PropertyNames );
        Assert.Contains( nameof(ICompilation.Types), compilationType.PropertyNames );

        var method = compilation.Types.Single().Methods.Single();
        var methodType = FacadeTypeFactory.Default.GetFacadeType( method.GetType() );
        Assert.DoesNotContain( nameof(IDeclaration.Depth), methodType.PropertyNames );
        Assert.Contains( nameof(IMethod.ReturnType), methodType.PropertyNames );
        Assert.False( methodType.TryGetProperty( nameof(IDeclaration.Depth), out _ ) );
    }

    [Fact]
    public void PropertiesAreOrderedByPriority()
    {
        var type = FacadeTypeFactory.Default.GetFacadeType( typeof(Ordered) );

        Assert.Equal( ["Id", "Name", "Zeta", "Items"], type.PropertyNames.ToArray() );
    }

    [Fact]
    public void AnonymousTypeKeepsDeclarationOrder()
    {
        var type = FacadeTypeFactory.Default.GetFacadeType( new { Zeta = 1, Name = "n", Id = 2 }.GetType() );

        Assert.Equal( ["Zeta", "Name", "Id"], type.PropertyNames.ToArray() );
    }

    public sealed class Ordered
    {
        public int Zeta { get; } = 1;

        public string Name { get; } = "n";

        public int Id { get; } = 2;

        public int[] Items { get; } = [1];
    }

    [Fact]
    public void AnonymousTypeExposesItsProperties()
    {
        var type = FacadeTypeFactory.Default.GetFacadeType( new { Id = 1, Name = "name" }.GetType() );

        Assert.Contains( "Id", type.PropertyNames );
        Assert.Contains( "Name", type.PropertyNames );
    }
}
