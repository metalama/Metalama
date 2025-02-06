// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Backends.Azure;

internal sealed class AzureCacheSynchronizerExceptionEventArgs : EventArgs
{
    [UsedImplicitly]
    public Exception Exception { get; }

    public AzureCacheSynchronizerExceptionEventArgs( Exception exception )
    {
        this.Exception = exception;
    }
}