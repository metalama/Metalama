// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Step of the aspect linker pipeline.
/// </summary>
/// <typeparam name="TInput">Input of the step.</typeparam>
/// <typeparam name="TOutput">Output of the step.</typeparam>
internal abstract class AspectLinkerPipelineStep<TInput, TOutput>
{
    // ReSharper disable once UnusedMemberInSuper.Global
    public abstract Task<TOutput> ExecuteAsync( TInput input, CancellationToken cancellationToken );
}