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
/// Provides extension methods for configuring the immutability of types via fabrics.
/// </summary>
/// <remarks>
/// <para>These methods allow you to assign an <see cref="ImmutabilityKind"/> to types for which you cannot add
/// the <see cref="ImmutableAttribute"/> aspect directly, such as types from external libraries.</para>
/// <para>You can pass either a fixed <see cref="ImmutabilityKind"/> if the type always has the same immutability,
/// or an <see cref="IImmutabilityClassifier"/> to determine the immutability dynamically, which is useful for
/// generic types whose immutability depends on their type arguments.</para>
/// </remarks>
/// <seealso cref="ImmutableAttribute"/>
/// <seealso cref="ImmutabilityKind"/>
/// <seealso cref="IImmutabilityClassifier"/>
/// <seealso href="@immutability"/>
[CompileTime]
[PublicAPI]
public static class ImmutabilityConfigurationExtensions
{
    /// <summary>
    /// Configures the immutability of a namespace by supplying an <see cref="ImmutabilityKind"/>.
    /// All types in the namespace will be assigned this immutability kind.
    /// </summary>
    /// <param name="query">A query selecting the namespace(s) to configure.</param>
    /// <param name="immutabilityKind">The <see cref="ImmutabilityKind"/> to assign to types in the namespace.</param>
    public static void ConfigureImmutability( this IQuery<INamespace> query, ImmutabilityKind immutabilityKind )
        => query.SetOptions( new ImmutabilityOptions() { Kind = immutabilityKind } );

    /// <summary>
    /// Configures the immutability of a namespace by supplying an <see cref="IImmutabilityClassifier"/>.
    /// The classifier will be used to determine the immutability of each type in the namespace dynamically.
    /// </summary>
    /// <param name="query">A query selecting the namespace(s) to configure.</param>
    /// <param name="classifier">The classifier that determines the <see cref="ImmutabilityKind"/> for each type.</param>
    public static void ConfigureImmutability( this IQuery<INamespace> query, IImmutabilityClassifier classifier )
        => query.SetOptions( new ImmutabilityOptions() { Classifier = classifier } );

    /// <summary>
    /// Configures the immutability of a type by supplying an <see cref="ImmutabilityKind"/>.
    /// </summary>
    /// <param name="query">A query selecting the type(s) to configure.</param>
    /// <param name="immutabilityKind">The <see cref="ImmutabilityKind"/> to assign to the type.</param>
    public static void ConfigureImmutability( this IQuery<INamedType> query, ImmutabilityKind immutabilityKind )
        => query.SetOptions( new ImmutabilityOptions() { Kind = immutabilityKind } );

    /// <summary>
    /// Configures the immutability of a type by supplying an <see cref="IImmutabilityClassifier"/>.
    /// The classifier will be used to determine the immutability of the type dynamically, which is useful for
    /// generic types whose immutability depends on their type arguments.
    /// </summary>
    /// <param name="query">A query selecting the type(s) to configure.</param>
    /// <param name="classifier">The classifier that determines the <see cref="ImmutabilityKind"/> for each type.</param>
    public static void ConfigureImmutability( this IQuery<INamedType> query, IImmutabilityClassifier classifier )
        => query.SetOptions( new ImmutabilityOptions() { Classifier = classifier } );
}