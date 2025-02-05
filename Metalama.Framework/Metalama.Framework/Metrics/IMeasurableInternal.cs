// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Metrics
{
    internal interface IMeasurableInternal : IMeasurable
    {
        // This method is weakly typed, compared to the ExtensibleExtensions.Extensions.Get. This is why it is internal.
        T GetMetric<T>()
            where T : IMetric;
    }
}