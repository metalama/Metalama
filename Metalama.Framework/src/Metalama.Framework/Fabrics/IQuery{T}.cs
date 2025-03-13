// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using System.Collections.Generic;

#pragma warning disable SA1649

namespace Metalama.Framework.Fabrics;

/// <summary>
/// Represents query of declarations to which aspects, validators, diagnostics and code fix suggestions can be added.
/// This interface exposes LINQ-like methods that can be combined in complex queries.
/// </summary>
/// <typeparam name="TDeclaration">The type of declarations in the current set.</typeparam>
public interface IQuery<out TDeclaration> : IQuery
    where TDeclaration : class, IDeclaration
{
    /// <summary>
    /// Projects each declaration of the current set to an <see cref="IEnumerable{T}"/> (typically a list of child declarations) and flattens the resulting sequences into one set.
    /// </summary>
    /// <remarks>
    /// <para>The query on the <i>right</i> part of <see cref="SelectMany{TMember}"/> is executed concurrently. It is therefore preferable to use the <see cref="Where"/>, <see cref="Select{TMember}"/>
    /// or <see cref="SelectMany{TMember}"/> methods of the current interface instead of using the equivalent system methods inside the <paramref name="selector"/> query.</para>
    /// </remarks>
    IQuery<TMember> SelectMany<TMember>( Func<TDeclaration, IEnumerable<TMember>> selector )
        where TMember : class, IDeclaration;

    /// <summary>
    /// Projects each declaration of the current set into a new declaration.
    /// </summary>
    IQuery<TMember> Select<TMember>( Func<TDeclaration, TMember> selector )
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
    IQuery<INamedType> SelectTypes( bool includeNestedTypes = true );

    /// <summary>
    /// Selects all types, among those enclosed in declarations of the current set, that derive from or implement a given <see cref="Type"/>. 
    /// </summary>
    /// <remarks>
    /// <para>The query on the <i>right</i> part of <see cref="SelectTypes"/> is executed concurrently.</para>. 
    /// </remarks>
    IQuery<INamedType> SelectTypesDerivedFrom( Type type, DerivedTypesOptions options = DerivedTypesOptions.Default );

    /// <summary>
    /// Selects all types, among those enclosed in declarations of the current set, that derive from or implement a given <see cref="INamedType"/>. 
    /// </summary>
    /// <remarks>
    /// <para>The query on the <i>right</i> part of <see cref="SelectTypes"/> is executed concurrently.</para>. 
    /// </remarks>
    IQuery<INamedType> SelectTypesDerivedFrom( INamedType type, DerivedTypesOptions options = DerivedTypesOptions.Default );

    /// <summary>
    /// Filters the set of declarations based on a predicate.
    /// </summary>
    IQuery<TDeclaration> Where( Func<TDeclaration, bool> predicate );

    /// <summary>
    /// Selects all declarations of a given type.
    /// </summary>
    /// <typeparam name="TOut">The type of selected declarations.</typeparam>
    IQuery<TOut> OfType<TOut>()
        where TOut : class, IDeclaration;

    [Obsolete( "The method has been renamed WithTag." )]
    ITaggedQuery<TDeclaration, TTag> Tag<TTag>( Func<TDeclaration, TTag> getTag );

    /// <summary>
    /// Projects the declarations in the current set by adding a tag for each declaration, and returns a <see cref="ITaggedQuery{TDeclaration,TTag}"/>.
    /// Methods of this interface have overloads that accept this tag. 
    /// </summary>
    ITaggedQuery<TDeclaration, TTag> WithTag<TTag>( Func<TDeclaration, TTag> getTag );

    /// <summary>
    /// Evaluates the current query into a collection. This method should only be used for debugging or testing purposes.
    /// </summary>
    [Obsolete( "The ToCollection method is meant for debugging and testing only. It does not work at design time." )]
    IReadOnlyCollection<TDeclaration> ToCollection( ICompilation? compilation = null );
}