// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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