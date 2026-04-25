// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Metrics;
using Metalama.Framework.Services;

namespace Metalama.Extensions.Metrics;

/// <summary>
/// Extension methods for registering metric providers with a service builder.
/// </summary>
[CompileTime]
public static class ServiceBuilderExtensions
{
    /// <summary>
    /// Registers the standard metric providers (<see cref="IMetricProvider{T}"/> for <see cref="SyntaxNodesCount"/>,
    /// <see cref="StatementsCount"/>, and <see cref="LinesOfCode"/>) with the service builder.
    /// </summary>
    /// <param name="builder">The service builder to register the metric providers with.</param>
    /// <remarks>
    /// Call this method to enable consumption of the standard metrics from the Workspaces API. For example:
    /// <code>
    /// WorkspaceCollection.Default.ServiceBuilder.AddMetrics();
    /// var workspace = await WorkspaceCollection.Default.LoadAsync("MyProject.csproj");
    /// var count = method.Metrics().Get&lt;SyntaxNodesCount&gt;();
    /// </code>
    /// </remarks>
    /// <seealso cref="SyntaxNodesCount"/>
    /// <seealso cref="StatementsCount"/>
    /// <seealso cref="LinesOfCode"/>
    public static void AddMetrics( this ServiceProviderBuilder<IProjectService> builder )
    {
        builder.Add<IMetricProvider<SyntaxNodesCount>>( _ => new SyntaxNodesCountMetricProvider() );
        builder.Add<IMetricProvider<StatementsCount>>( _ => new StatementsCountMetricProvider() );
        builder.Add<IMetricProvider<LinesOfCode>>( _ => new LinesOfCodeMetricProvider() );
    }
}