// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Utilities.Threading;
using Metalama.Framework.Services;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Pipeline.DesignTime;

public interface IAspectPipelineConfigurationProvider : IGlobalService
{
    ValueTask<FallibleResultWithDiagnostics<AspectPipelineConfiguration>> GetConfigurationAsync(
        PartialCompilation compilation,
        AsyncExecutionContext executionContext,
        TestableCancellationToken cancellationToken );
}