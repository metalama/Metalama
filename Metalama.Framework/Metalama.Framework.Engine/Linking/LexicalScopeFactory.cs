// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using MethodKind = Metalama.Framework.Code.MethodKind;
using RoslynMethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LexicalScopeFactory : ITemplateLexicalScopeProvider
{
    /*
     * Calling the SemanticModel.LookupSymbols method is expensive, so expensive that it can be a hotspot of Metalama.
     *
     *  To save calls to LookupSymbols, we cache the result per type declaration, and we discover the rest incrementally from the syntax.
     */

    private readonly CompilationModel _compilationModel;
    private readonly ConcurrentDictionary<IFullRef<IDeclaration>, TemplateLexicalScope> _scopes;
    private readonly ConcurrentDictionary<TypeDeclarationSyntax, ImmutableHashSet<string>> _identifiersInTypeDeclaration = new();
    private readonly SemanticModelProvider _semanticModelProvider;

    public LexicalScopeFactory( CompilationModel compilation )
    {
        this._compilationModel = compilation;
        this._semanticModelProvider = compilation.RoslynCompilation.GetSemanticModelProvider();
        this._scopes = new ConcurrentDictionary<IFullRef<IDeclaration>, TemplateLexicalScope>( RefEqualityComparer<IDeclaration>.Default );
    }

    /// <summary>
    /// Gets a shared lexical code where consumers can add their own symbols.
    /// </summary>
    public TemplateLexicalScope GetLexicalScope( IFullRef<IDeclaration> declaration ) => this._scopes.GetOrAdd( declaration, this.CreateLexicalScope );

    private ImmutableHashSet<string> GetIdentifiersInTypeScope( TypeDeclarationSyntax type )
        => this._identifiersInTypeDeclaration.GetOrAdd( type, this.GetIdentifiersInTypeScopeCore );

    private ImmutableHashSet<string> GetIdentifiersInTypeScopeCore( TypeDeclarationSyntax type )
    {
        var semanticModel = this._semanticModelProvider.GetSemanticModel( type.SyntaxTree );
        var symbols = semanticModel.LookupSymbols( type.OpenBraceToken.Span.End );

        var declaredTypeSymbol = (INamedTypeSymbol?) semanticModel.GetDeclaredSymbol( type );

        if ( declaredTypeSymbol == null
             || !this._compilationModel.TryGetDeclaration( declaredTypeSymbol, out var declaration )
             || declaration is not INamedType declaredType )
        {
            throw new AssertionFailedException( $"Could not find declaration for {type.Identifier} in {type.SyntaxTree.FilePath}." );
        }

        var builder = ImmutableHashSet<string>.Empty.ToBuilder();

        foreach ( var symbol in symbols )
        {
            builder.Add( symbol.Name );
        }

        CollectIntroducedRecursive( declaredType, builder );

        return builder.ToImmutable();

        static void CollectIntroducedRecursive( INamedType type, ImmutableHashSet<string>.Builder builder )
        {
            // TODO: It may be worthwhile to cache this for better performance on large hierarchies.
            foreach ( var builtMember in type.Members().OfType<IntroducedMember>() )
            {
                builder.Add( builtMember.Name );
            }

            if ( type.BaseType != null )
            {
                CollectIntroducedRecursive( type.BaseType, builder );
            }

            if ( type.DeclaringType != null )
            {
                CollectIntroducedRecursive( type.DeclaringType, builder );
            }
        }
    }

    private TemplateLexicalScope CreateLexicalScope( IFullRef<IDeclaration> declarationRef )
    {
        switch ( declarationRef )
        {
            case IIntroducedRef:
                {
                    var declaration = declarationRef.Definition;

                    var contextType = declaration switch
                    {
                        IMemberOrNamedType { DeclaringType: { } declaringType } => declaringType,
                        INamedType type => type,
                        _ => throw new AssertionFailedException( $"Declarations without declaring type are not supported {declaration}." )
                    };

                    // Builder-based source.
                    if ( contextType.GetPrimaryDeclarationSyntax() == null )
                    {
                        // TODO: Temp hack.
                        return new TemplateLexicalScope( ImmutableHashSet<string>.Empty );
                    }

                    var typeDeclaration = contextType.GetPrimaryDeclarationSyntax().AssertNotNull().GetDeclaringType().AssertNotNull();

                    var identifiers = this.GetIdentifiersInTypeScope( typeDeclaration ).ToBuilder();

                    if ( declaration is IMethod method )
                    {
                        identifiers.AddRange( method.Parameters.SelectAsReadOnlyList( p => p.Name ) );
                        identifiers.AddRange( method.TypeParameters.SelectAsReadOnlyList( p => p.Name ) );
                    }

                    // Accessors have implicit "value" parameter.
                    if ( declaration is IMethod { MethodKind: MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove } )
                    {
                        identifiers.Add( "value" );
                    }

                    return new TemplateLexicalScope( identifiers.ToImmutable() );
                }

            case ISymbolRef symbolRef:
                {
                    var symbol = symbolRef.Symbol;

                    // Symbol-based scope.
                    var syntaxReference = symbol.GetPrimarySyntaxReference();

                    // For implicitly defined symbols, we need to try harder.
                    if ( syntaxReference == null )
                    {
                        switch ( symbol )
                        {
                            // For accessors, look at the associated symbol.
                            case IMethodSymbol { AssociatedSymbol: { } associatedSymbol }:
                                syntaxReference = associatedSymbol.GetPrimarySyntaxReference();

                                if ( syntaxReference == null )
                                {
                                    throw new AssertionFailedException( $"No syntax for '{associatedSymbol}'." );
                                }

                                break;

                            // Otherwise (e.g. for implicit constructors), take the containing type.
                            case { ContainingType: { } containingType }:
                                syntaxReference = containingType.GetPrimarySyntaxReference();

                                if ( syntaxReference == null )
                                {
                                    throw new AssertionFailedException( $"No syntax for '{containingType}'." );
                                }

                                break;

                            default:
                                throw new AssertionFailedException( $"Unexpected symbol '{symbol}'." );
                        }
                    }

                    var syntaxNode = syntaxReference.GetSyntax();
                    var typeDeclarationSyntax = syntaxNode.GetDeclaringType();

                    if ( syntaxNode is LocalFunctionStatementSyntax && typeDeclarationSyntax == null )
                    {
                        throw new AssertionFailedException( "Top-level local functions are not supported: {syntaxNode}" );
                    }

                    var builder = this.GetIdentifiersInTypeScope( typeDeclarationSyntax.AssertNotNull() ).ToBuilder();

                    // Accessors have implicit "value" parameter.
                    if ( symbol is IMethodSymbol { MethodKind: RoslynMethodKind.PropertySet or RoslynMethodKind.EventAdd or RoslynMethodKind.EventRemove } )
                    {
                        builder.Add( "value" );
                    }

                    // Get the symbols defined in the declaration.
                    var visitor = new Visitor( builder );

                    var declarationSyntax =
                        syntaxReference.GetSyntax() switch
                        {
                            { Parent: AccessorListSyntax { Parent: IndexerDeclarationSyntax indexer } } => indexer,
                            { } anything => anything
                        };

                    visitor.Visit( declarationSyntax );

                    return new TemplateLexicalScope( builder.ToImmutable() );
                }

            default:
                throw new AssertionFailedException();
        }
    }
}