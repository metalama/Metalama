// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Introspection.Navigation.Telemetry;

/// <summary>
/// Identifies the component that reports telemetry through <see cref="IntrospectionTelemetry"/>.
/// </summary>
/// <remarks>
/// The name of a member is sent in usage reports and is part of the kind of the usage session, so renaming a member
/// changes the reported data.
/// </remarks>
internal enum IntrospectionHost
{
    /// <summary>
    /// The Metalama.Mcp server.
    /// </summary>
    Mcp,

    /// <summary>
    /// The Metalama.LinqPad driver.
    /// </summary>
    LinqPad
}
