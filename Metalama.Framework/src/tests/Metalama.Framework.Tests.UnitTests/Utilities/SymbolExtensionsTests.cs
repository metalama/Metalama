// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

public sealed class SymbolExtensionsTests : UnitTestClass
{
    [Fact]
    public void GetPrimarySyntaxReference_RegularMethod()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M() { }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var type = compilation.GetTypeByMetadataName( "C" ).AssertNotNull();
        var method = type.GetMembers( "M" ).Single();

        var syntaxRef = method.GetPrimarySyntaxReference();

        Assert.NotNull( syntaxRef );
        Assert.Contains( "void M()", syntaxRef.GetSyntax().ToString(), StringComparison.Ordinal );
    }

    [Fact]
    public void GetPrimarySyntaxReference_PartialMethod_ReturnsImplementation()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            partial class C
                            {
                                partial void M();
                            }

                            partial class C
                            {
                                partial void M() { }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var type = compilation.GetTypeByMetadataName( "C" ).AssertNotNull();
        var method = type.GetMembers( "M" ).Single();

        var syntaxRef = method.GetPrimarySyntaxReference();

        Assert.NotNull( syntaxRef );

        // The implementation part contains the body
        Assert.Contains( "{ }", syntaxRef.GetSyntax().ToString(), StringComparison.Ordinal );
    }

#if ROSLYN_4_12_0_OR_GREATER
    [Fact]
    public void GetPrimarySyntaxReference_PartialProperty_ReturnsImplementation()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            partial class C
                            {
                                public partial int P { get; }
                            }

                            partial class C
                            {
                                public partial int P => 42;
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var type = compilation.GetTypeByMetadataName( "C" ).AssertNotNull();
        var property = type.GetMembers( "P" ).Single();

        var syntaxRef = property.GetPrimarySyntaxReference();

        Assert.NotNull( syntaxRef );

        // The implementation part contains the expression body
        Assert.Contains( "=> 42", syntaxRef.GetSyntax().ToString(), StringComparison.Ordinal );
    }
#endif

#if ROSLYN_5_0_0_OR_GREATER
    [Fact]
    public void GetPrimarySyntaxReference_PartialEvent_ReturnsImplementation()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            using System;

                            partial class C
                            {
                                public partial event EventHandler E;
                            }

                            partial class C
                            {
                                public partial event EventHandler E
                                {
                                    add { }
                                    remove { }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var type = compilation.GetTypeByMetadataName( "C" ).AssertNotNull();
        var @event = type.GetMembers( "E" ).Single();

        var syntaxRef = @event.GetPrimarySyntaxReference();

        Assert.NotNull( syntaxRef );

        // The implementation part contains the accessors
        Assert.Contains( "add { }", syntaxRef.GetSyntax().ToString(), StringComparison.Ordinal );
    }
#endif

    [Fact]
    public void GetPrimarySyntaxReference_NullSymbol_ReturnsNull()
    {
        var syntaxRef = SymbolExtensions.GetPrimarySyntaxReference( null );

        Assert.Null( syntaxRef );
    }

    [Fact]
    public void GetPrimarySyntaxReference_Accessor_ReturnsSyntaxReference()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                int P { get; set; }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var type = compilation.GetTypeByMetadataName( "C" ).AssertNotNull();
        var getter = type.GetMembers( "get_P" ).Single();

        var getterSyntaxRef = getter.GetPrimarySyntaxReference();

        Assert.NotNull( getterSyntaxRef );

        // The accessor should have a valid syntax reference
        Assert.Contains( "get", getterSyntaxRef.GetSyntax().ToString(), StringComparison.Ordinal );
    }

    [Fact]
    public void GetPrimaryDeclarationSyntax_ReturnsNode()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M() { }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var type = compilation.GetTypeByMetadataName( "C" ).AssertNotNull();
        var method = type.GetMembers( "M" ).Single();

        var syntaxNode = method.GetPrimaryDeclarationSyntax();

        Assert.NotNull( syntaxNode );
        Assert.Contains( "void M()", syntaxNode.ToString(), StringComparison.Ordinal );
    }
}
