// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Xunit;

namespace Metalama.Framework.Engine.Analyzers.Tests;

public class KindCheckOptimizationAnalyzerTests
{
    #region Test Infrastructure

    private static readonly MetadataReference[] _references;

    static KindCheckOptimizationAnalyzerTests()
    {
        var references = new List<MetadataReference>();

        // Core .NET references
        var runtimeDir = Path.GetDirectoryName( typeof( object ).Assembly.Location )!;

        references.Add( MetadataReference.CreateFromFile( typeof( object ).Assembly.Location ) );
        references.Add( MetadataReference.CreateFromFile( Path.Combine( runtimeDir, "netstandard.dll" ) ) );
        references.Add( MetadataReference.CreateFromFile( Path.Combine( runtimeDir, "System.Runtime.dll" ) ) );
        references.Add( MetadataReference.CreateFromFile( Path.Combine( runtimeDir, "System.Collections.dll" ) ) );
        references.Add( MetadataReference.CreateFromFile( Path.Combine( runtimeDir, "System.Linq.dll" ) ) );

        // Microsoft.CodeAnalysis references (for ISymbol, SyntaxNode)
        references.Add( MetadataReference.CreateFromFile( typeof( SyntaxNode ).Assembly.Location ) );
        references.Add( MetadataReference.CreateFromFile( typeof( CSharpSyntaxNode ).Assembly.Location ) );

        // Metalama.Framework reference (for IDeclaration)
        references.Add( MetadataReference.CreateFromFile( typeof( Metalama.Framework.Code.IDeclaration ).Assembly.Location ) );

        _references = references.ToArray();
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync( string code )
    {
        var parseOptions = new CSharpParseOptions( LanguageVersion.CSharp12 );
        var syntaxTree = CSharpSyntaxTree.ParseText( code, parseOptions );

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            _references,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        var analyzer = new KindCheckOptimizationAnalyzer();

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>( analyzer ) );

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        return diagnostics.Where( d => d.Id == "LAMA0860" ).ToImmutableArray();
    }

    private static async Task AssertDiagnosticCountAsync( string code, int expectedCount )
    {
        var diagnostics = await GetDiagnosticsAsync( code );

        Assert.Equal( expectedCount, diagnostics.Length );
    }

    #endregion

    #region Is-Pattern Tests - True Positives

