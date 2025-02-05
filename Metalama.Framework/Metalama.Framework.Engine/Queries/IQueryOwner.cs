// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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