// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Queries.Options;

internal sealed class OptionsQuerySource<TDeclaration, TOptions> : IHierarchicalOptionsSource
    where TDeclaration : class, IDeclaration
    where TOptions : class, IHierarchicalOptions, IHierarchicalOptions<TDeclaration>, new()
{
    private readonly IQueryImpl<TDeclaration> _query;
    private readonly Func<TDeclaration, object?, TOptions> _func;

    public OptionsQuerySource( IQueryImpl<TDeclaration> query, Func<TDeclaration, object?, TOptions> func )
    {
        this._query = query;
        this._func = func;
    }

    public Task CollectOptionsAsync(
        CompilationModel compilation,
        Action<HierarchicalOptionsInstance> addOptions,
        IUserDiagnosticSink diagnosticSink,
        CancellationToken cancelationToken )
    {
        return this._query.InvokeAsync(
            compilation,
            diagnosticSink,
            GeneralDiagnosticDescriptors.CanSetOptionsOnlyUnderParent,
            ( declaration, tag, context ) =>
            {
                if ( context.UserCodeInvoker.TryInvoke(
                        () => this._func( declaration, tag ),
                        context.UserCodeExecutionContext.AssertNotNull(),
                        out var options ) )
                {
                    addOptions( new HierarchicalOptionsInstance( declaration, options ) );
                }

                return Task.CompletedTask;
            },
            cancelationToken );
    }
}