// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Patterns.Observability.Configuration;

/// <summary>
/// Builder for configuring <see cref="ObservableAttribute"/> options at the member level (properties, fields, methods).
/// </summary>
/// <remarks>
/// <para>
/// Use this builder with the <see cref="ObservabilityExtensions.ConfigureObservability(Metalama.Framework.Fabrics.IQuery{IMember},System.Action{ObservabilityMemberOptionsBuilder})"/>
/// extension method to configure observability options for specific members from a fabric.
/// </para>
/// </remarks>
/// <seealso cref="ObservableAttribute"/>
/// <seealso cref="ObservabilityExtensions"/>
/// <seealso cref="ObservabilityTypeOptionsBuilder"/>
/// <seealso href="@observability"/>
[PublicAPI]
[CompileTime]
public sealed class ObservabilityMemberOptionsBuilder
{
    internal DependencyAnalysisOptions? DependencyAnalysisOptions { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether warnings about unobservable expressions in the target member should be suppressed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set to <c>true</c>, the <see cref="ObservableAttribute"/> aspect will not report warnings for code constructs
    /// in this member that it cannot analyze. This has the same effect as applying <see cref="SuppressObservabilityWarningsAttribute"/>
    /// to the member.
    /// </para>
    /// </remarks>
    public bool? IgnoreUnobservableExpressions
    {
        get => this.DependencyAnalysisOptions?.SuppressWarnings;
        set
            => this.DependencyAnalysisOptions =
                new DependencyAnalysisOptions { SuppressWarnings = value };
    }
}