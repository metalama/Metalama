// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Backstage.Threading;
using System;

namespace Metalama.Framework.CompilerExtensions;

/// <summary>
/// Creates a <see cref="NamedLockService"/> in an assembly that starts no Backstage service.
/// </summary>
/// <remarks>
/// <para>
/// The constructor of <see cref="NamedLockService"/> resolves an <see cref="INamedLockServiceEnvironment"/> from a
/// service provider. The assemblies that compile this file cannot reference <c>Metalama.Backstage</c>, which normally
/// registers this service, so this class provides a service provider that contains only this service.
/// </para>
/// <para>
/// This file is compiled into <c>Metalama.Framework.CompilerExtensions</c> and into
/// <c>Metalama.Framework.DesignTime.Contracts</c>. Both assemblies merge the <c>SharpCrafters.Backstage.Threading</c>
/// package and declare its types as internal.
/// </para>
/// </remarks>
internal static class StandaloneNamedLockService
{
    /// <summary>
    /// The prefix of the names of the machine-wide locks of Metalama. It is the value that <c>MetalamaProduct</c>
    /// registers in <c>Metalama.Backstage</c>.
    /// </summary>
    private const string _globalLockNamePrefix = "Global\\Metalama_";

    /// <summary>
    /// Creates a new <see cref="NamedLockService"/>.
    /// </summary>
    /// <returns>A new instance of <see cref="NamedLockService"/>.</returns>
    public static NamedLockService Create() => new( new ServiceProvider() );

    /// <summary>
    /// A service provider that contains only an <see cref="INamedLockServiceEnvironment"/>.
    /// </summary>
    private sealed class ServiceProvider : IServiceProvider, INamedLockServiceEnvironment
    {
        /// <inheritdoc />
        public string GlobalLockNamePrefix => _globalLockNamePrefix;

        /// <inheritdoc />
        public object? GetService( Type serviceType ) => serviceType == typeof(INamedLockServiceEnvironment) ? this : null;
    }
}
