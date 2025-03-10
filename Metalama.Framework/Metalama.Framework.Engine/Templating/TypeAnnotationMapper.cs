// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.Templating;
// This class uses WeakReferences because it seems that Roslyn's ObjectPool may cache SyntaxAnnotation beyond the lifetime of a compilation.

internal static class TypeAnnotationMapper
{
    public const string ExpressionTypeSymbolAnnotationKind = "Metalama.ExpressionType";

    private const string _expressionITypeAnnotationKind = "Metalama.ExpressionIType";
    private const string _expressionIsReferenceableAnnotationKind = "Metalama.ExpressionIsReferenceable";

    private static readonly SyntaxAnnotation _expressionIsReferenceableAnnotation = new( _expressionIsReferenceableAnnotationKind, "true" );
    private static readonly SyntaxAnnotation _expressionIsNotReferenceableAnnotation = new( _expressionIsReferenceableAnnotationKind, "false" );
    private static readonly ConditionalWeakTable<SyntaxAnnotation, WeakReference<ITypeSymbol>> _annotationToSymbolMap = new();
    private static readonly ConditionalWeakTable<ITypeSymbol, List<SyntaxAnnotation>> _symbolToAnnotationsMap = new();
    private static readonly ConditionalWeakTable<SyntaxAnnotation, WeakReference<IType>> _annotationToITypeMap = new();
    private static readonly ConditionalWeakTable<IType, List<SyntaxAnnotation>> _iTypeToAnnotationsMap = new();

    public static SyntaxAnnotation GetOrCreateAnnotation( string kind, ITypeSymbol symbol )
    {
        var list = _symbolToAnnotationsMap.GetOrCreateValue( symbol );

        lock ( list )
        {
            var annotation = list.SingleOrDefault( x => x.Kind == kind );

            if ( annotation == null )
            {
                annotation = new SyntaxAnnotation( kind );
                list.Add( annotation );
                _annotationToSymbolMap.Add( annotation, new WeakReference<ITypeSymbol>( symbol ) );
            }

            return annotation;
        }
    }

    private static ITypeSymbol GetSymbolFromAnnotation( SyntaxAnnotation annotation )
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if ( !_annotationToSymbolMap.TryGetValue( annotation, out var reference ) || !reference.TryGetTarget( out var symbol ) )
        {
            throw new KeyNotFoundException();
        }

