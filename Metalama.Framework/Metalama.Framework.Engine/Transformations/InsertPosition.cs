// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Transformations;

internal readonly struct InsertPosition : IEquatable<InsertPosition>
{
    private readonly SyntaxTree? _syntaxTree;

    /// <summary>
    /// Gets the relation of the insert position to the specified node.
    /// </summary>
    public InsertPositionRelation Relation { get; }

    /// <summary>
    /// Gets the node near to which/into which new nodes should be inserted.
    /// </summary>
    public MemberDeclarationSyntax? SyntaxNode { get; }

    /// <summary>
    /// Gets the builder into which the new node should be inserted.
    /// </summary>
    public NamedDeclarationBuilderData? BuilderData { get; }

    /// <summary>
    /// Gets the target syntax tree of the insertion.
    /// </summary>
    public SyntaxTree SyntaxTree => this._syntaxTree.AssertNotNull();

    public InsertPosition( InsertPositionRelation relation, MemberDeclarationSyntax? node )
    {
        this.Relation = relation;
        this.SyntaxNode = node;
        this._syntaxTree = node?.SyntaxTree;
    }

    public InsertPosition( InsertPositionRelation relation, NamedDeclarationBuilderData builderData )
    {
        this.Relation = relation;
        this.BuilderData = builderData;
        this._syntaxTree = builderData.PrimarySyntaxTree.AssertNotNull();
    }

    public InsertPosition( SyntaxTree introducedSyntaxTree )
    {
        this.Relation = InsertPositionRelation.Root;
        this._syntaxTree = introducedSyntaxTree;
    }

    public override bool Equals( object? obj ) => obj is InsertPosition position && this.Equals( position );

    public bool Equals( InsertPosition other )
        => this.Relation == other.Relation
           && this.SyntaxNode == other.SyntaxNode
           && ReferenceEquals( this.BuilderData, other.BuilderData )
           && this.SyntaxTree == other.SyntaxTree;

    public override int GetHashCode() => HashCode.Combine( this.Relation, this.SyntaxNode, this.BuilderData, this.SyntaxTree );

    public override string ToString()
        => this.SyntaxNode != null
            ? $"{this.Relation} {this.SyntaxNode.Kind()} in {this.SyntaxNode.SyntaxTree.FilePath}"
            : this.BuilderData switch
            {
                NamedTypeBuilderData namedTypeBuilder => $"{this.Relation} {namedTypeBuilder.AssertNotNull().Name} (built type)",
                NamespaceBuilderData namespaceBuilder => $"{this.Relation} {namespaceBuilder.AssertNotNull().Name} (built namespace)",
                _ => throw new AssertionFailedException( $"Unexpected: {this.BuilderData}" )
            };
}