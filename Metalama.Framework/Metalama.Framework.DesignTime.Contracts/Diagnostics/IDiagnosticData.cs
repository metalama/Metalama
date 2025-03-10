// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Diagnostics;

[ComImport]
[Guid( "2D5AD05C-ED86-45CC-A9F2-5F6E8186AF7C" )]
public interface IDiagnosticData
{
    DiagnosticSeverity Severity { get; }

    string? FilePath { get; }

    string Message { get; }

    int StartLine { get; }

    int StartColumn { get; }

    int EndLine { get; }

    int EndColumn { get; }
}