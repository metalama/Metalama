// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Project;

namespace Metalama.Patterns.Observability.Implementation.ClassicStrategy;

[CompileTime]
internal sealed class ClassicObservabilityStrategy : IObservabilityStrategy
{
    private ClassicObservabilityStrategy() { }

    public static ClassicObservabilityStrategy Instance { get; } = new();

    public void BuildAspect( IAspectBuilder<INamedType> aspectBuilder )
    {
        var executionScenario = MetalamaExecutionContext.Current.ExecutionScenario;

        var implementation = executionScenario.CapturesNonObservableTransformations
            ? (IObservabilityStrategy) new ClassicObservabilityStrategyImpl( aspectBuilder )
            : new ClassicDesignTimeObservabilityStrategyImpl( aspectBuilder );

        implementation.BuildAspect( aspectBuilder );
    }
}