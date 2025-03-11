// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Fabrics;
using Metalama.Framework.Fabrics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Queries;

/// <summary>
/// An interface of <see cref="Query{TDeclaration,TTag}"/> that does not depend on the type of the tag.
/// </summary>
/// <typeparam name="TDeclaration"></typeparam>
public interface IQueryImpl<out TDeclaration> : IQuery<TDeclaration>
    where TDeclaration : class, IDeclaration
{
    Task InvokeAsync(
        CompilationModel compilation,
        IDiagnosticAdder diagnosticAdder,
        DiagnosticDefinition<(FormattableString Predecessor, IDeclaration Child, IDeclaration Parent)> diagnosticDefinition,
        Func<TDeclaration, object?, QueryExecutionContext, Task> invokeAction, // Must be captured by the caller.
        CancellationToken cancellationToken );

    IQueryOwner Owner { get; }

    /// <summary>
    /// Notifies that a child has been added. This allows the query to decide whether caching should be used.
    /// </summary>
    void OnChildAdded();
}