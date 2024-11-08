// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Introspection;

namespace Metalama.Framework.Workspaces;

internal sealed class UserDiagnostic( Severity severity, string id, string message, string? filePath, int? line, IDeclaration? declaration, object? details )
    : IIntrospectionDiagnostic
{
    public ICompilation? Compilation => this.Declaration?.Compilation;

    public string Id { get; } = id;

    public string Message { get; } = message;

    public string? FilePath { get; } = filePath;

    public int? Line { get; } = line;

    public IDeclaration? Declaration { get; } = declaration;

    public Severity Severity { get; } = severity;

    public IntrospectionDiagnosticSource Source => IntrospectionDiagnosticSource.User;

    public object? Details { get; } = details;
}