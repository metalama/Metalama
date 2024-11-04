// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Accessibility = Metalama.Framework.Code.Accessibility;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// Rewrites a run-time syntax tree so that the implementation of compile-time-only methods is replaced
/// by a <c>throw new NotSupportedException()</c>.
/// </summary>
internal sealed class RunTimeAssemblyRewriter : SafeSyntaxRewriter
{
    private const string _intrinsics = @"
using System;

namespace Metalama.Compiler
{
    internal static class Intrinsics
    {
        public static RuntimeMethodHandle GetRuntimeMethodHandle(string documentationId) => throw new InvalidOperationException(""Code calling this method has to be compiled by the Metalama compiler."");
        public static RuntimeFieldHandle GetRuntimeFieldHandle(string documentationId) => throw new InvalidOperationException(""Code calling this method has to be compiled by the Metalama compiler."");
        public static RuntimeTypeHandle GetRuntimeTypeHandle(string documentationId) => throw new InvalidOperationException(""Code calling this method has to be compiled by the Metalama compiler."");
    }
}
";

    /// <summary>
    /// List of warnings that are suppressed from the run-time code of aspects.
    /// </summary>
    private static readonly SeparatedSyntaxList<ExpressionSyntax> _suppressedWarnings = SeparatedList<ExpressionSyntax>(
        new[]
        {
            // An event was declared but never used in the class in which it was declared.
            IdentifierName( "CS0067" ),

            // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            IdentifierName( "CS8618" ),

            // The compiler detected code that will never be executed.
            IdentifierName( "CS0162" ),

            // The private field is never used.
            IdentifierName( "CS0169" ),

            // The private field 'field' is assigned but its value is never used.
            IdentifierName( "CS0414" ),

            // Can be made static.
            IdentifierName( "CA1822" ),

            // Avoid unused private fields
            IdentifierName( "CA1823" ),

            // Private member 'x' is unused.
            IdentifierName( "IDE0051" ),

            // Private member 'x' can be removed as the value assigned to it is never read.
            IdentifierName( "IDE0052" )
        } );

    // TODO: We can do more in cleaning the run-time assembly. 
    // Private compile-time code can be stripped, except when they are templates, because their metadata must be preserved.
    // In general, accessible compile-time metadata must remain.

    private static readonly Lazy<SyntaxTree> _intrinsicsSyntaxTree =
        new( () => CSharpSyntaxTree.ParseText( _intrinsics, SupportedCSharpVersions.DefaultParseOptions, "@@Intrinsics.cs", Encoding.UTF8 ) );

    private readonly INamedTypeSymbol? _aspectDriverSymbol;
    private readonly bool _removeCompileTimeOnlyCode;
    private readonly RewriterHelper _rewriterHelper;
    private readonly IEqualityComparer<ISymbol> _symbolEqualityComparer;
    private readonly ClassifyingCompilationContext _compilationContext;
    private readonly SyntaxGenerationOptions _syntaxGenerationOptions;
    private readonly SyntaxGenerationContext _syntaxGenerationContext;

    private RunTimeAssemblyRewriter( in ProjectServiceProvider serviceProvider, ClassifyingCompilationContext compilationContext, SyntaxTree syntaxTree )
    {
        this._syntaxGenerationOptions = serviceProvider.GetRequiredService<SyntaxGenerationOptions>();
        this._compilationContext = compilationContext;
        this._rewriterHelper = new RewriterHelper( compilationContext );
        this._aspectDriverSymbol = compilationContext.SourceCompilation.GetTypeByMetadataName( typeof(IAspectDriver).FullName.AssertNotNull() );
        this._removeCompileTimeOnlyCode = serviceProvider.GetRequiredService<IProjectOptions>().RemoveCompileTimeOnlyCode;
        this._symbolEqualityComparer = compilationContext.CompilationContext.SymbolComparer;
        this._syntaxGenerationContext = compilationContext.CompilationContext.GetSyntaxGenerationContext( this._syntaxGenerationOptions, syntaxTree, 0 );
    }

    private SemanticModelProvider SemanticModelProvider => this._rewriterHelper.SemanticModelProvider;

    private ISymbolClassifier SymbolClassifier => this._rewriterHelper.SymbolClassifier;

