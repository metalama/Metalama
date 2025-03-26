// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using System;
using System.Threading;

namespace Metalama.Framework.Engine.Utilities.Threading;

public sealed class ApplicationExitManager : IGlobalService, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public void OnApplicationExiting()
    {
        this._cancellationTokenSource.Cancel();
    }

    public CancellationToken Token => this._cancellationTokenSource.Token;

    public void Dispose() => this._cancellationTokenSource.Dispose();
}