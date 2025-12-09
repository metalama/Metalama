// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Interface for handling exceptions that occur during RPC operations. Register an implementation
/// in the service provider to receive notifications of RPC-related errors.
/// </summary>
[PublicAPI]
public interface IRpcExceptionHandler
{
    /// <summary>
    /// Called when an exception occurs during an RPC operation.
    /// </summary>
    /// <param name="e">The exception that occurred.</param>
    /// <param name="logger">The logger associated with the endpoint.</param>
    /// <param name="isDisposing">Whether the exception occurred during disposal.</param>
    void OnException( Exception e, ILogger logger, bool isDisposing );
}