    public static async Task<IPartialCompilation> RewriteAsync( IPartialCompilation compilation, ProjectServiceProvider serviceProvider )
    {
        var compilationContext = serviceProvider.GetRequiredService<ClassifyingCompilationContextFactory>().GetInstance( compilation.Compilation );

        var transformedCompilation =
            await compilation.RewriteSyntaxTreesAsync(
                oldRoot => new RunTimeAssemblyRewriter( serviceProvider, compilationContext, oldRoot.SyntaxTree ),
                serviceProvider );

        if ( transformedCompilation.Compilation.GetTypeByMetadataName( "Metalama.Compiler.Intrinsics" ) == null )
        {
            var instrinsicsSyntaxTree = _intrinsicsSyntaxTree.Value;

            // We need to copy syntax tree options, otherwise we may have language version mismatch.
            if ( compilation.Compilation.SyntaxTrees.Any() )
            {
                var options = compilation.Compilation.SyntaxTrees.First().Options;
                instrinsicsSyntaxTree = instrinsicsSyntaxTree.WithRootAndOptions( await instrinsicsSyntaxTree.GetRootAsync(), options );
            }

            transformedCompilation =
                transformedCompilation.WithSyntaxTreeTransformations( [SyntaxTreeTransformation.AddTree( instrinsicsSyntaxTree )] );
        }

        return transformedCompilation;
    }

