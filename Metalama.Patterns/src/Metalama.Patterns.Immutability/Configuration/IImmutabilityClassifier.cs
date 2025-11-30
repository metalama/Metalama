// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Patterns.Immutability.Configuration;

/// <summary>
/// Exposes a method that determines the <see cref="ImmutabilityKind"/> of a type dynamically. This interface
/// is useful for generic types whose immutability depends on their type arguments.
/// </summary>
/// <remarks>
/// <para>Implementations of this interface must be compile-time serializable because they may need to
/// be persisted across compilation steps or project boundaries.</para>
/// <para>Use <see cref="ImmutabilityConfigurationExtensions.ConfigureImmutability(Metalama.Framework.Fabrics.IQuery{INamedType},IImmutabilityClassifier)"/>
/// to configure a classifier for specific types or namespaces.</para>
/// </remarks>
/// <seealso cref="ImmutabilityKind"/>
/// <seealso cref="ImmutabilityConfigurationExtensions"/>
/// <seealso href="@immutability"/>
public interface IImmutabilityClassifier : ICompileTimeSerializable
{
    /// <summary>
    /// Returns the <see cref="ImmutabilityKind"/> of the specified type.
    /// </summary>
    /// <param name="type">The type to classify.</param>
    /// <returns>The <see cref="ImmutabilityKind"/> of the type.</returns>
    ImmutabilityKind GetImmutabilityKind( INamedType type );
}