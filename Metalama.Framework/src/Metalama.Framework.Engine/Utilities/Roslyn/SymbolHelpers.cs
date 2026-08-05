// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

internal static class SymbolHelpers
{
    internal static bool? BelongsToCompilation( this ISymbol symbol, CompilationContext compilationContext )
    {
        if ( IsErrorSymbol( symbol ) )
        {
            // An error symbol is a placeholder that Roslyn synthesizes for a reference that it could not resolve, so it is
            // not a legitimate member of any compilation and no decision can be taken. Taking one would not be reliable
            // either: the assembly that contains an error symbol can be the assembly symbol of a different compilation, or
            // a synthesized placeholder, whose identity collides with an identity of the current compilation. Error symbols
            // reach this method in the IDE whenever the references of a project are momentarily incomplete, for instance
            // while a build replaces them.

            return null;
        }

        var assembly = symbol.ContainingAssembly;

        if ( assembly == null )
        {
            return null;
        }

        if ( !compilationContext.Assemblies.TryGetValue( assembly.Identity, out var thisCompilationAssembly ) )
        {
            // If we cannot find the assembly, we cannot make any decision whether this is or not a legit symbol.
            // It can happen that a referenced assembly has symbols to another referenced assembly that is not directly
            // referenced by our compilation.

            return null;
        }

        return assembly.Equals( thisCompilationAssembly );
    }

    [Conditional( "DEBUG" )]
    internal static void ThrowIfBelongsToDifferentCompilationThan( this ISymbol? symbol, CompilationContext compilationContext )
    {
        if ( symbol == null )
        {
            return;
        }

        if ( symbol.BelongsToCompilation( compilationContext ) == false )
        {
            throw new AssertionFailedException( $"The symbol '{symbol}' does not belong to the expected compilation." );
        }
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    [return: NotNullIfNotNull( nameof(symbol) )]
    internal static T? AssertBelongsToCompilationContext<T>( this T? symbol, CompilationContext compilationContext )
        where T : class, ISymbol
    {
#if DEBUG

        if ( symbol == null )
        {
            return null;
        }

        if ( symbol.BelongsToCompilation( compilationContext ) == false )
        {
            throw new AssertionFailedException( $"The symbol '{symbol}' does not belong to the expected compilation." );
        }

        return symbol;
#else
        return symbol;
#endif
    }

    [Conditional( "DEBUG" )]
    internal static void ThrowIfBelongsToDifferentCompilationThan( this ISymbol? symbol, ISymbol? otherSymbol )
    {
        if ( symbol?.ContainingAssembly == null || otherSymbol?.ContainingAssembly == null )
        {
            return;
        }

        if ( IsErrorSymbol( symbol ) || IsErrorSymbol( otherSymbol ) )
        {
            // See the comment in BelongsToCompilation.

            return;
        }

        if ( symbol.ContainingAssembly.Identity.Equals( otherSymbol.ContainingAssembly.Identity )
             && !symbol.ContainingAssembly.Equals( otherSymbol.ContainingAssembly ) )
        {
            throw new AssertionFailedException( $"The symbols '{symbol}' and '{otherSymbol}' do not belong to the same compilation." );
        }
    }

    /// <summary>
    /// Determines whether a symbol is an error symbol, that is, a placeholder that Roslyn synthesizes for a reference
    /// that it could not resolve, or a member of such a placeholder.
    /// </summary>
    private static bool IsErrorSymbol( ISymbol symbol ) => symbol.Kind == SymbolKind.ErrorType || symbol.ContainingType?.Kind == SymbolKind.ErrorType;

    internal static ImmutableArray<SpecialType> ArrayGenericInterfaces { get; } =
        ImmutableArray.Create(
            SpecialType.System_Collections_Generic_IEnumerable_T,
            SpecialType.System_Collections_Generic_IList_T,
            SpecialType.System_Collections_Generic_ICollection_T,
            SpecialType.System_Collections_Generic_IReadOnlyList_T,
            SpecialType.System_Collections_Generic_IReadOnlyCollection_T );
}