    public override SyntaxNode VisitClassDeclaration( ClassDeclarationSyntax node )
    {
        var symbol = this.SemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetDeclaredSymbol( node )!;

        // Special case: aspect weavers and other aspect drivers are preserved in the runtime assembly.
        // This only happens if regular Metalama.Framework is referenced from the weaver project, which generally shouldn't happen.
        // But it is a pattern used by Metalama.Samples for try.postsharp.net.
        if ( this._aspectDriverSymbol != null && symbol.AllInterfaces.Any( i => this._symbolEqualityComparer.Equals( i, this._aspectDriverSymbol ) ) )
        {
            return node;
        }

        // In classes that contain compile-time-only code, we should disable a few warnings:
        // - warning CS0067: X is never used (because method bodies have been replaced by 'throw')

        var leadingTrivia = node.GetLeadingTrivia();
        var trailingTrivia = node.GetTrailingTrivia();

        if ( symbol.GetMembers().Any( this.MustReplaceByThrow ) )
        {
            var syntaxGenerationContext = this._compilationContext.CompilationContext.GetSyntaxGenerationContext( this._syntaxGenerationOptions, node );

            leadingTrivia = leadingTrivia.InsertAfterFirstNonWhitespaceTrivia(
                Trivia(
                        PragmaWarningDirectiveTrivia(
                                Token( SyntaxKind.DisableKeyword ),
                                true )
                            .WithErrorCodes( _suppressedWarnings )
                            .NormalizeWhitespace( eol: syntaxGenerationContext.EndOfLine )
                            .StructuredTriviaWithRequiredLeadingLineFeed( syntaxGenerationContext )
                            .StructuredTriviaWithRequiredTrailingLineFeed( syntaxGenerationContext ) )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ) );

            trailingTrivia = trailingTrivia.InsertBeforeLastNonWhitespaceTrivia(
                Trivia(
                        PragmaWarningDirectiveTrivia(
                                Token( SyntaxKind.RestoreKeyword ),
                                true )
                            .WithErrorCodes( _suppressedWarnings )
                            .NormalizeWhitespace( eol: syntaxGenerationContext.EndOfLine )
                            .StructuredTriviaWithRequiredLeadingLineFeed( syntaxGenerationContext )
                            .StructuredTriviaWithRequiredTrailingLineFeed( syntaxGenerationContext ) )
                    .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ) );
        }

        return base.VisitClassDeclaration( node )!
            .WithLeadingTrivia( leadingTrivia )
            .WithTrailingTrivia( trailingTrivia );
    }

    public override SyntaxNode VisitFieldDeclaration( FieldDeclarationSyntax node )
        => this.VisitFieldOrEventFieldDeclaration(
            node,
            ( n, variables ) => n.WithDeclaration( n.Declaration.WithVariables( SeparatedList( variables ) ) ) );

    public override SyntaxNode VisitEventFieldDeclaration( EventFieldDeclarationSyntax node )
        => this.VisitFieldOrEventFieldDeclaration(
            node,
            ( n, variables ) => n.WithDeclaration( n.Declaration.WithVariables( SeparatedList( variables ) ) ) );

    private T VisitFieldOrEventFieldDeclaration<T>( T node, Func<T, List<VariableDeclaratorSyntax>, T> replaceVariables )
        where T : BaseFieldDeclarationSyntax
    {
        var variables = new List<VariableDeclaratorSyntax>();
        ISymbol? lastTemplateSymbol = null;
        var transformedNode = node;

        foreach ( var variable in node.Declaration.Variables )
        {
            var symbol = this.SemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetDeclaredSymbol( variable )!;

            var transformedVariable = variable;

            if ( this.IsTemplate( symbol ) )
            {
                lastTemplateSymbol = symbol;

                transformedVariable = variable
                    .WithInitializer( null )
                    .WithIncludeInReferenceAssemblyAnnotation();
            }

            variables.Add( transformedVariable );
        }

        if ( lastTemplateSymbol != null )
        {
            transformedNode = replaceVariables( node, variables );

            transformedNode = this.PreserveAndAddAttribute( transformedNode, node, lastTemplateSymbol );
        }

        return transformedNode;
    }

    public override SyntaxNode VisitMethodDeclaration( MethodDeclarationSyntax node )
    {
        var symbol = this.SemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetDeclaredSymbol( node )!;
        var transformedNode = node;

        if ( this.MustReplaceByThrow( symbol ) )
        {
            transformedNode = this._rewriterHelper.WithThrowNotSupportedExceptionBody(
                node,
                "Compile-time-only code cannot be called at run-time.",
                this._syntaxGenerationContext );
        }

        if ( this.IsTemplate( symbol ) )
        {
            var isAsync = symbol.IsAsync;
            var isIteratorMethod = IteratorHelper.IsIteratorMethod( node );
            transformedNode = this.PreserveAndAddAttribute( transformedNode, node, symbol, isAsync, isIteratorMethod );
        }

        return transformedNode;
    }

    private bool MustReplaceByThrow( ISymbol symbol )
        => this._removeCompileTimeOnlyCode && !symbol.IsAbstract
                                           && (this.SymbolClassifier.GetTemplatingScope( symbol ).GetExpressionExecutionScope()
                                               == TemplatingScope.CompileTimeOnly ||
                                               !this.SymbolClassifier.GetTemplateInfo( symbol ).IsNone);

    private bool IsTemplate( ISymbol symbol ) => !this.SymbolClassifier.GetTemplateInfo( symbol ).IsNone;

    public override SyntaxNode? VisitPropertyDeclaration( PropertyDeclarationSyntax node )
    {
        // Properties can be in following forms:
        //  * Accessors with implicit bodies and backing field:         int Foo { get; set; }
        //  * Accessors with explicit bodies:                           int Foo { get { ... } set { ... } }
        //  * Accessors without bodies (abstract):                      abstract int Foo { get; set; }
        //  * Expression body:                                          int Foo => 42;
        //  * Accessors and initializer and backing field:              int Foo { get; } = 42;

        var symbol = this.SemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetDeclaredSymbol( node )!;
        var transformedNode = node;

        if ( this.MustReplaceByThrow( symbol ) )
        {
            if ( node.Modifiers.All( x => !x.IsKind( SyntaxKind.AbstractKeyword ) )
                 && node.AccessorList?.Accessors.All( x => x.Body == null && x.ExpressionBody == null ) == true )
            {
                // This is auto property - we keep it as it is (otherwise we lose the initial value and the fact that it is an auto property).
            }
            else
            {
                transformedNode = (PropertyDeclarationSyntax) this._rewriterHelper.WithThrowNotSupportedExceptionBody(
                    node,
                    "Compile-time-only code cannot be called at run-time." );
            }
        }

        if ( this.IsTemplate( symbol ) )
        {
            if ( node.Initializer != null )
            {
                transformedNode =
                    transformedNode
                        .WithInitializer( null )
                        .WithSemicolonToken( default );
            }

            transformedNode = this.PreserveAndAddAttribute( transformedNode, node, symbol );

            void ReplaceAccessor( SyntaxKind accessorKind, IMethodSymbol? accessorSymbol )
            {
                var accessor = node.AccessorList?.Accessors.FirstOrDefault( a => a.IsKind( accessorKind ) );

                if ( accessor != null )
                {
                    var transformedAccessor = transformedNode.AccessorList!.Accessors.First( a => a.IsKind( accessorKind ) );
                    var accessorMadePublic = this.PreserveAndAddAttribute( transformedAccessor, accessor, accessorSymbol.AssertNotNull() );
                    transformedNode = transformedNode.ReplaceNode( transformedAccessor, accessorMadePublic );
                }
            }

            ReplaceAccessor( SyntaxKind.GetAccessorDeclaration, symbol.GetMethod );
            ReplaceAccessor( SyntaxKind.InitAccessorDeclaration, symbol.SetMethod );
            ReplaceAccessor( SyntaxKind.SetAccessorDeclaration, symbol.SetMethod );
        }

        return transformedNode;
    }

    public override SyntaxNode VisitEventDeclaration( EventDeclarationSyntax node )
    {
        var symbol = this.SemanticModelProvider.GetSemanticModel( node.SyntaxTree ).GetDeclaredSymbol( node )!;
        var transformedNode = node;

        if ( this.MustReplaceByThrow( symbol ) )
        {
            transformedNode = (EventDeclarationSyntax) this._rewriterHelper.WithThrowNotSupportedExceptionBody(
                node,
                "Compile-time-only code cannot be called at run-time." );
        }

        if ( this.IsTemplate( symbol ) )
        {
            transformedNode = this.PreserveAndAddAttribute( transformedNode, node, symbol );
        }

        return transformedNode;
    }

    private T PreserveAndAddAttribute<T>( T transformedNode, T originalNode, ISymbol symbol, bool isAsyncMethod = false, bool isIteratorMethod = false )
        where T : MemberDeclarationSyntax
    {
        var accessibility = symbol.DeclaredAccessibility.ToOurAccessibility();

        if ( accessibility is Accessibility.Public or Accessibility.Protected && !isAsyncMethod && !isIteratorMethod )
        {
            // No change is needed.
            return transformedNode;
        }

        var attributeList = this.CreateCompiledTemplateAttribute( originalNode, accessibility, isAsyncMethod, isIteratorMethod )
            .WithOptionalTrailingLineFeed( this._syntaxGenerationContext );

        return (T) transformedNode.WithIncludeInReferenceAssemblyAnnotation()
            .WithAttributeLists( transformedNode.AttributeLists.Add( attributeList ) )
            .WithLeadingTrivia( transformedNode.GetLeadingTrivia() );
    }

    private AccessorDeclarationSyntax PreserveAndAddAttribute(
        AccessorDeclarationSyntax transformedNode,
        AccessorDeclarationSyntax originalNode,
        IMethodSymbol symbol )
    {
        var isIteratorMethod = symbol.IsIteratorMethod();
        var accessibility = symbol.DeclaredAccessibility.ToOurAccessibility();

        if ( accessibility is Accessibility.Public or Accessibility.Protected
             && !isIteratorMethod )
        {
            // No change is needed.
            return transformedNode;
        }

        var attributeList = this.CreateCompiledTemplateAttribute( originalNode, accessibility, false, isIteratorMethod )
            .WithTrailingTrivia( ElasticSpace );

        return transformedNode.WithIncludeInReferenceAssemblyAnnotation()
            .WithAttributeLists( transformedNode.AttributeLists.Add( attributeList ) )
            .WithLeadingTrivia( transformedNode.GetLeadingTrivia() );
    }

    private AttributeListSyntax CreateCompiledTemplateAttribute( SyntaxNode node, Accessibility accessibility, bool isAsyncMethod, bool isIteratorMethod )
    {
        var syntaxFactory = this._compilationContext.CompilationContext.GetSyntaxGenerationContext( this._syntaxGenerationOptions, node );
        var compiledTemplateAttributeType = (INamedTypeSymbol) syntaxFactory.ReflectionMapper.GetTypeSymbol( typeof(CompiledTemplateAttribute) );
        var accessibilityType = (INamedTypeSymbol) syntaxFactory.ReflectionMapper.GetTypeSymbol( typeof(Accessibility) );

        var attribute = Attribute( (NameSyntax) syntaxFactory.SyntaxGenerator.Type( compiledTemplateAttributeType ) )
            .WithArgumentList(
                AttributeArgumentList(
                    SeparatedList(
                    [
                        AttributeArgument( syntaxFactory.SyntaxGenerator.EnumValueExpression( accessibilityType, (int) accessibility ) )
                            .WithNameEquals( NameEquals( nameof(CompiledTemplateAttribute.Accessibility) ) ),
                        AttributeArgument( SyntaxFactoryEx.LiteralExpression( isAsyncMethod ) )
                            .WithNameEquals( NameEquals( nameof(CompiledTemplateAttribute.IsAsync) ) ),
                        AttributeArgument( SyntaxFactoryEx.LiteralExpression( isIteratorMethod ) )
                            .WithNameEquals( NameEquals( nameof(CompiledTemplateAttribute.IsIteratorMethod) ) )
                    ] ) ) );

        var attributeList = AttributeList( SingletonSeparatedList( attribute ) )
            .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation );

        return attributeList;
    }
}