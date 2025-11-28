// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Introspection;

/// <summary>
/// Specifies which kinds of child declarations to include when querying references.
/// </summary>
/// <seealso href="@introspection-api"/>
[Flags]
public enum IntrospectionChildKinds
{
    /// <summary>
    /// No child declarations.
    /// </summary>
    None = 0,

    /// <summary>
    /// Include derived types.
    /// </summary>
    DerivedType = 1,

    /// <summary>
    /// Include declarations contained within the declaration.
    /// </summary>
    ContainingDeclaration = 2,

    /// <summary>
    /// Include all child declarations (both derived types and contained declarations).
    /// </summary>
    All = DerivedType | ContainingDeclaration
}