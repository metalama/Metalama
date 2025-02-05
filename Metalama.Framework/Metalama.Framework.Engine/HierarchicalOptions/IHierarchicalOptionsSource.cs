// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Extensibility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.HierarchicalOptions;

internal interface IHierarchicalOptionsSource : IPipelineContributor
{
    Task CollectOptionsAsync(
        CompilationModel compilation,
        Action<HierarchicalOptionsInstance> addOptions,
        IUserDiagnosticSink diagnosticSink,
        CancellationToken cancelationToken );
}