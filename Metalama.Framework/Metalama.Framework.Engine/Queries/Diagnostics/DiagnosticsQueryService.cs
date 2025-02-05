// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Fabrics;
using System;

namespace Metalama.Framework.Engine.Queries.Diagnostics;

internal sealed class DiagnosticsQueryService : IDiagnosticsQueryService
{
    public void ReportDiagnostic<TDeclaration>( IQuery<TDeclaration> query, Func<TDeclaration, IDiagnostic> getDiagnostic )
        where TDeclaration : class, IDeclaration
    {
        var queryImpl = (IQueryImpl<TDeclaration>) query;
        queryImpl.OnChildAdded();

        var source = new DiagnosticQuerySource<TDeclaration>(
            queryImpl,
            ( sink, source, declaration, _ )
                => sink.Report( getDiagnostic( (TDeclaration) declaration ), declaration, source ) );

        queryImpl.Owner.AddContributor( source );
    }

    public void ReportDiagnostic<TDeclaration, TTag>( ITaggedQuery<TDeclaration, TTag> query, Func<TDeclaration, TTag, IDiagnostic> getDiagnostic )
        where TDeclaration : class, IDeclaration
    {
        var queryImpl = (IQueryImpl<TDeclaration>) query;
        queryImpl.OnChildAdded();

        var source = new DiagnosticQuerySource<TDeclaration>(
            queryImpl,
            ( sink, source, declaration, tag )
                => sink.Report( getDiagnostic( (TDeclaration) declaration, (TTag) tag! ), declaration, source ) );

        queryImpl.Owner.AddContributor( source );
    }

    public void SuppressDiagnostic<TDeclaration>( IQuery<TDeclaration> query, Func<TDeclaration, SuppressionDefinition> getSuppression )
        where TDeclaration : class, IDeclaration
    {
        var queryImpl = (IQueryImpl<TDeclaration>) query;
        queryImpl.OnChildAdded();

        var source = new DiagnosticQuerySource<TDeclaration>(
            queryImpl,
            ( sink, source, declaration, _ )
                => sink.Suppress( getSuppression( (TDeclaration) declaration ), declaration, source ) );

        queryImpl.Owner.AddContributor( source );
    }

    public void SuppressDiagnostic<TDeclaration, TTag>( ITaggedQuery<TDeclaration, TTag> query, Func<TDeclaration, TTag, SuppressionDefinition> getSuppression )
        where TDeclaration : class, IDeclaration
    {
        var queryImpl = (IQueryImpl<TDeclaration>) query;
        queryImpl.OnChildAdded();

        var source = new DiagnosticQuerySource<TDeclaration>(
            queryImpl,
            ( sink, source, declaration, tag )
                => sink.Suppress( getSuppression( (TDeclaration) declaration, (TTag) tag! ), declaration, source ) );

        queryImpl.Owner.AddContributor( source );
    }
}