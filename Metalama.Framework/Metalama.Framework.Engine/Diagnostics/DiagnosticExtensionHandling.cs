// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Diagnostics;

public enum DiagnosticExtensionHandling
{
    /// <summary>
    /// The extension can be fully ignored.
    /// </summary>
    None,

    /// <summary>
    /// Diagnostic properties must be added and the extension object kept.
    /// </summary>
    Process,

    /// <summary>
    /// Only diagnostic properties should be kept. The extension object can be dropped.
    /// </summary>
    PropertiesOnly
}