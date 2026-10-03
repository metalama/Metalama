// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Queries;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Tests.ExtensionPoints.Engine;

/// <summary>
/// The contributor that requests the redirection of the source calls to the methods of a given name.
/// </summary>
internal sealed class TestRedirection : IExtensionPipelineContributor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestRedirection"/> class.
    /// </summary>
    public TestRedirection(
        string methodName,
        ExtensionContributionOrigin origin,
        TestRedirectionOptions options,
        IRef<IMethod>? replacement,
        (string TypeName, string MethodName)? replacementName,
        IRef<IDeclaration>? scope,
        IQueryImpl<IDeclaration>? scopeQuery,
        ImmutableArray<SyntaxNode>? declarationRoots )
    {
        this.MethodName = methodName;
        this.Origin = origin;
        this.Options = options;
        this.Replacement = replacement;
        this.ReplacementName = replacementName;
        this.Scope = scope;
        this.ScopeQuery = scopeQuery;
        this.DeclarationRoots = declarationRoots;
    }

    /// <summary>
    /// Gets the name of the source methods.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets the origin captured when the verb was called.
    /// </summary>
    public ExtensionContributionOrigin Origin { get; }

    /// <summary>
    /// Gets the options of the redirection.
    /// </summary>
    public TestRedirectionOptions Options { get; }

    /// <summary>
    /// Gets the replacement method given by an adviser, or <c>null</c>.
    /// </summary>
    public IRef<IMethod>? Replacement { get; }

    /// <summary>
    /// Gets the names of the replacement method given by a query, or <c>null</c>.
    /// </summary>
    public (string TypeName, string MethodName)? ReplacementName { get; }

    /// <summary>
    /// Gets the target of the adviser, or <c>null</c> for a redirection requested through a query.
    /// </summary>
    public IRef<IDeclaration>? Scope { get; }

    /// <summary>
    /// Gets the query whose declarations are the scope of the redirection, or <c>null</c> for a redirection requested through an adviser.
    /// </summary>
    public IQueryImpl<IDeclaration>? ScopeQuery { get; }

    /// <summary>
    /// Gets the syntax of the target of the adviser, which restricts the shared index, or <c>null</c> for a query.
    /// </summary>
    public ImmutableArray<SyntaxNode>? DeclarationRoots { get; }

    /// <inheritdoc />
    public ContributorKind ContributorKind => TestContributorKinds.Redirection;
}
