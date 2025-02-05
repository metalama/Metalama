// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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