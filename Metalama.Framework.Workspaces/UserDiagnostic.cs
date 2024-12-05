// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Introspection;

namespace Metalama.Framework.Workspaces;

internal sealed class UserDiagnostic : IIntrospectionDiagnostic
{
    public UserDiagnostic( Severity severity, string id, string message, string? filePath, int? line, IDeclaration? declaration, object? details )
    {
        this.Id = id;
        this.Message = message;
        this.FilePath = filePath;
        this.Line = line;
        this.Declaration = declaration;
        this.Severity = severity;
        this.Details = details;
    }

    public ICompilation? Compilation => this.Declaration?.Compilation;

    public string Id { get; }

    public string Message { get; }

    public string? FilePath { get; }

    public int? Line { get; }

    public IDeclaration? Declaration { get; }

    public Severity Severity { get; }

    public IntrospectionDiagnosticSource Source => IntrospectionDiagnosticSource.User;

    public object? Details { get; }
}