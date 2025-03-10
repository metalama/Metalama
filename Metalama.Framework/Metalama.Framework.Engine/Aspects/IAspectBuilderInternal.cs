// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;

namespace Metalama.Framework.Engine.Aspects
{
    /// <summary>
    /// Defines the internal semantics of classes implementing <see cref="IAspectBuilder"/>. This interface
    /// exists because the only implementation <see cref="AspectBuilder{T}"/> is generic, and some parts of the
    /// code need a common, non-generic interface.
    /// </summary>
    internal interface IAspectBuilderInternal : IAspectBuilder, IPipelineContributorCollector, IAdviserInternal
    {
        ProjectServiceProvider ServiceProvider { get; }

        DisposeAction WithPredecessor( in AspectPredecessor predecessor );

        IDiagnosticAdder DiagnosticAdder { get; }
    }
}