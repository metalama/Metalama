// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility;

namespace Metalama.Framework.Tests.ExtensionPoints.Engine;

/// <summary>
/// The contributor that a verb of the proof of concept adds to the pipeline.
/// </summary>
internal sealed class TestRegistration : IExtensionPipelineContributor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestRegistration"/> class.
    /// </summary>
    public TestRegistration( string tag, ExtensionContributionOrigin origin, IRef<IDeclaration>? scope, string channel, bool? templateProviderMatches )
    {
        this.Tag = tag;
        this.Origin = origin;
        this.Scope = scope;
        this.Channel = channel;
        this.TemplateProviderMatches = templateProviderMatches;
    }

    /// <summary>
    /// Gets the tag given by the user code.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the origin captured when the verb was called.
    /// </summary>
    public ExtensionContributionOrigin Origin { get; }

    /// <summary>
    /// Gets the target of the adviser, or <c>null</c> for a registration made through a query.
    /// </summary>
    public IRef<IDeclaration>? Scope { get; }

    /// <summary>
    /// Gets the surface through which the registration was made: <c>adviser</c> or <c>query</c>.
    /// </summary>
    public string Channel { get; }

    /// <summary>
    /// Gets a value indicating whether the template provider of the adviser is the expected one, or <c>null</c> when no provider was expected.
    /// </summary>
    public bool? TemplateProviderMatches { get; }

    /// <inheritdoc />
    public ContributorKind ContributorKind => TestContributorKinds.Registration;
}
