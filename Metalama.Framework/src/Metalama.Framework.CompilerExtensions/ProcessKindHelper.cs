// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Diagnostics;

namespace Metalama.Framework.CompilerExtensions;

/// <summary>
/// Exposes the kind of the current process to the design-time entry points of this assembly.
/// </summary>
public static class ProcessKindHelper
{
    /// <summary>
    /// Gets the kind of the current process. The value is computed once, in this property initializer, and is then
    /// cached for the lifetime of the process. The table itself is in <see cref="ProcessKindDetector"/>, which
    /// <c>Metalama.Backstage</c> also compiles.
    /// </summary>
    public static ProcessKind CurrentProcessKind { get; } =
        ProcessKindDetector.GetProcessKind( Process.GetCurrentProcess().ProcessName, Environment.CommandLine );
}
