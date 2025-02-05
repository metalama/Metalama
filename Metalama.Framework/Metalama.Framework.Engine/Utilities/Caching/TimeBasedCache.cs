// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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