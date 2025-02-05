// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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