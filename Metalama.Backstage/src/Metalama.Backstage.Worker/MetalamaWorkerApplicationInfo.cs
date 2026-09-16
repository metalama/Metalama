// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Worker;

namespace Metalama.Backstage;

/// <summary>
/// The description of the worker process of Metalama.
/// </summary>
internal sealed class MetalamaWorkerApplicationInfo : BackstageWorkerApplicationInfo
{
    public MetalamaWorkerApplicationInfo() : base( typeof(MetalamaWorkerApplicationInfo).Assembly, MetalamaProduct.Instance ) { }

    /// <inheritdoc />
    public override string Name => "Metalama Backstage Worker";
}
