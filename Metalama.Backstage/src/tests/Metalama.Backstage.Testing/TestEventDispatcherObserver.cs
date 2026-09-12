// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Testing;

/// <summary>
/// An <see cref="IEventDispatcherObserver"/> that records the failures of the dispatcher, so that a test can assert
/// that every published event was handled and that no subscriber threw.
/// </summary>
[PublicAPI]
public sealed class TestEventDispatcherObserver : IEventDispatcherObserver
{
    private readonly object _sync = new();
    private readonly List<(object Event, Exception Exception)> _failures = [];
    private readonly List<object> _unhandledEvents = [];

    /// <summary>
    /// Gets the events whose subscriber threw, with the exception.
    /// </summary>
    public IReadOnlyList<(object Event, Exception Exception)> Failures
    {
        get
        {
            lock ( this._sync )
            {
                return this._failures.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets the events that were published while nothing had subscribed to their type.
    /// </summary>
    public IReadOnlyList<object> UnhandledEvents
    {
        get
        {
            lock ( this._sync )
            {
                return this._unhandledEvents.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public void OnSubscriberFailed( object @event, Exception exception )
    {
        lock ( this._sync )
        {
            this._failures.Add( (@event, exception) );
        }
    }

    /// <inheritdoc />
    public void OnEventUnhandled( object @event )
    {
        lock ( this._sync )
        {
            this._unhandledEvents.Add( @event );
        }
    }
}
