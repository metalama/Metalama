// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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