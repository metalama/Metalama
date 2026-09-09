// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.Types;

/// <summary>
/// Authoring forms of a union of C# 15, reported by <see cref="IUnionFacet.UnionKind"/>.
/// </summary>
/// <remarks>
/// <para>
/// The distinction matters because the language checks the restrictions on the members of a union for
/// <see cref="Declaration"/> only. An instance field, an automatic property and a field-like event are forbidden in a
/// union declaration and are allowed in the attribute form.
/// </para>
/// </remarks>
/// <seealso cref="IUnionFacet.UnionKind"/>
[CompileTime]
public enum UnionKind
{
    /// <summary>
    /// The authoring form of the union is not known. This is the value reported for a union read from a referenced
    /// assembly, because the compiled form of a union is the same for the two forms below and therefore does not
    /// record which one was written.
    /// </summary>
    None = 0,

    /// <summary>
    /// The union is declared with the <c>union</c> keyword.
    /// </summary>
    Declaration,

    /// <summary>
    /// The union is a class or a struct that carries the <c>System.Runtime.CompilerServices.UnionAttribute</c>
    /// attribute.
    /// </summary>
    Attribute
}
