// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Utilities.Caching;

#pragma warning disable SA1402

public sealed class TimeBasedCache<TKey, TValue> : TimeBasedCache<TKey, TValue, int>
    where TKey : notnull
{
    public TimeBasedCache( TimeSpan rotationTimeSpan, IEqualityComparer<TKey>? keyComparer = null ) : base( rotationTimeSpan, keyComparer ) { }

    protected override bool Validate( TKey key, in Item item ) => true;

    protected override int GetTag( TKey key ) => 0;
}

public class TimeBasedCache<TKey, TValue, TTag> : Cache<TKey, TValue, TTag>
    where TKey : notnull
{
    // We use a Stopwatch instead of DateTime.Now because DateTime.Now is slower than Stopwatch.

    private readonly long _rotationTimeSpan;
    private long _lastRotationTimestamp = SharedStopwatch.Instance.ElapsedMilliseconds;

    protected TimeBasedCache( TimeSpan rotationTimeSpan, IEqualityComparer<TKey>? keyComparer = null ) : base( keyComparer )
    {
        this._rotationTimeSpan = (long) rotationTimeSpan.TotalMilliseconds;
    }

    protected override bool ShouldRotate() => this._lastRotationTimestamp + this._rotationTimeSpan < SharedStopwatch.Instance.ElapsedMilliseconds;

    protected override void OnRotated() => this._lastRotationTimestamp = SharedStopwatch.Instance.ElapsedMilliseconds;
}