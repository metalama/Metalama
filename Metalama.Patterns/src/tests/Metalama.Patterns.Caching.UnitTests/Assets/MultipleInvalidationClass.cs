// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Aspects;

namespace Metalama.Patterns.Caching.Tests.Assets;

public class MultipleInvalidationClass
{
    private int _id;

    [Cache]
    public int GetId1()
    {
        CachingService.Default.AddDependency( nameof(this.GetId1) );

        return this._id;
    }

    [Cache]
    public int GetId2()
    {
        CachingService.Default.AddDependency( nameof(this.GetId2) );

        return this._id;
    }

    public void Increment()
    {
        this._id++;

        CachingService.Default.Invalidate( nameof(this.GetId1), nameof(this.GetId2) );
    }
}