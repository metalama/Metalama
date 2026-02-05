// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking;

internal sealed class ResolvedAspectReference
{
    private readonly IntermediateSymbolSemantic<IMethodSymbol>? _explicitResolvedSemanticBody;

    /// <summary>
    /// Gets the semantic that contains the reference.
    /// </summary>
    public IntermediateSymbolSemantic<IMethodSymbol> ContainingSemantic { get; }

    /// <summary>
    /// Gets the local function that contains this reference or <c>null</c> if it contained within a normal method.
    /// </summary>
    private IMethodSymbol? ContainingLocalFunction { get; }

    /// <summary>
    /// Gets the body that contains the reference, i.e. local function or containing semantic.
    /// </summary>
    public IMethodSymbol ContainingBody => this.ContainingLocalFunction ?? this.ContainingSemantic.Symbol;

    /// <summary>
    /// Gets the symbol the reference was originally pointing to.
    /// </summary>
    public ISymbol OriginalSymbol { get; }

    /// <summary>
    /// Gets the symbol semantic that is the target of the reference (C# declaration, i.e. method, property or event).
    /// </summary>
    public IntermediateSymbolSemantic ResolvedSemantic { get; }

    /// <summary>
    /// Gets the symbol semantic for the target body (always a method).
    /// </summary>
    public IntermediateSymbolSemantic<IMethodSymbol> ResolvedSemanticBody
        => this._explicitResolvedSemanticBody ?? (this.ResolvedSemantic.Symbol.Kind, this.TargetKind) switch
        {
            (SymbolKind.Method, AspectReferenceTargetKind.Self)
                when this.ResolvedSemantic.Symbol is IMethodSymbol method =>
                method.ToSemantic( this.ResolvedSemantic.Kind ),
            (SymbolKind.Property, AspectReferenceTargetKind.PropertyGetAccessor)
                when this.ResolvedSemantic.Symbol is IPropertySymbol property =>
                property.GetMethod.AssertNotNull().ToSemantic( this.ResolvedSemantic.Kind ),
            (SymbolKind.Property, AspectReferenceTargetKind.PropertySetAccessor)
                when this.ResolvedSemantic.Symbol is IPropertySymbol { SetMethod: null } property =>
                property.GetMethod.AssertNotNull().ToSemantic( this.ResolvedSemantic.Kind ),
            (SymbolKind.Property, AspectReferenceTargetKind.PropertySetAccessor)
                when this.ResolvedSemantic.Symbol is IPropertySymbol property =>
                property.SetMethod.AssertNotNull().ToSemantic( this.ResolvedSemantic.Kind ),
            (SymbolKind.Event, AspectReferenceTargetKind.EventAddAccessor)
                when this.ResolvedSemantic.Symbol is IEventSymbol @event =>
                @event.AddMethod.AssertNotNull().ToSemantic( this.ResolvedSemantic.Kind ),
            (SymbolKind.Event, AspectReferenceTargetKind.EventRemoveAccessor)
                when this.ResolvedSemantic.Symbol is IEventSymbol @event =>
                @event.RemoveMethod.AssertNotNull().ToSemantic( this.ResolvedSemantic.Kind ),
            _ => throw new AssertionFailedException( $"{this} does not point to a semantic with a body." )
        };

    public bool HasResolvedSemanticBody
        => this._explicitResolvedSemanticBody != null ||
           (this.ResolvedSemantic.Symbol.Kind, this.TargetKind) switch
           {
               (SymbolKind.Method, AspectReferenceTargetKind.Self) => true,
               (SymbolKind.Property, AspectReferenceTargetKind.PropertyGetAccessor) => true,
               (SymbolKind.Property, AspectReferenceTargetKind.PropertySetAccessor) => true,
               (SymbolKind.Event, AspectReferenceTargetKind.EventAddAccessor) => true,
               (SymbolKind.Event, AspectReferenceTargetKind.EventRemoveAccessor) => true,
               (SymbolKind.Event, AspectReferenceTargetKind.EventRaiseAccessor) => false,
               (SymbolKind.Field, AspectReferenceTargetKind.PropertyGetAccessor) => false,
               (SymbolKind.Field, AspectReferenceTargetKind.PropertySetAccessor) => false,
               _ => throw new AssertionFailedException( $"{this} is not expected." )
           };

#if DEBUG

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    // ReSharper disable once UnusedAutoPropertyAccessor.Local

