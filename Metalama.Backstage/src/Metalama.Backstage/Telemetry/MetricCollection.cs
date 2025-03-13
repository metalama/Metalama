// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.ObjectModel;

namespace Metalama.Backstage.Telemetry
{
    /// <summary>
    /// Collection of metrics (<see cref="Metric"/>).
    /// </summary>
    [Serializable]
    public sealed class MetricCollection : KeyedCollection<string, Metric>
    {
        /// <inheritdoc />
        protected override string GetKeyForItem( Metric item )
        {
            return item.Name;
        }
    }
}