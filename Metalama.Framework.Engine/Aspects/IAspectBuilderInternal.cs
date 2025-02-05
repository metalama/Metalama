// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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