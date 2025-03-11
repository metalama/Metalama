// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Activities;
using JetBrains.Annotations;

namespace Flashtrace.Options;

/// <summary>
/// Kinds of <see cref="LogActivity{TActivityDescription}"/>.
/// </summary>
[PublicAPI]
public enum LogActivityKind
{
    /// <summary>
    /// A general-purpose activity, as typically created by user code.
    /// </summary>
    Default,

    /// <summary>
    /// Activity of creating a <see cref="System.Threading.Tasks.Task"/>.
    /// </summary>
    TaskLauncher,

    /// <summary>
    /// Code running in a <see cref="System.Threading.Tasks.Task"/>.
    /// </summary>
    Task,

    /// <summary>
    /// Activity of waiting for a <see cref="System.Threading.Tasks.Task"/> or another dispatcher object.
    /// </summary>
    Wait,

    /// <summary>
    /// An outgoing request. The property "RequestId" is expected to be defined.
    /// </summary>
    OutgoingRequest,

    /// <summary>
    /// An incoming request of the service (typically a web request). A transaction boundary.
    /// </summary>
    IncomingRequest,

    /// <summary>
    /// A custom transaction.
    /// </summary>
    Transaction
}