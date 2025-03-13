// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Utilities.Threading;

internal sealed class RandomizingSingleThreadedTaskRunner : SingleThreadedTaskRunner
{
    private readonly IRandomNumberProvider _randomNumberProvider;

    public RandomizingSingleThreadedTaskRunner( GlobalServiceProvider serviceProvider )
    {
        this._randomNumberProvider = serviceProvider.GetRequiredService<IRandomNumberProvider>();
    }

    protected override IEnumerable<T> GetOrderedItems<T>( IEnumerable<T> items )
    {
        return items.Select( x => (Value: x, Order: this._randomNumberProvider.GetNextDouble()) ).OrderBy( p => p.Order ).Select( p => p.Value );
    }
}