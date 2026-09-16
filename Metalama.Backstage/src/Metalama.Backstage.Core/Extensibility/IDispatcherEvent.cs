// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Extensibility;

/// <summary>
/// Marks the types that <see cref="IEventDispatcher"/> can publish. The name of an event type ends with
/// <c>Event</c>, for instance <c>TelemetryActivatedEvent</c>.
/// </summary>
public interface IDispatcherEvent;
