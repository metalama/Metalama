// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.Advising;

/// <summary>
/// Implements <see cref="IObjectReaderFactory"/> for a project with the <see cref="ObjectReaderFactory"/> of the process, which caches the
/// readers of each type.
/// </summary>
internal sealed class ObjectReaderFactoryService : IObjectReaderFactory
{
    /// <summary>
    /// The service provider of the project, which the readers use to read the values of objects.
    /// </summary>
    private readonly ProjectServiceProvider _serviceProvider;

    /// <summary>
    /// The factory of the process.
    /// </summary>
    private readonly ObjectReaderFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectReaderFactoryService"/> class.
    /// </summary>
    public ObjectReaderFactoryService( in ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
        this._factory = serviceProvider.Global.GetRequiredService<ObjectReaderFactory>();
    }

    /// <inheritdoc />
    public IObjectReader GetReader( object? instance ) => this._factory.GetReader( this._serviceProvider, instance );
}
