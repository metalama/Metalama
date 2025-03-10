// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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