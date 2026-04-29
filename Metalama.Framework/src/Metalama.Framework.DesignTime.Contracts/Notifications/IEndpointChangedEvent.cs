// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Notifications;

/// <summary>
/// Event raised when an endpoint configuration changes for a project. Subscribers may need to
/// refresh their notion of which Metalama processes serve a given project.
/// </summary>
/// <remarks>
/// Cross-version contract. <see cref="GuidAttribute"/>, type name, and member signatures are frozen forever.
/// </remarks>
[ComImport]
[Guid( "FB32D6B7-5D68-479D-966A-34070B38B1B6" )]
public interface IEndpointChangedEvent : IDesignTimeNotificationEvent
{
    /// <summary>
    /// Gets the GUID of the project whose endpoint configuration changed.
    /// </summary>
    Guid ProjectGuid { get; }
}
