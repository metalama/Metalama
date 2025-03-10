// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;
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

    UserCodeExecutionContext UserCodeExecutionContext { get; }
}