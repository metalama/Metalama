// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Utilities.Threading;

/// <summary>
/// An abstraction that exposes the <see cref="RunSynchronously(Func{Task},CancellationToken)"/> method.
/// It could, in theory, have different implementations, for instance using <c>Microsoft.VisualStudio.Threading</c>,
/// but in practice the only implementation uses the system methods.
/// </summary>
[PublicAPI]
public interface ITaskRunner : IGlobalService
{
    void RunSynchronously( Func<Task> func, CancellationToken cancellationToken = default );

    void RunSynchronously( Func<ValueTask> func, CancellationToken cancellationToken = default );

    T RunSynchronously<T>( Func<Task<T>> func, CancellationToken cancellationToken = default );

    T RunSynchronously<T>( Func<ValueTask<T>> func, CancellationToken cancellationToken = default );
}