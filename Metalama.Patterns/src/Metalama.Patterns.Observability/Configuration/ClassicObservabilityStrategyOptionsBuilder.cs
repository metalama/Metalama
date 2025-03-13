// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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