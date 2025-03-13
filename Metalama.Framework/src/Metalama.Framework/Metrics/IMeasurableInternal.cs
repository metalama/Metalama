// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Metrics
{
    internal interface IMeasurableInternal : IMeasurable
    {
        // This method is weakly typed, compared to the ExtensibleExtensions.Extensions.Get. This is why it is internal.
        T GetMetric<T>()
            where T : IMetric;
    }
}