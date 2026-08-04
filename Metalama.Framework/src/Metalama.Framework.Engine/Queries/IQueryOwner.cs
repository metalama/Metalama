// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Project;
using System;

namespace Metalama.Framework.Engine.Queries;

public interface IQueryOwner : IPipelineContributorCollector, IDiagnosticSource
{
    IProject Project { get; }

    string? Namespace { get; }

    ProjectServiceProvider ServiceProvider { get; }

    IAspectClassResolver AspectClasses { get; }

    UserCodeInvoker UserCodeInvoker { get; }

    AspectPredecessor AspectPredecessor { get; }

    Type Type { get; }

    /// <summary>
    /// Returns the <see cref="UserCodeExecutionContext"/> under which the queries of this owner are to be executed
    /// against a given compilation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The owner is asked to produce a context rather than to expose one it holds, because an owner may be durable
    /// while a context is bound to a compilation. A fabric amender is such an owner: it belongs to the
    /// <c>AspectPipelineConfiguration</c>, which is long-lived by design, being reused across keystrokes because
    /// rebuilding it per keystroke would be prohibitively slow. An amender that held a context would make that
    /// configuration pin one whole version of the project for the entire editing session, which is the defect reported
    /// by issue #1799.
    /// </para>
    /// <para>
    /// Returning a context per compilation, rather than storing one and rebinding it, is what makes that impossible by
    /// construction instead of by discipline. An owner whose own lifetime is that of a single run, such as an aspect
    /// builder, may still return a context it holds.
    /// </para>
    /// </remarks>
    UserCodeExecutionContext GetUserCodeExecutionContext( CompilationModel compilation, IDiagnosticAdder diagnostics );
}