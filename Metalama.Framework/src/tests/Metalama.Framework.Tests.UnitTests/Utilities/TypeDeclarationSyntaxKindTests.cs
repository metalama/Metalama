// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests of the syntax kind lists that decide whether a node is a type declaration. See issue #1941.
/// </summary>
/// <remarks>
/// <para>
/// A diagnostic located on the header of a type declaration, that is on its identifier, its modifiers, its base list
/// or its parameter list, has no member declaration below that header. The diagnostic suppressor and the scoped
/// suppression matcher therefore call <c>SyntaxExtensions.FindSymbolDeclaringNode</c> and walk upwards from
/// the node that it returns. When the kind of the header is missing from the list of that method, the walk passes
/// over the type and a suppression registered on it is never matched, which fails silently.
/// </para>
/// <para>
/// These tests pin the answer of that method, and of <c>GetDeclaringType</c>, for the kinds that the lists were
/// missing.
/// </para>
/// </remarks>
public sealed class TypeDeclarationSyntaxKindTests
{
    private static SyntaxNode ParseSingleTypeDeclaration( string code, LanguageVersion languageVersion )
    {
        var tree = CSharpSyntaxTree.ParseText( code, CSharpParseOptions.Default.WithLanguageVersion( languageVersion ) );

        Assert.DoesNotContain( tree.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error );

        return tree.GetRoot();
    }

    /// <summary>
    /// Verifies that the declaring node of the base list of an interface is the interface declaration, and not the
    /// enclosing namespace.
    /// </summary>
    [Fact]
    public void DeclaringNodeOfInterfaceHeaderIsTheInterface()
    {
        const string code = """
                            namespace Ns
                            {
                                public interface IBase;

                                public interface IInterface : IBase;
                            }
                            """;

        var root = ParseSingleTypeDeclaration( code, SupportedCSharpVersions.Latest );

        var declaration = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Single( t => t.Identifier.Text == "IInterface" );
        var header = declaration.BaseList!;

        Assert.Same( declaration, header.FindSymbolDeclaringNode() );
        Assert.Same( declaration, header.GetDeclaringType() );
        Assert.True( declaration.SyntaxKind.IsTypeDeclaration );
    }

    /// <summary>
    /// Verifies that the declaring node of the receiver parameter of an extension block is the extension block, and
    /// not the enclosing static class. The extension block kind was missing from all four lists.
    /// </summary>
    [Fact]
    public void DeclaringNodeOfExtensionBlockHeaderIsTheExtensionBlock()
    {
        const string code = """
                            public static class Extensions
                            {
                                extension( int value )
                                {
                                    public int Doubled => value * 2;
                                }
                            }
                            """;

        var root = ParseSingleTypeDeclaration( code, SupportedCSharpVersions.Latest );

        var declaration = root.DescendantNodes().OfType<ExtensionBlockDeclarationSyntax>().Single();
        var header = declaration.ParameterList!;

        Assert.Same( declaration, header.FindSymbolDeclaringNode() );
        Assert.Same( declaration, header.GetDeclaringType() );

        // The extension block stays out of IsTypeDeclaration, which is the narrower predicate of the type
        // declarations that declare a type of their own.
        Assert.False( declaration.SyntaxKind.IsTypeDeclaration );
    }

#if ROSLYN_5_11_0_OR_GREATER

    // The union kind exists in the latest Roslyn variant only, and the engine names it only under the opt-in of
    // eng/RoslynPreview.props, for the reason explained in section 6 of
    // Metalama.Framework/docs/2027.0/DECISIONS.md.

    /// <summary>
    /// Verifies that the declaring node of the case list of a union is the union declaration, and not the enclosing
    /// namespace. The case types of a union are parsed into the parameter list of the union header, so a diagnostic
    /// reported on a case type is the case that fails today.
    /// </summary>
    [Fact]
    public void DeclaringNodeOfUnionHeaderIsTheUnion()
    {
        const string code = """
                            namespace Ns
                            {
                                public union Shape( Circle, Rectangle );

                                public record Circle;

                                public record Rectangle;
                            }
                            """;

        var root = ParseSingleTypeDeclaration( code, SupportedCSharpVersions.Latest );

        var declaration = root.DescendantNodes().OfType<UnionDeclarationSyntax>().Single();
        var header = declaration.ParameterList!;

        Assert.Same( declaration, header.FindSymbolDeclaringNode() );
        Assert.Same( declaration, header.GetDeclaringType() );
        Assert.True( declaration.SyntaxKind.IsTypeDeclaration );
    }

#endif
}
