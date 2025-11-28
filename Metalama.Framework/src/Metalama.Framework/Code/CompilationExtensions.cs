// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Code;

/// <summary>
/// Extension methods for the <see cref="ICompilation"/> interface.
/// </summary>
/// <seealso cref="ICompilation"/>
/// <seealso cref="IDeclaration"/>
/// <seealso cref="IAttribute"/>
public static class CompilationExtensions
{
    /// <summary>
    /// Gets all declarations in the compilation that are annotated with a specific attribute type.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to search for. Must be a compile-time or run-time-or-compile-time type.</typeparam>
    /// <param name="compilation">The compilation to search.</param>
    /// <param name="predicate">An optional predicate to filter attributes based on their constructed values.</param>
    /// <param name="includeDerivedTypes">Whether to include attributes whose type derives from <typeparamref name="TAttribute"/>.</param>
    /// <returns>An enumerable of declarations that have the specified attribute.</returns>
    public static IEnumerable<IDeclaration> GetDeclarationsWithAttribute<TAttribute>(
        this ICompilation compilation,
        Func<TAttribute, bool>? predicate = null,
        bool includeDerivedTypes = true )
    {
        var attributes = ((ICompilationInternal) compilation)
            .GetAllAttributesOfType( typeof(TAttribute), includeDerivedTypes );

        if ( predicate != null )
        {
            attributes = attributes.Where(
                a => a.TryConstruct( out var constructedAttribute ) && constructedAttribute is TAttribute typedAttribute
                                                                    && predicate( typedAttribute ) );
        }

        return
            attributes
                .Select( a => a.ContainingDeclaration )
                .Distinct();
    }

    /// <summary>
    /// Gets all declarations in the compilation that are annotated with a specific attribute type, specified as a reflection <see cref="Type"/>.
    /// </summary>
    /// <param name="compilation">The compilation to search.</param>
    /// <param name="attributeType">The attribute type to search for.</param>
    /// <param name="predicate">An optional predicate to filter attributes based on their <see cref="IAttribute"/> representation.</param>
    /// <param name="includeDerivedTypes">Whether to include attributes whose type derives from <paramref name="attributeType"/>.</param>
    /// <returns>An enumerable of declarations that have the specified attribute.</returns>
    public static IEnumerable<IDeclaration> GetDeclarationsWithAttribute(
        this ICompilation compilation,
        Type attributeType,
        Func<IAttribute, bool>? predicate = null,
        bool includeDerivedTypes = true )
    {
        var attributes = ((ICompilationInternal) compilation)
            .GetAllAttributesOfType( attributeType, includeDerivedTypes );

        if ( predicate != null )
        {
            attributes = attributes.Where( predicate );
        }

        return
            attributes
                .Select( a => a.ContainingDeclaration )
                .Distinct();
    }
}