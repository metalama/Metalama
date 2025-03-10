// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Metrics;
using Metalama.Framework.Services;
using System;
using System.Collections.Concurrent;

namespace Metalama.Framework.Engine.Metrics;

/// <summary>
/// Manages the metric providers and routes metric requests to them.
/// </summary>
internal sealed class MetricManager : IProjectService
{
    private readonly ProjectServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, object?> _metricProviders = new();

    internal MetricManager( in ProjectServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets a given metric for a given object.
    /// </summary>
    /// <param name="measurable">The object for which the metric is requested.</param>
    /// <typeparam name="T">The type of requested object.</typeparam>
    public T GetMetric<T>( IMeasurable measurable )
        where T : IMetric
    {
        var provider = this._metricProviders.GetOrAdd( typeof(T), static ( _, me ) => me.GetExtensionProvider<T>(), this )
                       ??
                       throw new InvalidOperationException( $"No extension provider registered for {typeof(T).Name}" );

        return ((IMetricProvider<T>) provider).GetMetric( measurable );
    }

    private object? GetExtensionProvider<T>()
        where T : IMetric
        => this._serviceProvider.GetService<IMetricProvider<T>>();
}