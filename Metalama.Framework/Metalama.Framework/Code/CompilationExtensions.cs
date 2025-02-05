// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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