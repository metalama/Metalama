// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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