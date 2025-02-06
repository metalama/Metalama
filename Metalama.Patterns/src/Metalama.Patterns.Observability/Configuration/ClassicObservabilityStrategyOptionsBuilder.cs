// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Observability.Configuration;

/// <summary>
/// Builds options for the classic implementation strategy of the <see cref="ObservableAttribute"/> aspect.
/// To use, call <see cref="ObservabilityExtensions.ConfigureObservability(Metalama.Framework.Aspects.IAspectReceiver{Metalama.Framework.Code.ICompilation},System.Action{Metalama.Patterns.Observability.Configuration.ObservabilityTypeOptionsBuilder})"/>.
/// </summary>
[PublicAPI]
[CompileTime]
public sealed class ClassicObservabilityStrategyOptionsBuilder
{
    internal ClassicObservabilityStrategyOptions? Options { get; private set; }

    internal ClassicObservabilityStrategyOptionsBuilder( ClassicObservabilityStrategyOptions? options )
    {
        this.Options = options;
    }
}