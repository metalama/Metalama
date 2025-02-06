// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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