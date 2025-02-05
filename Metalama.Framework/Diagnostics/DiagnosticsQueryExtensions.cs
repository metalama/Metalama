// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using System;

namespace Metalama.Framework.Diagnostics;

[CompileTime]
public static class DiagnosticsQueryExtensions
{
    /// <summary>
    /// Reports a diagnostic for each declaration selected by the the current object.
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param> 
    /// <param name="diagnostic">A function returning an <see cref="IDiagnostic"/> given a declaration.</param>
    public static void ReportDiagnostic<TDeclaration>( this IQuery<TDeclaration> query, Func<TDeclaration, IDiagnostic> diagnostic )
        where TDeclaration : class, IDeclaration
        => query.Project.ServiceProvider.GetRequiredService<IDiagnosticsQueryService>().ReportDiagnostic( query, diagnostic );

    /// <summary>
    /// Suppresses a diagnostic for each declaration selected by the current object.
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param> 
    /// <param name="suppression">A function returning a <see cref="SuppressionDefinition"/> given a declaration.</param>
    public static void SuppressDiagnostic<TDeclaration>( this IQuery<TDeclaration> query, Func<TDeclaration, SuppressionDefinition> suppression )
        where TDeclaration : class, IDeclaration
        => query.Project.ServiceProvider.GetRequiredService<IDiagnosticsQueryService>().SuppressDiagnostic( query, suppression );

    /// <summary>
    /// Reports a diagnostic for each declaration selected by the the current object.
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param> 
    /// <param name="diagnostic">A function returning an <see cref="IDiagnostic"/> given a declaration.</param>
    public static void ReportDiagnostic<TDeclaration, TTag>( this ITaggedQuery<TDeclaration, TTag> query, Func<TDeclaration, TTag, IDiagnostic> diagnostic )
        where TDeclaration : class, IDeclaration
        => query.Project.ServiceProvider.GetRequiredService<IDiagnosticsQueryService>().ReportDiagnostic( query, diagnostic );

    /// <summary>
    /// Suppresses a diagnostic for each declaration selected by the current object.
    /// </summary>
    /// <param name="query">A query selecting the declarations to validate.</param> 
    /// <param name="suppression">A function returning a <see cref="SuppressionDefinition"/> given a declaration.</param>
    public static void SuppressDiagnostic<TDeclaration, TTag>(
        this ITaggedQuery<TDeclaration, TTag> query,
        Func<TDeclaration, TTag, SuppressionDefinition> suppression )
        where TDeclaration : class, IDeclaration
        => query.Project.ServiceProvider.GetRequiredService<IDiagnosticsQueryService>().SuppressDiagnostic( query, suppression );
}