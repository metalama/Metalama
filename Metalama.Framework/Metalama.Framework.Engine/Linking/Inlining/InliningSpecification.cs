// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Metalama.Framework.Engine.Linking.Inlining;

internal sealed class InliningSpecification
{
    private readonly InliningId _inliningId;
    private readonly InliningId? _parentInliningId;

    /// <summary>
    /// Gets the semantic which is a the destination of inlining (the top-level semantic which will contain the body).
    /// This allows us to discern between inlining of one aspect reference into two separate bodies (used for deconstructed auto properties).
    /// </summary>
    public IntermediateSymbolSemantic<IMethodSymbol> DestinationSemantic { get; }

    /// <summary>
    /// Gets the identifier of inlining within the destination semantic.
    /// This allows us to inline one aspect reference twice into one target body (used for deconstructed auto properties).
    /// </summary>
    public InliningContextIdentifier ContextIdentifier => new( this.DestinationSemantic, this._inliningId );

    /// <summary>
    /// Gets the identifier of inlining within the destination semantic.
    /// This allows us to inline one aspect reference twice into one target body (used for deconstructed auto properties).
    /// </summary>
    public InliningContextIdentifier ParentContextIdentifier => new( this.DestinationSemantic, this._parentInliningId );

    /// <summary>
    /// Gets the aspect reference to be inlined.
    /// </summary>
    public ResolvedAspectReference AspectReference { get; }

    /// <summary>
    /// Gets the inliner that created this specification.
    /// </summary>
    public Inliner Inliner { get; }

    /// <summary>
    /// Gets the statement replaced by inlining.
    /// </summary>
    public SyntaxNode ReplacedNode { get; }

    /// <summary>
    /// Gets a value indicating whether simple inlining (no return transformation) may be used.
    /// </summary>
    public bool UseSimpleInlining { get; }

    /// <summary>
    /// Gets a value indicating whether this inlining requires the return variable to be declared.
    /// </summary>
    public bool DeclareReturnVariable { get; }

    /// <summary>
    /// Gets the return variable identifier.
    /// </summary>
    public string? ReturnVariableIdentifier { get; }

    /// <summary>
    /// Gets the return label identifier.
    /// </summary>
    public string? ReturnLabelIdentifier { get; }

    /// <summary>
    /// Gets a symbol from intermediate compilation that will be inlined.
    /// </summary>
    public IntermediateSymbolSemantic<IMethodSymbol> TargetSemantic { get; }

    public InliningSpecification(
        IntermediateSymbolSemantic<IMethodSymbol> destinationSemantic,
        InliningId inliningId,
        InliningId? parentInliningId,
        ResolvedAspectReference aspectReference,
        Inliner inliner,
        SyntaxNode replacedNode,
        bool useSimpleInlining,
        bool declareReturnVariable,
        string? returnVariableIdentifier,
        string? returnLabelIdentifier,
        IntermediateSymbolSemantic<IMethodSymbol> targetSemantic )
    {
        Invariant.AssertNot( declareReturnVariable && returnVariableIdentifier == null );
        Invariant.AssertNot( targetSemantic.Kind == IntermediateSymbolSemanticKind.Final );
        Invariant.Assert( SymbolEqualityComparer.Default.Equals( targetSemantic.Symbol.ContainingType, destinationSemantic.Symbol.ContainingType ) );

        this.DestinationSemantic = destinationSemantic;
        this._inliningId = inliningId;
        this._parentInliningId = parentInliningId;
        this.AspectReference = aspectReference;
        this.Inliner = inliner;
        this.UseSimpleInlining = useSimpleInlining;
        this.ReplacedNode = replacedNode;
        this.DeclareReturnVariable = declareReturnVariable;
        this.ReturnVariableIdentifier = returnVariableIdentifier;
        this.ReturnLabelIdentifier = returnLabelIdentifier;
        this.TargetSemantic = targetSemantic;
    }

    public override string ToString()
        => $"Inline {this.ReplacedNode.Kind()} {(this.AspectReference.HasResolvedSemanticBody ? this.AspectReference.ResolvedSemanticBody : this.AspectReference.ResolvedSemantic)} "
           +
           $"into {this.DestinationSemantic} (id: {this._inliningId})";
}