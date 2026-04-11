// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Helpers;

/// <summary>
/// Resolves the base constructor implicitly invoked by a default-shape constructor (one without
/// an explicit <c>:base(...)</c> initializer). Used by <c>SourceConstructor</c>, <c>IntroducedConstructor</c>
/// and <c>ConstructorBuilder</c> to share the same lookup logic.
/// </summary>
internal static class BaseConstructorResolver
{
    /// <summary>
    /// Returns the base constructor implicitly invoked by a default-shape constructor on
    /// <paramref name="declaringType"/>. This is the parameterless base constructor, or — if none
    /// exists — the unique accessible base constructor whose parameters all have default values.
    /// Returns <c>null</c> if no base type exists.
    /// </summary>
    /// <remarks>
    /// The <paramref name="declaringType"/>'s <c>BaseType</c> is used directly, so if the base type
    /// is a constructed generic type (e.g. <c>BaseClass&lt;int&gt;</c>) the returned constructor's
    /// parameters are already substituted — callers do not need to re-map generic parameters.
    /// </remarks>
    public static IConstructor? GetImplicitBaseConstructor( INamedType declaringType )
    {
        var baseType = declaringType.BaseType;

        if ( baseType == null )
        {
            return null;
        }

        // The base constructor is either the parameterless constructor of the base type, or — if
        // none exists — the unique accessible constructor whose parameters all have default values.
        var parameterless = baseType.Constructors.SingleOrDefault(
            c => c.Parameters.Count == 0
                 && c.IsAccessibleFrom( declaringType ) );

        if ( parameterless != null )
        {
            return parameterless;
        }

        var possibleConstructors = baseType.Constructors
            .Where( c => c.Parameters.All( p => p.DefaultValue != null ) && c.IsAccessibleFrom( declaringType ) )
            .ToReadOnlyList();

        return possibleConstructors.Count switch
        {
            1 => possibleConstructors[0],
            0 => null,
            _ => throw new AmbiguousMatchException(
                $"The base type '{baseType}' has several constructors where all parameters are optional." )
        };
    }
}
