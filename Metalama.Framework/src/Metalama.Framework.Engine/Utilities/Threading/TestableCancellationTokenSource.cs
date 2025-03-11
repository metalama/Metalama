// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

    public TestableCancellationTokenSource( CancellationToken linkedToken )
    {
        this.CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource( linkedToken );
        this.Token = new TestableCancellationToken( this.CancellationTokenSource.Token, this );
    }

    public virtual void OnPossibleCancellationPoint() { }

    public virtual void Dispose()
    {
        this.CancellationTokenSource.Dispose();
    }
}