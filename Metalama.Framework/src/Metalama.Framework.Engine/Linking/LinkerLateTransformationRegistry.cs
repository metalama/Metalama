// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Stores information about transformations that are unrelated to linking but has to be performed during linking. 
/// </summary>
internal sealed class LinkerLateTransformationRegistry
{
    private readonly ISet<INamedTypeSymbol> _typesWithRemovedPrimaryConstructor;
    private readonly ISet<ISymbol> _primaryConstructorInitializedMembers;

    public LinkerLateTransformationRegistry(
        PartialCompilation intermediateCompilation,
        IReadOnlyDictionary<ISymbolRef<INamedType>, LateTypeLevelTransformations> lateTypeLevelTransformations )
    {
        // TODO: Parallelize.
        HashSet<INamedTypeSymbol> typesWithRemovedPrimaryConstructor;
        HashSet<ISymbol> primaryConstructorInitializedMembers;

        this._typesWithRemovedPrimaryConstructor = typesWithRemovedPrimaryConstructor =
            new HashSet<INamedTypeSymbol>( intermediateCompilation.CompilationContext.SymbolComparer );

        this._primaryConstructorInitializedMembers =
            primaryConstructorInitializedMembers = new HashSet<ISymbol>( intermediateCompilation.CompilationContext.SymbolComparer );

        foreach ( var lateTypeLevelTransformationPair in lateTypeLevelTransformations )
        {
            var type = lateTypeLevelTransformationPair.Key;
            var transformations = lateTypeLevelTransformationPair.Value;

            var typeSymbol = (INamedTypeSymbol) intermediateCompilation.CompilationContext.SymbolTranslator.Translate( type.Symbol ).AssertNotNull();

            if ( transformations.ShouldRemovePrimaryConstructor )
            {
                typesWithRemovedPrimaryConstructor.Add( typeSymbol );

                foreach ( var symbol in typeSymbol.GetMembers() )
                {
                    if ( symbol.IsImplicitlyDeclared )
                    {
                        continue;
                    }

                    switch ( symbol.Kind )
                    {
                        case SymbolKind.Field when symbol is IFieldSymbol { IsStatic: false } fieldSymbol:
                            var declarator = (VariableDeclaratorSyntax) fieldSymbol.GetPrimaryDeclarationSyntax().AssertNotNull();

                            if ( declarator.Initializer == null )
                            {
                                continue;
                            }

                            primaryConstructorInitializedMembers.Add( fieldSymbol );

                            break;

                        case SymbolKind.Property when symbol is IPropertySymbol { IsStatic: false } propertySymbol:
                            var primaryDeclaration = propertySymbol.GetPrimaryDeclarationSyntax().AssertNotNull();

                            switch ( primaryDeclaration.Kind() )
                            {
                                case SyntaxKind.PropertyDeclaration:
                                    var propertyDeclaration = (PropertyDeclarationSyntax) primaryDeclaration;

                                    if ( propertyDeclaration.Initializer == null )
                                    {
                                        continue;
                                    }

                                    primaryConstructorInitializedMembers.Add( propertySymbol );

                                    break;

                                case SyntaxKind.Parameter:
                                    primaryConstructorInitializedMembers.Add( propertySymbol );

                                    break;
                            }

                            break;

                        case SymbolKind.Event when symbol is IEventSymbol { IsStatic: false } eventSymbol:
                            var eventDeclaration = eventSymbol.GetPrimaryDeclarationSyntax().AssertNotNull();

                            if ( eventDeclaration.IsKind( SyntaxKind.VariableDeclarator ) )
                            {
                                var eventFieldDeclarator = (VariableDeclaratorSyntax) eventDeclaration;

                                if ( eventFieldDeclarator.Initializer == null )
                                {
                                    continue;
                                }

                                primaryConstructorInitializedMembers.Add( eventSymbol );
                            }

                            break;
                    }
                }
            }
        }
    }

    public bool HasRemovedPrimaryConstructor( INamedTypeSymbol type ) => this._typesWithRemovedPrimaryConstructor.Contains( type );

#pragma warning disable CA1822 // Mark members as static

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public IReadOnlyList<IFieldSymbol> GetPrimaryConstructorFields( INamedTypeSymbol type )
#pragma warning restore CA1822 // Mark members as static
    {
#if ROSLYN_4_8_0_OR_GREATER
        var typeSyntax =
            (TypeDeclarationSyntax) type.DeclaringSyntaxReferences.Select( r => r.GetSyntax() )
                .Single( d => (d.IsKind( SyntaxKind.ClassDeclaration ) || d.IsKind( SyntaxKind.StructDeclaration ) || d.IsKind( SyntaxKind.RecordDeclaration ) || d.IsKind( SyntaxKind.RecordStructDeclaration )) && ((TypeDeclarationSyntax) d).ParameterList != null );

        if ( typeSyntax.IsKind( SyntaxKind.RecordDeclaration ) || typeSyntax.IsKind( SyntaxKind.RecordStructDeclaration ) )
        {
            return Array.Empty<IFieldSymbol>();
        }

        var parameterList = typeSyntax.ParameterList.AssertNotNull();

        return type.GetMembers().OfType<IFieldSymbol>().Where( f => f.Locations.Any( l => parameterList.Span.Contains( l.SourceSpan.Start ) ) ).ToArray();
#else
        return Array.Empty<IFieldSymbol>();
#endif
    }

#pragma warning disable CA1822 // Mark members as static

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public IReadOnlyList<IPropertySymbol> GetPrimaryConstructorProperties( INamedTypeSymbol type )
#pragma warning restore CA1822 // Mark members as static
    {
        return type.GetMembers().OfType<IPropertySymbol>().Where( p => p.GetPrimaryDeclarationSyntax()?.IsKind( SyntaxKind.Parameter ) == true ).ToArray();
    }

    public bool IsPrimaryConstructorInitializedMember( ISymbol symbol ) => this._primaryConstructorInitializedMembers.Contains( symbol );

    public ArgumentListSyntax? GetPrimaryConstructorBaseArgumentList( IMethodSymbol constructor )
    {
        var type = constructor.ContainingType;

        Invariant.Assert( this.HasRemovedPrimaryConstructor( type ) );

#if ROSLYN_4_8_0_OR_GREATER
        var typeSyntax =
            (TypeDeclarationSyntax) type.DeclaringSyntaxReferences.Select( r => r.GetSyntax() )
                .Single( d => (d.IsKind( SyntaxKind.ClassDeclaration ) || d.IsKind( SyntaxKind.StructDeclaration ) || d.IsKind( SyntaxKind.RecordDeclaration ) || d.IsKind( SyntaxKind.RecordStructDeclaration )) && ((TypeDeclarationSyntax) d).ParameterList != null );
#else
        var typeSyntax =
            (RecordDeclarationSyntax) type.DeclaringSyntaxReferences.Select( r => r.GetSyntax() )
            .Single( d => (d.IsKind( SyntaxKind.RecordDeclaration ) || d.IsKind( SyntaxKind.RecordStructDeclaration )) && ((RecordDeclarationSyntax) d).ParameterList != null );
#endif

        var primaryConstructorBase = typeSyntax.BaseList?.Types.OfType<PrimaryConstructorBaseTypeSyntax>().SingleOrDefault();

        return primaryConstructorBase?.ArgumentList;
    }
}