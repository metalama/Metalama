// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.DesignTime.Contracts;

internal sealed class DisposeAction( Action action ) : IDisposable
{
    public void Dispose() => action();
}