    [Fact]
    public async Task IsPattern_IDeclaration_NoKindCheck_ShouldWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl)
                {
                    if (decl is IMethod method) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 1 );
    }

    [Fact]
    public async Task IsPattern_ISymbol_NoKindCheck_ShouldWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            class Test
            {
                void M(ISymbol symbol)
                {
                    if (symbol is INamedTypeSymbol namedType) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 1 );
    }

    [Fact]
    public async Task IsPattern_SyntaxNode_NoKindCheck_ShouldWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            class Test
            {
                void M(SyntaxNode node)
                {
                    if (node is ClassDeclarationSyntax classDecl) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 1 );
    }

    [Fact]
    public async Task IsPattern_RecursivePattern_NoKindCheck_ShouldWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl)
                {
                    if (decl is IMethod { Name: "X" } method) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 1 );
    }

    #endregion

    #region Is-Pattern Tests - False Positives (Should Not Warn)

    [Fact]
    public async Task IsPattern_PrecedingKindCheck_Equality_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl)
                {
                    if (decl.DeclarationKind == DeclarationKind.Method && decl is IMethod method) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task IsPattern_PrecedingKindCheck_ISymbol_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            class Test
            {
                void M(ISymbol symbol)
                {
                    if (symbol.Kind == SymbolKind.NamedType && symbol is INamedTypeSymbol namedType) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task IsPattern_PrecedingKindCheck_IsKind_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            class Test
            {
                void M(SyntaxNode node)
                {
                    if (node.IsKind(SyntaxKind.ClassDeclaration) && node is ClassDeclarationSyntax classDecl) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task IsPattern_PrecedingKindCheck_ConditionalIsKind_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            class Test
            {
                void M(SyntaxNode? node)
                {
                    if (node?.IsKind(SyntaxKind.ClassDeclaration) == true && node is ClassDeclarationSyntax classDecl) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task IsPattern_KindPropertyPattern_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl)
                {
                    if (decl is IMethod { DeclarationKind: DeclarationKind.Method } method) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task IsPattern_AbstractSyntaxNode_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            class Test
            {
                void M(SyntaxNode node)
                {
                    if (node is ExpressionSyntax expr) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task IsPattern_NotRelevantBaseType_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(object obj)
                {
                    if (obj is IMethod method) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task IsPattern_KindIdentifierVariable_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl, DeclarationKind validatedKind)
                {
                    if (validatedKind == DeclarationKind.Method && decl is IMethod method) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    #endregion

    #region Switch Statement Tests - True Positives

    [Fact]
    public async Task SwitchStatement_IDeclaration_NoKindCheck_ShouldWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl)
                {
                    switch (decl)
                    {
                        case IMethod method:
                            break;
                    }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 1 );
    }

    [Fact]
    public async Task SwitchStatement_ISymbol_MultipleArms_ShouldWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            class Test
            {
                void M(ISymbol symbol)
                {
                    switch (symbol)
                    {
                        case IFieldSymbol field:
                            break;
                        case IPropertySymbol property:
                            break;
                    }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 2 );
    }

    #endregion

    #region Switch Statement Tests - False Positives (Should Not Warn)

    [Fact]
    public async Task SwitchStatement_GoverningKindAccess_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl)
                {
                    switch (decl.DeclarationKind)
                    {
                        case DeclarationKind.Method:
                            break;
                    }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task SwitchStatement_CasePatternWithKindCheck_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl)
                {
                    switch (decl)
                    {
                        case IMethod { DeclarationKind: DeclarationKind.Method } method:
                            break;
                    }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    #endregion

    #region Switch Expression Tests - True Positives

    [Fact]
    public async Task SwitchExpression_ISymbol_NoKindCheck_ShouldWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            class Test
            {
                int M(ISymbol symbol) => symbol switch
                {
                    IFieldSymbol => 1,
                    _ => 0
                };
            }
            """;

        await AssertDiagnosticCountAsync( code, 1 );
    }

    [Fact]
    public async Task SwitchExpression_SyntaxNode_MultipleArms_ShouldWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            class Test
            {
                int M(SyntaxNode node) => node switch
                {
                    ClassDeclarationSyntax => 1,
                    MethodDeclarationSyntax => 2,
                    _ => 0
                };
            }
            """;

        await AssertDiagnosticCountAsync( code, 2 );
    }

    #endregion

    #region Switch Expression Tests - False Positives (Should Not Warn)

    [Fact]
    public async Task SwitchExpression_GoverningKindAccess_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            class Test
            {
                int M(ISymbol symbol) => symbol.Kind switch
                {
                    SymbolKind.Field => 1,
                    _ => 0
                };
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task SwitchExpression_ArmWithKindPropertyPattern_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                int M(IDeclaration decl) => decl switch
                {
                    IMethod { DeclarationKind: DeclarationKind.Method } => 1,
                    _ => 0
                };
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    #endregion

    #region Nested Context Tests - False Positives (Should Not Warn)

    [Fact]
    public async Task IsPattern_InsideSwitchOnKind_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl)
                {
                    switch (decl.DeclarationKind)
                    {
                        case DeclarationKind.Method:
                            if (decl is IMethod method) { }
                            break;
                    }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task IsPattern_InsideSwitchExpressionArmWithKindPattern_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                bool M(IDeclaration decl) => decl.DeclarationKind switch
                {
                    DeclarationKind.Method when decl is IMethod method => true,
                    _ => false
                };
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public async Task NoWarning_TypeNotInHierarchy_ShouldNotWarn()
    {
        var code = """
            interface IUnrelated {}
            class Test
            {
                void M(object obj)
                {
                    if (obj is IUnrelated unrelated) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task NoWarning_MissingReferences_ShouldNotWarn()
    {
        // Compilation without Metalama references - should not crash
        var code = """
            class Test
            {
                void M(object obj)
                {
                    if (obj is string s) { }
                }
            }
            """;

        // Create a minimal compilation without Metalama references
        var syntaxTree = CSharpSyntaxTree.ParseText( code );
        var runtimeDir = Path.GetDirectoryName( typeof( object ).Assembly.Location )!;

        var minimalReferences = new[]
        {
            MetadataReference.CreateFromFile( typeof( object ).Assembly.Location ),
            MetadataReference.CreateFromFile( Path.Combine( runtimeDir, "System.Runtime.dll" ) )
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            minimalReferences,
            new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        var analyzer = new KindCheckOptimizationAnalyzer();

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>( analyzer ) );

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        var lama0860Diagnostics = diagnostics.Where( d => d.Id == "LAMA0860" ).ToImmutableArray();

        Assert.Empty( lama0860Diagnostics );
    }

    [Fact]
    public async Task MultiplePatterns_SameFile_ShouldWarnMultiple()
    {
        var code = """
            using Metalama.Framework.Code;
            using Microsoft.CodeAnalysis;
            class Test
            {
                void M(IDeclaration decl, ISymbol symbol)
                {
                    if (decl is IMethod method) { }
                    if (symbol is IFieldSymbol field) { }
                    if (decl is IProperty property) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 3 );
    }

    [Fact]
    public async Task TupleKindAccess_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            class Test
            {
                int M(ISymbol left, ISymbol right) => (left.Kind, right.Kind) switch
                {
                    (SymbolKind.Field, SymbolKind.Field) => 1,
                    _ => 0
                };
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task ParenthesizedKindAccess_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            class Test
            {
                int M(ISymbol symbol) => ((symbol.Kind)) switch
                {
                    SymbolKind.Field => 1,
                    _ => 0
                };
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    #endregion

    #region Session-Specific False Positives (Regression Tests)

    [Fact]
    public async Task FalsePositive_NestedPropertyPattern_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            class Wrapper { public ISymbol Symbol { get; set; } }
            class Test
            {
                void M(Wrapper wrapper)
                {
                    if (wrapper is { Symbol.Kind: SymbolKind.Field }) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task FalsePositive_KindInIdentifierName_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl, DeclarationKind validatedDeclarationKind)
                {
                    if (validatedDeclarationKind == DeclarationKind.Method && decl is IMethod method) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task FalsePositive_SyntaxKindMethod_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            class Test
            {
                void M(SyntaxNode node)
                {
                    var kind = node.Kind();
                    if (kind == SyntaxKind.ClassDeclaration && node is ClassDeclarationSyntax classDecl) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task FalsePositive_MethodStartsWithIs_InWhenClause_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            static class SymbolKindExtensions
            {
                public static bool IsFieldOrProperty(this SymbolKind kind) => kind == SymbolKind.Field || kind == SymbolKind.Property;
            }
            class Test
            {
                void M(ISymbol symbol)
                {
                    switch (symbol.Kind)
                    {
                        case var kind when kind.IsFieldOrProperty():
                            if (symbol is IFieldSymbol field) { }
                            break;
                    }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    #endregion

    #region Additional Edge Cases

    [Fact]
    public async Task IsPattern_WithNegation_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl)
                {
                    if (!(decl.DeclarationKind != DeclarationKind.Method) && decl is IMethod method) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task IsPattern_IsKindPattern_ShouldNotWarn()
    {
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl)
                {
                    if (decl.DeclarationKind is DeclarationKind.Method && decl is IMethod method) { }
                }
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task SwitchExpression_OnKindMethod_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            class Test
            {
                int M(SyntaxNode node) => node.Kind() switch
                {
                    SyntaxKind.ClassDeclaration => 1,
                    _ => 0
                };
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    [Fact]
    public async Task SwitchExpression_ConditionalKindAccess_ShouldNotWarn()
    {
        var code = """
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.CSharp;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            class Test
            {
                int M(SyntaxNode? node) => node?.Kind() switch
                {
                    SyntaxKind.ClassDeclaration => 1,
                    _ => 0
                };
            }
            """;

        await AssertDiagnosticCountAsync( code, 0 );
    }

    #endregion

    #region Regression Tests

    [Fact]
    public async Task SwitchExpression_ConstantPatternForType_ShouldWarn()
    {
        // Verifies that ConstantPatternSyntax that resolves to a type is handled correctly
        var code = """
            using Microsoft.CodeAnalysis;
            class Test
            {
                int M(ISymbol symbol) => symbol switch
                {
                    IFieldSymbol => 1,
                    _ => 0
                };
            }
            """;

        var diagnostics = await GetDiagnosticsAsync( code );

        // We expect 1 LAMA0860 warning for IFieldSymbol pattern
        Assert.Single( diagnostics );
        Assert.Equal( "LAMA0860", diagnostics[0].Id );
    }

    [Fact]
    public async Task IsPattern_OldStyleIsTypeCheck_ShouldNotWarn()
    {
        // Test: decl.DeclarationKind is DeclarationKind.Method (old-style IsExpression)
        // This pattern is parsed as BinaryExpressionSyntax with SyntaxKind.IsExpression, not IsPatternExpressionSyntax
        var code = """
            using Metalama.Framework.Code;
            class Test
            {
                void M(IDeclaration decl)
                {
                    if (decl.DeclarationKind is DeclarationKind.Method && decl is IMethod method) { }
                }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync( code );

        // Should be 0 because there's a preceding Kind check
        Assert.Empty( diagnostics );
    }

    #endregion
}
