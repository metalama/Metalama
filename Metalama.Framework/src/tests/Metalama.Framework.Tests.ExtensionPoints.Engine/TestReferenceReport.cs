// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Extensibility;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Tests.ExtensionPoints.Engine;

/// <summary>
/// The contributor that requests the references to the methods of a given name from the shared index of source references.
/// </summary>
internal sealed class TestReferenceReport : IExtensionPipelineContributor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestReferenceReport"/> class.
    /// </summary>
    public TestReferenceReport( string methodName, ImmutableArray<SyntaxNode> declarationRoots )
    {
        this.MethodName = methodName;
        this.DeclarationRoots = declarationRoots;
    }

    /// <summary>
    /// Gets the name of the methods whose references are reported.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets the syntax of the target of the adviser when the index must be restricted to it, or a default array.
    /// </summary>
    public ImmutableArray<SyntaxNode> DeclarationRoots { get; }

    /// <inheritdoc />
    public ContributorKind ContributorKind => TestContributorKinds.ReferenceReport;
}
