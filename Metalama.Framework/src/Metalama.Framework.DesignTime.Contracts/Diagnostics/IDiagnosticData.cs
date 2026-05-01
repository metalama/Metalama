// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Diagnostics;

[ComImport]
[Guid( "24F86F3F-E0A4-418B-A658-580DC7546409" )]
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