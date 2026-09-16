// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Diagnostics;
using System.Reflection;

namespace Metalama.Backstage.Worker;

/// <summary>
/// The description of the worker process of a product. The executable of the product derives a class from it that
/// gives the product and the display name, and passes an instance to <see cref="BackstageWorkerProgram.RunAsync"/>.
/// </summary>
[PublicAPI]
public abstract class BackstageWorkerApplicationInfo : ApplicationInfoBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackstageWorkerApplicationInfo"/> class.
    /// </summary>
    /// <param name="metadataAssembly">The assembly whose metadata gives the version of the worker, normally the executable.</param>
    /// <param name="product">The product family.</param>
    protected BackstageWorkerApplicationInfo( Assembly metadataAssembly, BackstageProduct product ) : base( metadataAssembly, product.Profile )
    {
        this.Product = product;
    }

    /// <summary>
    /// Gets the product family, which binds the Backstage services of the worker.
    /// </summary>
    public BackstageProduct Product { get; }

    /// <inheritdoc />
    public override ProcessKind ProcessKind => ProcessKind.BackstageWorker;

    /// <inheritdoc />
    public override bool IsLongRunningProcess => false;

    /// <inheritdoc />
    public override bool IsUnattendedProcess( ILoggerFactory loggerFactory ) => true;
}
