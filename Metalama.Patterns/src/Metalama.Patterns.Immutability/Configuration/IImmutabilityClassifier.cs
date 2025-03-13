// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Patterns.Immutability.Configuration;

/// <summary>
/// Exposes a <see cref="GetImmutabilityKind"/> method that returns the <see cref="ImmutabilityKind"/>
/// of a given type. This interface is useful when the immutability of a type depends on its type arguments.
/// </summary>
public interface IImmutabilityClassifier : ICompileTimeSerializable
{
    /// <summary>
    /// Returns the <see cref="ImmutabilityKind"/> of a given type.
    /// </summary>
    ImmutabilityKind GetImmutabilityKind( INamedType type );
}