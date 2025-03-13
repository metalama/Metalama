// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Code;

public static class CompilationExtensions
{
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