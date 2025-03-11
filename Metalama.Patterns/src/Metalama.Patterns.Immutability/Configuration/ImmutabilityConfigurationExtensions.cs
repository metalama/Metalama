// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Immutability.Configuration;

/// <summary>
/// Provides extension methods for configuring the immutability of types.
/// </summary>
[CompileTime]
[PublicAPI]
public static class ImmutabilityConfigurationExtensions
{
    /// <summary>
    /// Configures the immutability of a given namespace by supplying an <see cref="ImmutabilityKind"/>.
    /// </summary>
    public static void ConfigureImmutability( this IQuery<INamespace> query, ImmutabilityKind immutabilityKind )
        => query.SetOptions( new ImmutabilityOptions() { Kind = immutabilityKind } );

    /// <summary>
    /// Configures the immutability of a given namespace by supplying an <see cref="IImmutabilityClassifier"/>.
    /// </summary>
    public static void ConfigureImmutability( this IQuery<INamespace> query, IImmutabilityClassifier classifier )
        => query.SetOptions( new ImmutabilityOptions() { Classifier = classifier } );

    /// <summary>
    /// Configures the immutability of a given type by supplying an <see cref="ImmutabilityKind"/>.
    /// </summary>
    public static void ConfigureImmutability( this IQuery<INamedType> query, ImmutabilityKind immutabilityKind )
        => query.SetOptions( new ImmutabilityOptions() { Kind = immutabilityKind } );

    /// <summary>
    /// Configures the immutability of a given type by supplying an <see cref="IImmutabilityClassifier"/>.
    /// </summary>
    public static void ConfigureImmutability( this IQuery<INamedType> query, IImmutabilityClassifier classifier )
        => query.SetOptions( new ImmutabilityOptions() { Classifier = classifier } );
}