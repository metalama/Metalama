// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Threading;

/// <summary>
/// Describes something that happened to a named lock, reported through
/// <see cref="NamedLockService.LockEventReported"/>.
/// </summary>
/// <remarks>
/// <para>
/// The lock implementation reports its activity through an event rather than through a logger, because it is
/// compiled into assemblies that cannot reference the logging services. Each consumer adapts these events to
/// whatever diagnostic facility it has.
/// </para>
/// <para>
/// A test implementation of <see cref="INamedLockService"/> is expected to report the same events, so that the
/// trace of a load test running against real operating system objects can be compared with the trace of a
/// deterministic test running against an in-memory implementation.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class LockEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LockEventArgs"/> class.
    /// </summary>
    /// <param name="kind">The kind of event.</param>
    /// <param name="name">The name of the lock.</param>
    /// <param name="duration">The duration relevant to <paramref name="kind"/>, or <see cref="TimeSpan.Zero"/>.</param>
    /// <param name="detail">Additional information, typically the message of an exception.</param>
    public LockEventArgs( LockEventKind kind, string name, TimeSpan duration = default, string? detail = null )
    {
        this.Kind = kind;
        this.Name = name;
        this.Duration = duration;
        this.Detail = detail;
        this.ThreadId = Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// Gets the kind of event.
    /// </summary>
    public LockEventKind Kind { get; }

    /// <summary>
    /// Gets the name of the lock, as it was given to <see cref="INamedLockService.GetLock"/>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the time the lock was held, for <see cref="LockEventKind.Released"/> and
    /// <see cref="LockEventKind.HeldTooLong"/>, or the time spent waiting, for
    /// <see cref="LockEventKind.Acquired"/> and <see cref="LockEventKind.TimedOut"/>. It is
    /// <see cref="TimeSpan.Zero"/> for the other kinds.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets additional information, typically the message of an exception, or <see langword="null"/>.
    /// </summary>
    public string? Detail { get; }

    /// <summary>
    /// Gets the managed identifier of the thread that reported the event.
    /// </summary>
    public int ThreadId { get; }

    /// <summary>
    /// Gets a value indicating whether the event reports a situation that the user should be told about, as
    /// opposed to a routine acquisition or release.
    /// </summary>
    /// <remarks>
    /// This spares each consumer from having to classify the kinds itself when it maps the events to its own
    /// diagnostic levels.
    /// </remarks>
    public bool IsWarning => IsWarningKind( this.Kind );

    /// <summary>
    /// Determines whether a kind of event reports a situation that the user should be told about.
    /// </summary>
    /// <param name="kind">The kind of event.</param>
    /// <returns><see langword="true"/> if the event is worth a warning.</returns>
    /// <remarks>
    /// This is exposed on the kind, and not only on the event, so that a subscriber can decide whether it is
    /// interested before an event object has been created for it. See
    /// <see cref="NamedLockService.ReportFilter"/>.
    /// </remarks>
    public static bool IsWarningKind( LockEventKind kind )
        => kind is LockEventKind.Degraded or LockEventKind.Abandoned or LockEventKind.HeldTooLong or LockEventKind.ReentrancyDetected;

    /// <inheritdoc />
    public override string ToString()
        => $"{this.Kind} '{this.Name}' on thread {this.ThreadId}"
           + (this.Duration == TimeSpan.Zero ? "" : $" after {this.Duration.TotalMilliseconds:F0} ms")
           + (this.Detail == null ? "" : $": {this.Detail}");
}

/// <summary>
/// The kinds of <see cref="LockEventArgs"/>.
/// </summary>
[PublicAPI]
public enum LockEventKind
{
    /// <summary>
    /// The operating system object backing the lock was opened or created.
    /// </summary>
    Created,

    /// <summary>
    /// The operating system refused to provide a named object, so the lock excludes only the threads of the
    /// current process. See the remarks of <see cref="INamedLockService.GetLock"/>.
    /// </summary>
    Degraded,

    /// <summary>
    /// An acquisition had to wait, because another thread or process owned the lock.
    /// </summary>
    Blocked,

    /// <summary>
    /// The lock was acquired.
    /// </summary>
    Acquired,

    /// <summary>
    /// The lock was acquired after its previous owner terminated without releasing it. The state protected by
    /// the lock may be inconsistent.
    /// </summary>
    Abandoned,

    /// <summary>
    /// The lock could not be acquired within the requested timeout.
    /// </summary>
    TimedOut,

    /// <summary>
    /// The lock was released.
    /// </summary>
    Released,

    /// <summary>
    /// The lock was held for an unexpectedly long time. This is the symptom that issue 1847 was reported for,
    /// so it is worth keeping visible in the field.
    /// </summary>
    HeldTooLong,

    /// <summary>
    /// A thread attempted to acquire a lock that it already held. Named locks are not reentrant: see the
    /// remarks of <see cref="INamedLock.TryAcquire"/>.
    /// </summary>
    ReentrancyDetected
}