    /// <summary>
    /// Gets the annotated node. This is the node that originally had the annotation.
    /// </summary>
#pragma warning disable IDE0052
    private SyntaxNode AnnotatedNode { get; }
#pragma warning restore IDE0052

#endif

    /// <summary>
    /// Gets the root node. This is the node that needs to be replaced by the linker.
    /// </summary>
    public SyntaxNode RootNode { get; }

    /// <summary>
    /// Gets the annotated expression. This is for convenience in inliners which always work with expressions.
    /// </summary>
    public ExpressionSyntax RootExpression
        => this.RootNode as ExpressionSyntax ?? throw new AssertionFailedException( $"Root node not an expression {this.RootNode} is not an expression." );

    /// <summary>
    /// Gets the symbol source node. This node is the source of the symbol that is referenced.
    /// </summary>
    public SyntaxNode SymbolSourceNode { get; }

    /// <summary>
    /// Gets the target kind of the aspect reference.
    /// </summary>
    public AspectReferenceTargetKind TargetKind { get; }

    /// <summary>
    /// Gets a value indicating whether the reference is inlineable.
    /// </summary>
    public bool IsInlineable { get; }

    /// <summary>
    /// Gets a value indicating whether the reference has a custom receiver expression.
    /// </summary>
    public bool HasCustomReceiver { get; }

    /// <summary>
    /// Gets a value indicating whether the reference virtual, i.e. will have no substitution.
    /// Such references are used for concepts driven by the linker, for example event brokers.
    /// </summary>
    public bool IsVirtual { get; }

    public ResolvedAspectReference(
        IntermediateSymbolSemantic<IMethodSymbol> containingSemantic,
        IMethodSymbol? containingLocalFunction,
        ISymbol originalSymbol,
        IntermediateSymbolSemantic resolvedSemantic,
        IntermediateSymbolSemantic<IMethodSymbol>? explicitResolvedSemanticBody,
        SyntaxNode annotatedNode,
        SyntaxNode rootNode,
        SyntaxNode symbolSourceNode,
        AspectReferenceTargetKind targetKind,
        bool isInlineable,
        bool hasCustomReceiver,
        bool isVirtual )
    {
        Invariant.AssertNot(
            containingSemantic.Kind != IntermediateSymbolSemanticKind.Final
            && symbolSourceNode is not ExpressionSyntax
            && targetKind != AspectReferenceTargetKind.EventRaiseAccessor );

        Invariant.AssertNot(
            resolvedSemantic.Symbol.Kind == SymbolKind.Method && resolvedSemantic.Symbol is IMethodSymbol
            {
                MethodKind: not MethodKind.Ordinary and not MethodKind.ExplicitInterfaceImplementation and not MethodKind.Destructor
                and not MethodKind.UserDefinedOperator and not MethodKind.Conversion and not MethodKind.Constructor and not MethodKind.StaticConstructor
            } );

        this.ContainingSemantic = containingSemantic;
        this.ContainingLocalFunction = containingLocalFunction;
        this.OriginalSymbol = originalSymbol;
        this.ResolvedSemantic = resolvedSemantic;
#if DEBUG
        this.AnnotatedNode = annotatedNode;
#endif
        this.RootNode = rootNode;
        this.SymbolSourceNode = symbolSourceNode;
        this.TargetKind = targetKind;
        this.IsInlineable = isInlineable;
        this.HasCustomReceiver = hasCustomReceiver;
        this._explicitResolvedSemanticBody = explicitResolvedSemanticBody;
        this.IsVirtual = isVirtual;
    }

    public override string ToString()
        => $"{this.ContainingSemantic} ({(this.RootNode is ExpressionSyntax ? this.RootNode : "not expression")}) -> {this.ResolvedSemantic}";
}