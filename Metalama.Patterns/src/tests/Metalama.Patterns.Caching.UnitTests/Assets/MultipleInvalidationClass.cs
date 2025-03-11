// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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