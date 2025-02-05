// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Services;
using System;

namespace Metalama.Framework.Diagnostics;

internal interface IDiagnosticsQueryService : IProjectService
{
    void ReportDiagnostic<TDeclaration>( IQuery<TDeclaration> query, Func<TDeclaration, IDiagnostic> getDiagnostic )
        where TDeclaration : class, IDeclaration;

    void ReportDiagnostic<TDeclaration, TTag>( ITaggedQuery<TDeclaration, TTag> query, Func<TDeclaration, TTag, IDiagnostic> getDiagnostic )
        where TDeclaration : class, IDeclaration;

    void SuppressDiagnostic<TDeclaration>( IQuery<TDeclaration> query, Func<TDeclaration, SuppressionDefinition> getSuppression )
        where TDeclaration : class, IDeclaration;

    void SuppressDiagnostic<TDeclaration, TTag>( ITaggedQuery<TDeclaration, TTag> query, Func<TDeclaration, TTag, SuppressionDefinition> getSuppression )
        where TDeclaration : class, IDeclaration;
}