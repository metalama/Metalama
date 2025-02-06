// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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