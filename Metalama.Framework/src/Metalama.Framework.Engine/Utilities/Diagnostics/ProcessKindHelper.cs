// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Backstage.ProcessClassification;

namespace Metalama.Framework.Engine.Utilities.Diagnostics;

/// <summary>
/// Exposes the kind of the current process to the code that runs before a service provider exists.
/// </summary>
/// <remarks>
/// The kind is classified by <see cref="ProcessKindDetector"/> from the name and the command line of the process. When
/// a service provider exists, prefer <c>IApplicationInfoProvider.ProcessKind</c>, which also takes into account the
/// kind that the application declares.
/// </remarks>
public static class ProcessKindHelper
{
    /// <summary>
    /// Gets the kind of the current process. The value is computed once, in this property initializer, and is then
    /// cached for the lifetime of the process.
    /// </summary>
    public static ProcessKind CurrentProcessKind { get; } = ProcessKindDetector.GetCurrentProcessKind();
}
