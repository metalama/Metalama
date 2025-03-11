// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Contracts.Diagnostics;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System.Globalization;

namespace Metalama.Framework.DesignTime.Diagnostics;

[JsonObject]
public class DiagnosticData : IDiagnosticData
{
    public DiagnosticData( Diagnostic diagnostic )
    {
        var lineSpan = diagnostic.Location.GetLineSpan();

        this.Severity = diagnostic.Severity;
        this.FilePath = lineSpan.Path;
        this.Message = diagnostic.GetMessage( CultureInfo.CurrentCulture );
        this.StartLine = lineSpan.StartLinePosition.Line;
        this.StartColumn = lineSpan.StartLinePosition.Character;
        this.EndLine = lineSpan.EndLinePosition.Line;
        this.EndColumn = lineSpan.EndLinePosition.Character;
    }

    [JsonConstructor]
    public DiagnosticData( DiagnosticSeverity severity, string filePath, string message, int startLine, int startColumn, int endLine, int endColumn )
    {
        this.Severity = severity;
        this.FilePath = filePath;
        this.Message = message;
        this.StartLine = startLine;
        this.StartColumn = startColumn;
        this.EndLine = endLine;
        this.EndColumn = endColumn;
    }

    public DiagnosticSeverity Severity { get; }

    public string? FilePath { get; }

    public string Message { get; }

    public int StartLine { get; set; }

    public int StartColumn { get; set; }

    public int EndLine { get; set; }

    public int EndColumn { get; set; }
}