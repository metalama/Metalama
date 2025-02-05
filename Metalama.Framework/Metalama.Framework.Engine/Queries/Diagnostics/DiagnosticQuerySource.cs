// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Queries.Diagnostics;

internal sealed class DiagnosticQuerySource<TDeclaration> : IDiagnosticQuerySource
    where TDeclaration : class, IDeclaration
{
    private readonly IQueryImpl<TDeclaration> _query;
    
    private readonly Action<UserDiagnosticSink, IDiagnosticSource, IDeclaration, object?> _action;

    public DiagnosticQuerySource(
        IQueryImpl<TDeclaration> query,
        Action<UserDiagnosticSink, IDiagnosticSource, IDeclaration, object?> action )
    {
        this._query = query;
        this._action = action;
    }

    public Task CollectDiagnosticsAsync( CompilationModel compilation, UserDiagnosticSink diagnosticSink, CancellationToken cancellationToken )
    {
        return this._query.InvokeAsync(
            compilation,
            diagnosticSink,
            GeneralDiagnosticDescriptors.CanReportDiagnosticOnlyUnderParent,
            ( item, tag, context ) =>
            {
                context.UserCodeInvoker.Invoke( () => this._action( diagnosticSink, this._query.Owner, item, tag ), context.UserCodeExecutionContext );

                return Task.CompletedTask;
            },
            cancellationToken );
    }
}