// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Fabrics;

public interface ITaggedQuery<out TDeclaration, out TTag> : IQuery<TDeclaration>
    where TDeclaration : class, IDeclaration
{
    /// <summary>
    /// Projects each declaration of the current set to an <see cref="IEnumerable{T}"/> (typically a list of child declarations) and flattens the resulting sequences into one set.
    /// </summary>
    /// <remarks>
    /// <para>The query on the <i>right</i> part of <c>SelectMany</c> is executed concurrently. It is therefore preferable to use the <c>Where</c>, <c>Select</c>
    /// or <c>SelectMany</c> methods of the current interface instead of using the equivalent system methods inside the <paramref name="selector"/> query.</para>
    /// </remarks>
    new ITaggedQuery<TMember, TTag> SelectMany<TMember>( Func<TDeclaration, IEnumerable<TMember>> selector )
        where TMember : class, IDeclaration;

    /// <summary>
    /// Projects each declaration of the current set to an <see cref="IEnumerable{T}"/> (typically a list of child declarations) and flattens the resulting sequences into one set.
    /// This overload does supplies the tag to the <paramref name="selector"/> delegate.
    /// </summary>
    /// <remarks>
    /// <para>The query on the <i>right</i> part of <c>SelectMany</c> is executed concurrently. It is therefore preferable to use the <c>Where</c>, <c>Select</c>
    /// or <c>SelectMany</c> methods of the current interface instead of using the equivalent system methods inside the <paramref name="selector"/> query.</para>
    /// </remarks>
    ITaggedQuery<TMember, TTag> SelectMany<TMember>( Func<TDeclaration, TTag, IEnumerable<TMember>> selector )
        where TMember : class, IDeclaration;

    /// <summary>
    /// Projects each declaration of the current set into a new declaration.
    /// </summary>
    new ITaggedQuery<TMember, TTag> Select<TMember>( Func<TDeclaration, TMember> selector )
        where TMember : class, IDeclaration;

    /// <summary>
    /// Projects each declaration of the current set into a new declaration.
    /// This overload does supplies the tag to the <paramref name="selector"/> delegate.
    /// </summary>
    ITaggedQuery<TMember, TTag> Select<TMember>( Func<TDeclaration, TTag, TMember> selector )
        where TMember : class, IDeclaration;

    /// <summary>
    /// Selects all types enclosed in declarations of the current set. 
    /// </summary>
    /// <param name="includeNestedTypes">Indicates whether nested types should be recursively included in the output.</param>
    /// <remarks>
    /// <para>
    /// This method projects <see cref="ICompilation"/> and <see cref="INamespace"/> to all the types in the compilation or namespace.
    /// It projects <see cref="INamedType"/> to itself. It projects members or parameters to their declaring types.
    /// </para> 
    /// <para>The query on the <i>right</i> part of <see cref="SelectTypes"/> is executed concurrently.</para>. 
    /// </remarks>
    new ITaggedQuery<INamedType, TTag> SelectTypes( bool includeNestedTypes = true );

    /// <summary>
    /// Selects all types, among those enclosed in declarations of the current set, that derive from or implement a given <see cref="Type"/>. 
    /// </summary>
    /// <remarks>
    /// <para>The query on the <i>right</i> part of <see cref="SelectTypes"/> is executed concurrently.</para>. 
    /// </remarks>
    new ITaggedQuery<INamedType, TTag> SelectTypesDerivedFrom( Type type, DerivedTypesOptions options = DerivedTypesOptions.Default );

    /// <summary>
    /// Selects all types, among those enclosed in declarations of the current set, that derive from or implement a given <see cref="INamedType"/>. 
    /// </summary>
    /// <remarks>
    /// <para>The query on the <i>right</i> part of <see cref="SelectTypes"/> is executed concurrently.</para>. 
    /// </remarks>
    new ITaggedQuery<INamedType, TTag> SelectTypesDerivedFrom( INamedType type, DerivedTypesOptions options = DerivedTypesOptions.Default );

    /// <summary>
    /// Filters the set of declarations based on a predicate.
    /// </summary>
    new ITaggedQuery<TDeclaration, TTag> Where( Func<TDeclaration, bool> predicate );

    /// <summary>
    /// Filters the set of declarations based on a predicate.
    /// This overload does supplies the tag to the <paramref name="predicate"/> delegate.
    /// </summary>
    ITaggedQuery<TDeclaration, TTag> Where( Func<TDeclaration, TTag, bool> predicate );

    new ITaggedQuery<TOut, TTag> OfType<TOut>()
        where TOut : class, IDeclaration;

    /// <summary>
    /// Projects the declarations in the current set by replacing the tag of each declaration.
    /// </summary>
    new ITaggedQuery<TDeclaration, TNewTag> WithTag<TNewTag>( Func<TDeclaration, TNewTag> getTag );

    /// <summary>
    /// Projects the declarations in the current set by replacing the tag of each declaration.
    /// This overload does supplies the old tag to the <paramref name="getTag"/> delegate.
    /// </summary>
    ITaggedQuery<TDeclaration, TNewTag> WithTag<TNewTag>( Func<TDeclaration, TTag, TNewTag> getTag );

    [Obsolete( "This method has been renamed 'WithTag'." )]
    new ITaggedQuery<TDeclaration, TNewTag> Tag<TNewTag>( Func<TDeclaration, TNewTag> getTag );

    [Obsolete( "This method has been renamed 'WithTag'." )]
    ITaggedQuery<TDeclaration, TNewTag> Tag<TNewTag>( Func<TDeclaration, TTag, TNewTag> getTag );
}