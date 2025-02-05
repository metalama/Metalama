// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;
using System.Threading;

namespace Metalama.Framework.Engine.Utilities.Threading;

[PublicAPI]
public class TestableCancellationTokenSource : IDisposable
{
    public CancellationTokenSource CancellationTokenSource { get; }

    public TestableCancellationToken Token { get; }

    public TestableCancellationTokenSource()
    {
        this.CancellationTokenSource = new CancellationTokenSource();
        this.Token = new TestableCancellationToken( this.CancellationTokenSource.Token, this );
    }

    public TestableCancellationTokenSource( TimeSpan timeout )
    {
        this.CancellationTokenSource = new CancellationTokenSource( timeout );
        this.Token = new TestableCancellationToken( this.CancellationTokenSource.Token, this );
    }

    public virtual void OnPossibleCancellationPoint() { }

    public virtual void Dispose()
    {
        this.CancellationTokenSource.Dispose();
    }
}