// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Diagnostics;
using System.Threading;

namespace Metalama.Framework.Engine.Utilities.Threading;

public readonly struct TestableCancellationToken
{
    private readonly CancellationToken _cancellationToken;

#if DEBUG
    private readonly TestableCancellationTokenSource? _testableSource;
#endif

    [PublicAPI]
    public static TestableCancellationToken None => default;

    public void ThrowIfCancellationRequested()
    {
#if DEBUG
        this._testableSource?.OnPossibleCancellationPoint();
#endif

        this._cancellationToken.ThrowIfCancellationRequested();
    }

    public static implicit operator CancellationToken( TestableCancellationToken token )
    {
#if DEBUG
        token._testableSource?.OnPossibleCancellationPoint();
#endif

        return token._cancellationToken;
    }

    [DebuggerStepThrough]
    internal TestableCancellationToken( CancellationToken cancellationToken, TestableCancellationTokenSource? testableSource = null )
    {
        this._cancellationToken = cancellationToken;

#if DEBUG
        this._testableSource = testableSource;
#endif
    }

#if DEBUG

    // Resharper disable UnusedMember.Global
    public TestableCancellationToken WithTimeout( TimeSpan delay )
    {
        var source = new TestableCancellationTokenSource( delay );

        // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
        this._cancellationToken.Register( () => source.CancellationTokenSource.Cancel() );

        return new TestableCancellationToken( source.Token, new TestableCancellationTokenSource() );
    }
#endif
}