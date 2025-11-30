// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Options;
using Metalama.Patterns.Observability.Configuration;

namespace Metalama.Patterns.Observability;

/// <summary>
/// Suppresses warnings reported by the <see cref="ObservableAttribute"/> aspect when it encounters unsupported
/// code constructs in the target member or type.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ObservableAttribute"/> aspect reports <c>LAMA51**</c> warnings when it encounters code patterns
/// that it cannot analyze, such as calls to non-constant methods. This attribute suppresses these warnings for
/// the target declaration and its children.
/// </para>
/// <para>
/// Alternatively, warnings can be suppressed using the standard <c>#pragma warning disable</c> directive.
/// </para>
/// <para>
/// <b>Warning:</b> Suppressing warnings does not change the generated code. Dependencies that triggered the warnings
/// will still not be tracked. Use this attribute only when you are certain that the untracked dependencies will not
/// affect observable state.
/// </para>
/// </remarks>
/// <seealso cref="ObservableAttribute"/>
/// <seealso cref="NotObservableAttribute"/>
/// <seealso cref="ConstantAttribute"/>
/// <seealso href="@observability"/>
[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Struct | AttributeTargets.Class )]
public sealed class SuppressObservabilityWarningsAttribute : Attribute, IHierarchicalOptionsProvider
{
    private readonly bool _suppressWarnings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuppressObservabilityWarningsAttribute"/> class.
    /// </summary>
    /// <param name="suppressWarnings">
    /// <c>true</c> to suppress observability warnings; <c>false</c> to enable warnings (useful to re-enable
    /// warnings in a child declaration when they were suppressed in an ancestor).
    /// </param>
    public SuppressObservabilityWarningsAttribute( bool suppressWarnings = true )
    {
        this._suppressWarnings = suppressWarnings;
    }

    IEnumerable<IHierarchicalOptions> IHierarchicalOptionsProvider.GetOptions( in OptionsProviderContext context )
        => new[] { new DependencyAnalysisOptions() { SuppressWarnings = this._suppressWarnings } };
}