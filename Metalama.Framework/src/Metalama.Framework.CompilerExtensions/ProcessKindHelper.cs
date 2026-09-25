// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using SharpCrafters.Backstage.ProcessClassification;

namespace Metalama.Framework.CompilerExtensions;

/// <summary>
/// Exposes the kind of the current process to the design-time entry points of this assembly.
/// </summary>
/// <remarks>
/// <para>
/// The classification is in <see cref="ProcessKindDetector"/>, which comes from the
/// <c>SharpCrafters.Backstage.ProcessClassification</c> package. That package is merged into this assembly, and its
/// types are internal here.
/// </para>
/// <para>
/// This file is also compiled into <c>Metalama.Framework.EditorExtensions</c>, which merges the same package. The class
/// is internal, because a public member returning <see cref="ProcessKind"/> could not be called from another assembly:
/// each assembly declares its own copy of the enumeration.
/// </para>
/// </remarks>
internal static class ProcessKindHelper
{
    /// <summary>
    /// Gets the kind of the current process. The value is computed once, in this property initializer, and is then
    /// cached for the lifetime of the process.
    /// </summary>
    public static ProcessKind CurrentProcessKind { get; } = ProcessKindDetector.GetCurrentProcessKind();
}