        return symbol;
    }

    private static SyntaxAnnotation GetOrCreateAnnotation( string kind, IType type )
    {
        var list = _iTypeToAnnotationsMap.GetOrCreateValue( type );

        lock ( list )
        {
            var annotation = list.SingleOrDefault( x => x.Kind == kind );

            if ( annotation == null )
            {
                annotation = new SyntaxAnnotation( kind );
                list.Add( annotation );
                _annotationToITypeMap.Add( annotation, new WeakReference<IType>( type ) );
            }

            return annotation;
        }
    }

    private static IType GetTypeFromAnnotation( SyntaxAnnotation annotation )
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if ( !_annotationToITypeMap.TryGetValue( annotation, out var reference ) || !reference.TryGetTarget( out var type ) )
        {
            throw new KeyNotFoundException();
        }

        return type;
    }

    private static ExpressionSyntax AddAnnotationRecursive( ExpressionSyntax n, SyntaxAnnotation syntaxAnnotation )
    {
        if ( n is ParenthesizedExpressionSyntax parenthesizedExpression )
        {
            return parenthesizedExpression.WithExpression( AddAnnotationRecursive( parenthesizedExpression.Expression, syntaxAnnotation ) )
                .WithoutAnnotations( syntaxAnnotation.Kind! )
                .WithAdditionalAnnotations( syntaxAnnotation );
        }
        else
        {
            return n.WithoutAnnotations( syntaxAnnotation.Kind! ).WithAdditionalAnnotations( syntaxAnnotation );
        }
    }

    private static ExpressionSyntax AddExpressionTypeAnnotation( ExpressionSyntax node, ITypeSymbol? type )
    {
        if ( type == null )
        {
            return node;
        }

        var existingAnnotation = node.GetAnnotations( ExpressionTypeSymbolAnnotationKind ).SingleOrDefault();

        if ( existingAnnotation != null && SymbolEqualityComparer.IncludeNullability.Equals( GetSymbolFromAnnotation( existingAnnotation ), type ) )
        {
            return node;
        }

        var syntaxAnnotation = GetOrCreateAnnotation( ExpressionTypeSymbolAnnotationKind, type );

        Invariant.Assert( SymbolEqualityComparer.IncludeNullability.Equals( GetSymbolFromAnnotation( syntaxAnnotation ), type ) );

        return AddAnnotationRecursive( node, syntaxAnnotation );
    }

    private static bool TryFindExpressionTypeFromAnnotation(
        SyntaxNode node,
        CompilationContext compilationContext,
        [NotNullWhen( true )] out ITypeSymbol? type )
    {
        // If we don't know the exact type, check if we have a type annotation on the syntax.

        var typeAnnotation = node.GetAnnotations( ExpressionTypeSymbolAnnotationKind ).FirstOrDefault();

        if ( typeAnnotation != null )
        {
            type = GetSymbolFromAnnotation( typeAnnotation );
        }
        else
        {
            type = null;

            return false;
        }

        type = compilationContext.SymbolTranslator.Translate( type ).AssertNotNull( $"The symbol '{type}' could not be translated." );

        return true;
    }

    public static ExpressionSyntax AddExpressionTypeAnnotation( ExpressionSyntax node, IType? type )
    {
        if ( type == null )
        {
            return node;
        }

        if ( type.GetSymbol() is { } symbol )
        {
            return AddExpressionTypeAnnotation( node, symbol );
        }

        var existingAnnotation = node.GetAnnotations( _expressionITypeAnnotationKind ).SingleOrDefault();

        if ( existingAnnotation != null && type.Compilation.Comparers.IncludeNullability.Equals( GetTypeFromAnnotation( existingAnnotation ), type ) )
        {
            return node;
        }

        var syntaxAnnotation = GetOrCreateAnnotation( _expressionITypeAnnotationKind, type );

        Invariant.Assert( type.Compilation.Comparers.IncludeNullability.Equals( GetTypeFromAnnotation( syntaxAnnotation ), type ) );

        return AddAnnotationRecursive( node, syntaxAnnotation );
    }

    public static bool TryFindExpressionTypeFromAnnotation(
        SyntaxNode node,
        CompilationModel compilationModel,
        [NotNullWhen( true )] out IType? type )
    {
        // If we don't know the exact type, check if we have a type annotation on the syntax.

        var compilationContext = compilationModel.CompilationContext;

        if ( TryFindExpressionTypeFromAnnotation( node, compilationContext, out var symbol ) )
        {
            type = compilationModel.Factory.GetIType(
                compilationContext.SymbolTranslator.Translate( symbol ).AssertNotNull( $"The symbol '{symbol}' could not be translated." ) );

            return true;
        }

        var typeAnnotation = node.GetAnnotations( _expressionITypeAnnotationKind ).FirstOrDefault();

        if ( typeAnnotation != null )
        {
            type = GetTypeFromAnnotation( typeAnnotation );
        }
        else
        {
            type = null;

            return false;
        }

        type = compilationModel.Factory.Translate( type )
            .AssertNotNull( $"The type '{type}' could not be translated." );

        return true;
    }

    public static bool? GetExpressionIsReferenceableFromAnnotation( ExpressionSyntax expressionSyntax )
        => expressionSyntax.GetAnnotations( _expressionIsReferenceableAnnotationKind ).FirstOrDefault()?.Data switch
        {
            null => null,
            "true" => true,
            "false" => false,
            _ => throw new ArgumentOutOfRangeException()
        };

    public static ExpressionSyntax AddIsExpressionReferenceableAnnotation( ExpressionSyntax expressionSyntax, bool isReferenceable )
    {
        if ( GetExpressionIsReferenceableFromAnnotation( expressionSyntax ) != null )
        {
            return expressionSyntax;
        }
        else
        {
            return expressionSyntax.WithAdditionalAnnotations(
                isReferenceable ? _expressionIsReferenceableAnnotation : _expressionIsNotReferenceableAnnotation );
        }
    }
}