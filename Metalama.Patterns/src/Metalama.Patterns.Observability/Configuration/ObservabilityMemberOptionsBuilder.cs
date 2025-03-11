// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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