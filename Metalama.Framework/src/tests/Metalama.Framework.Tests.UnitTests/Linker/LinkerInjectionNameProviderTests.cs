// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Linking;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Linker;

/// <summary>
/// Tests of <see cref="LinkerInjectionNameProvider"/>.
/// </summary>
public sealed class LinkerInjectionNameProviderTests : UnitTestClass
{
    /// <summary>
    /// Verifies that a name hint that collides with members of the final compilation is suffixed with the first counter value that collides
    /// with no member. The provider used to test the hint instead of the candidate, so it never found a free name.
    /// </summary>
    [Fact]
    public void NameCollision_ExistingHint()
    {
        using var testContext = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M() {}
                                void M_Aspect() {}
                                void M_Aspect2() {}
                            }
                            """;

        var compilation = testContext.CreateCompilationModel( code );
        var type = compilation.Types.OfName( "C" ).Single();
        var method = type.Methods.OfName( "M" ).Single();

        var nameProvider = new LinkerInjectionNameProvider( compilation, new LinkerInjectionHelperProvider( compilation, false ) );
        var aspectLayer = new AspectLayerId( "AspectAttribute" );

        Assert.Equal( "M_Aspect3", nameProvider.GetOverrideName( type, aspectLayer, method ) );
        Assert.Equal( "M_Aspect4", nameProvider.GetOverrideName( type, aspectLayer, method ) );
    }

    /// <summary>
    /// Verifies that a name hint that collides with no member is returned unchanged, and that the next request for the same hint is suffixed.
    /// </summary>
    [Fact]
    public void NoCollision_HintReturned()
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "class C { void M() {} }" );
        var type = compilation.Types.OfName( "C" ).Single();
        var method = type.Methods.OfName( "M" ).Single();

        var nameProvider = new LinkerInjectionNameProvider( compilation, new LinkerInjectionHelperProvider( compilation, false ) );
        var aspectLayer = new AspectLayerId( "AspectAttribute" );

        Assert.Equal( "M_Aspect", nameProvider.GetOverrideName( type, aspectLayer, method ) );
        Assert.Equal( "M_Aspect1", nameProvider.GetOverrideName( type, aspectLayer, method ) );
    }
}
