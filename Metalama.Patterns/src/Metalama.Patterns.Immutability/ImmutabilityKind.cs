// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Immutability;

/// <summary>
/// Specifies the kind of immutability of a type.
/// </summary>
/// <remarks>
/// <para>This enumeration is used with the <see cref="ImmutableAttribute"/> aspect and the
/// <see cref="Configuration.ImmutabilityConfigurationExtensions.ConfigureImmutability(Metalama.Framework.Fabrics.IQuery{Metalama.Framework.Code.INamedType},ImmutabilityKind)"/>
/// fabric extension method to specify or query the immutability characteristics of types.</para>
/// <para>Deep immutability provides stronger guarantees than shallow immutability and is required by
/// certain code analyses, such as those performed by the <c>Metalama.Patterns.Observability</c> package.</para>
/// </remarks>
/// <seealso cref="ImmutableAttribute"/>
/// <seealso cref="ImmutabilityExtensions.GetImmutabilityKind"/>
/// <seealso href="@immutability"/>
[RunTimeOrCompileTime]
public enum ImmutabilityKind
{
    /// <summary>
    /// The type is mutable.
    /// </summary>
    None,

    /// <summary>
    /// The type itself is immutable (all instance fields are read-only and no automatic property has a setter),
    /// but some of its fields or properties may reference mutable objects.
    /// </summary>
    Shallow,

    /// <summary>
    /// The type is deeply immutable: all instance fields and automatic properties are of a deeply immutable type,
    /// ensuring that all objects reachable by recursively evaluating fields or properties are also immutable.
    /// </summary>
    Deep
}