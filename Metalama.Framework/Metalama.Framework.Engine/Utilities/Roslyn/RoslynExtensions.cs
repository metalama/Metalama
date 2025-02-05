// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Provides extension methods that are useful when writing code using Metalama SDK.
/// </summary>
internal static class RoslynExtensions
{
    private static TCompilation ReplaceTrees<TCompilation>(
        this TCompilation compilation,
        Func<SyntaxTree, SyntaxTree?> replacer,
        IEnumerable<SyntaxTree>? trees = null )
        where TCompilation : Compilation
    {
        trees ??= compilation.SyntaxTrees;

        foreach ( var tree in trees )
        {
            var newTree = replacer( tree );

            if ( newTree != null )
            {
                compilation = (TCompilation) compilation.ReplaceSyntaxTree( tree, newTree );
            }
            else
            {
                compilation = (TCompilation) compilation.RemoveSyntaxTrees( tree );
            }
        }

        return compilation;
    }

    public static TCompilation VisitTrees<TCompilation>(
        this CSharpSyntaxRewriter rewriter,
        TCompilation compilation,
        IEnumerable<SyntaxTree>? trees = null )
        where TCompilation : Compilation
        => compilation.ReplaceTrees(
            tree =>
            {
                var newRoot = rewriter.Visit( tree.GetRoot() );

                return newRoot == null! ? null : tree.WithRootAndOptions( newRoot, tree.Options );
            },
            trees );

    public static string ToDisplayName( this SymbolKind symbolKind )
        => symbolKind switch
        {
            SymbolKind.Alias => "alias",
            SymbolKind.ArrayType => "array type",
            SymbolKind.Assembly => "assembly",
            SymbolKind.DynamicType => "dynamic type",
            SymbolKind.ErrorType => "invalid type",
            SymbolKind.Event => "event",
            SymbolKind.Field => "field",
            SymbolKind.Label => "label",
            SymbolKind.Local => "local variable",
            SymbolKind.Method => "method",
            SymbolKind.NetModule => "module",
            SymbolKind.NamedType => "type",
            SymbolKind.Namespace => "namespace",
            SymbolKind.Parameter => "parameter",
            SymbolKind.PointerType => "pointer type",
            SymbolKind.Property => "property",
            SymbolKind.RangeVariable => "range variable",
            SymbolKind.TypeParameter => "type parameter",
            SymbolKind.Preprocessing => "preprocessing directive",
            SymbolKind.Discard => "discard",
            SymbolKind.FunctionPointerType => "function pointer type",
            _ => throw new ArgumentOutOfRangeException( nameof(symbolKind), symbolKind, null )
        };

    public static bool? ToIsAnnotated( this NullableAnnotation annotation )
        => annotation switch
        {
            NullableAnnotation.Annotated => true,
            NullableAnnotation.NotAnnotated => false,
            _ => null
        };

    public static bool? IsNullable( this ITypeSymbol typeSymbol )
    {
        if ( typeSymbol is ITypeParameterSymbol typeParameterSymbol )
        {
            // Unconstrained, class? constrained and IFoo? constrained are *not* considered nullable,
            // if they have NullableAnnotation.NotAnnotated, because non-nullable types also satify these constraints.

            // Annotation takes priority over constraint.
            // E.g. in void M<T>(T? t) where T : notnull, the type of t is ITypeParameterSymbol with TypeKindConstraint of NotNull and NullableAnnotation.Annotated.
            if ( typeParameterSymbol.NullableAnnotation.ToIsAnnotated() == true )
            {
                return true;
            }

            // If thw symbol didn't have '?', check constraints.
            if ( typeParameterSymbol.ConstraintTypes.Any( t => t.IsNullable() == false ) )
            {
                return false;
            }

            if ( typeParameterSymbol.HasReferenceTypeConstraint )
            {
                return typeParameterSymbol.ReferenceTypeConstraintNullableAnnotation.ToIsAnnotated() == false ? false : null;
            }
            else if ( typeParameterSymbol.HasValueTypeConstraint
                      || typeParameterSymbol.HasNotNullConstraint
                      || typeParameterSymbol.HasUnmanagedTypeConstraint )
            {
                return false;
            }

            return null;
        }

        if ( typeSymbol.IsReferenceType )
        {
            return typeSymbol.NullableAnnotation.ToIsAnnotated();
        }
        else
        {
            return typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }
    }
}