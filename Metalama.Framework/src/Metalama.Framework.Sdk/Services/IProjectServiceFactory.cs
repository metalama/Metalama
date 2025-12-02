// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Services;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Services;

/// <summary>
/// A factory interface that creates <see cref="IProjectService"/> instances to be added to the project's service provider.
/// Implement this interface and export it using <see cref="Extensibility.ExportExtensionAttribute"/> with
/// <see cref="Extensibility.ExtensionKinds.ServiceFactory"/> to provide custom services that can be consumed by aspects at compile time.
/// </summary>
/// <example>
/// <code>
/// [assembly: ExportExtension(typeof(MyServiceFactory), ExtensionKinds.ServiceFactory)]
///
/// internal class MyServiceFactory : IProjectServiceFactory
/// {
///     public IEnumerable&lt;IProjectService&gt; CreateServices(in ProjectServiceProvider serviceProvider)
///     {
///         yield return new MyCustomService(serviceProvider);
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="Extensibility.ExportExtensionAttribute"/>
/// <seealso cref="Extensibility.ExtensionKinds.ServiceFactory"/>
[CompileTime]
public interface IProjectServiceFactory
{
    /// <summary>
    /// Creates the project services provided by this factory. The returned services will be added to the
    /// project's service provider and can be resolved by aspects using <see cref="ProjectServiceProvider.GetService{T}"/>.
    /// </summary>
    /// <param name="serviceProvider">The current project service provider, which can be used to resolve dependencies.</param>
    /// <returns>An enumerable of <see cref="IProjectService"/> instances to be registered.</returns>
    IEnumerable<IProjectService> CreateServices( in ProjectServiceProvider serviceProvider );
}