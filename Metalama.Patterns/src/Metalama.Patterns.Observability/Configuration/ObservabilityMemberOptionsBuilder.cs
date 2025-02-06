// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Patterns.Observability.Configuration;

/// <summary>
/// Builds dependency options. Used at the level of <see cref="IMember"/>.
/// </summary>
[PublicAPI]
[CompileTime]
public sealed class ObservabilityMemberOptionsBuilder
{
    internal DependencyAnalysisOptions? DependencyAnalysisOptions { get; private set; }

    public bool? IgnoreUnobservableExpressions
    {
        get => this.DependencyAnalysisOptions?.SuppressWarnings;
        set
            => this.DependencyAnalysisOptions =
                new DependencyAnalysisOptions { SuppressWarnings = value };
    }
}