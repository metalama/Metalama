// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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