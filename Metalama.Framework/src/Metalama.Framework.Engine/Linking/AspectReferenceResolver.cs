// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Comparers;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

// Ordered declaration versions (intermediate compilation):
//  * Overridden declaration (base class declaration)
//  * Target declaration, base semantic (if from source code)
//  * Target declaration, default semantic (if introduced, no overridden declaration)
//  * Override 1-1
//  ...
//  * Override z-1-1
//  ...
//  * Override z-k-l (there are multiple overrides of the same declaration in one layer and multiple aspect instances).
//  ...
//  * Override n
//  * Target declaration,final semantic)

// Each of above correspond to an aspect layer in the global order.
// The reference we are trying to resolve also originates in of the aspect layers.

// Declaration semantics projected to global aspect layer order:
// * Layer (0, 0, 0):   Overridden declaration (base class declaration).
// * Layer (0, 0, 0):   Target declaration, default semantic (if from source code).
// * Layer (0, 0, 0):   Target declaration, base semantic (if introduced, no overridden declaration).
// ...
// * Layer (k, 0):   Target declaration, default semantic (if introduced).
// * Layer (k, 1, 1):   After introduction 1-1.
// * Layer (k, 1, 2):   After override 1-1 (same layer as introduction).
// ...
// * Layer (l_1, 1, 1): After override 2-1 (layer with multiple overrides).
// ...
// * Layer (l_1, 1, q_1): After override 2-q_1 in aspect instance 1.
// ...
// * Layer (l_n, p, q_n): After override q_n in aspect instance p.
// ...
// * Layer (m, 0):   Target declaration, final semantic.

// AspectReferenceOrder resolution:
//  * Base - resolved to the last override preceding the referencing layer.
//  * Previous - resolved to the last preceding override.
//  * Current - resolved to the last override preceding or equal to the referencing layer.
//  * Final - resolved to the last override in the order or final semantic.

// Special cases:
//  * Promoted fields do not count as introductions. The layer of the promotion target applies.
//    Source promoted fields are treated as source declarations. Introduced and then promoted fields
//    are treated as being introduced at the point of field introduction.

// Notes:
//  * Base and Self are different only for layers that override the referenced declaration.

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Resolves aspect references.
/// </summary>
internal sealed class AspectReferenceResolver
{
    private readonly LinkerInjectionRegistry _injectionRegistry;
    private readonly IReadOnlyList<AspectLayerId> _orderedLayers;
    private readonly IReadOnlyDictionary<AspectLayerId, int> _layerIndex;
    private readonly SafeSymbolComparer _comparer;
    private readonly ConcurrentDictionary<ISymbol, IReadOnlyList<OverrideIndex>> _overrideIndicesCache;

    public AspectReferenceResolver(
        LinkerInjectionRegistry injectionRegistry,
        IReadOnlyList<OrderedAspectLayer> orderedAspectLayers,
        CompilationContext intermediateCompilationContext )
    {
        this._injectionRegistry = injectionRegistry;

        var indexedLayers =
            new[] { AspectLayerId.Null }.Concat( orderedAspectLayers.SelectAsReadOnlyList( x => x.AspectLayerId ) )
                .Select( ( al, i ) => (AspectLayerId: al, Index: i) )
                .ToReadOnlyList();

        this._orderedLayers = indexedLayers.SelectAsImmutableArray( x => x.AspectLayerId );
        this._layerIndex = indexedLayers.ToDictionary( x => x.AspectLayerId, x => x.Index );
        this._comparer = intermediateCompilationContext.SymbolComparer;
        this._overrideIndicesCache = new ConcurrentDictionary<ISymbol, IReadOnlyList<OverrideIndex>>( intermediateCompilationContext.SymbolComparer );
    }

    public ResolvedAspectReference Resolve(
        IntermediateSymbolSemantic<IMethodSymbol> containingSemantic,
        IMethodSymbol? containingLocalFunction,
        ISymbol referencedSymbol,
        ExpressionSyntax expression,
        AspectReferenceSpecification referenceSpecification,
        SemanticModel semanticModel )
    {
        // Get the reference root node, the local symbol that is referenced, and the node that was the source for the symbol.
        //   1) Normal reference:
        //     this.Foo()
        //     ^^^^^^^^ - aspect reference (symbol points directly to the member)
        //     ^^^^^^^^ - symbol source
        //     ^^^^^^^^ - resolved root node
        //   2) Interface member references:
        //     ((IInterface)this).Foo()
        //                        ^^^ - aspect reference (symbol points to interface member)
        //                        ^^^ - symbol source
        //     ^^^^^^^^^^^^^^^^^^^^^^ - resolved root node
        //   3) Referencing a get-only property "setter":
        //     __LinkerInjectionHelpers__.__Property(this.Foo) = 42;
        //     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ - aspect reference (symbol points to the special linker helper)
        //                                           ^^^^^^^^  - symbol source
        //     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ - resolved root node
        //   4) Awaitable async-void method:
        //     __LinkerInjectionHelpers__.__AsyncVoidMethod(this.Foo)(<args>)
        //                                                  ^^^^^^^^  - aspect reference
        //                                                  ^^^^^^^^  - symbol source
        //     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ - resolved root node            

        this.ResolveTarget(
            containingSemantic.Symbol,
            referencedSymbol,
            expression,
            semanticModel,
            out var resolvedRootNode,
            out var resolvedReferencedSymbol,
            out var resolvedReferencedSymbolSourceNode );

        // resolvedRootNode is the node that will be replaced when rewriting the aspect reference.
        // resolvedReferencedSymbol is the real target of the reference.
        // resolvedReferencedSymbolSourceNode is the node that will be rewritten when renaming the aspect reference (e.g. redirecting to a particular override).

        var targetKind = referenceSpecification.TargetKind;
        var isInlineable = (referenceSpecification.Flags & AspectReferenceFlags.Inlineable) != 0;
        var hasCustomReceiver = (referenceSpecification.Flags & AspectReferenceFlags.CustomReceiver) != 0;

        if ( targetKind == AspectReferenceTargetKind.Self && resolvedReferencedSymbol is IPropertySymbol or IEventSymbol or IFieldSymbol )
        {
            // Resolves the symbol based on expression - this is used when aspect reference targets property/event/field
            // but it is not specified whether the getter/setter/adder/remover is targeted.
            targetKind = ResolveExpressionTarget( resolvedReferencedSymbol, expression );
        }

        // At this point we should always target a method or a specific target.
        Invariant.AssertNot( resolvedReferencedSymbol is IPropertySymbol or IEventSymbol or IFieldSymbol && targetKind == AspectReferenceTargetKind.Self );

        var annotationLayerIndex = this.GetAnnotationLayerIndex( containingSemantic.Symbol );

        // If the override target was introduced, determine the index.
        var targetIntroductionInjectedMember = this._injectionRegistry.GetInjectedMemberForSymbol( resolvedReferencedSymbol );
        var targetIntroductionIndex = this.GetIntroductionLogicalIndex( targetIntroductionInjectedMember );

        var overrideIndices = this.GetOverrideIndices( resolvedReferencedSymbol );

        Invariant.Assert(
            targetIntroductionIndex == null || overrideIndices.All( o => targetIntroductionIndex < o.Index )
                                            || !HasImplicitImplementation( referencedSymbol ) );

        this.ResolveLayerIndex(
            referenceSpecification,
            annotationLayerIndex,
            resolvedReferencedSymbol,
            targetIntroductionInjectedMember,
            targetIntroductionIndex,
            overrideIndices,
            out var resolvedIndex,
            out var resolvedInjectedMember );

        // At this point resolvedIndex should be 0, equal to target introduction index, this._orderedLayers.Count or be equal to index of one of the overrides.
        Invariant.Assert(
            resolvedIndex == default
            || resolvedIndex == new MemberLayerIndex( this._orderedLayers.Count, 0, 0 )
            || overrideIndices.Any( x => x.Index == resolvedIndex )
            || resolvedIndex == targetIntroductionIndex );

        if ( overrideIndices.Count > 0 && resolvedIndex == overrideIndices[^1].Index )
        {
            // If we have resolved to the last override, transition to the final declaration index.
            resolvedIndex = new MemberLayerIndex( this._orderedLayers.Count, 0, 0 );
        }

        if ( resolvedIndex == default )
        {
            // Resolved to the initial version of the symbol (before any aspects).

            if ( targetIntroductionInjectedMember == null
                 || (targetIntroductionInjectedMember.Transformation is IReplaceMemberTransformation { ReplacedMember: { } replacedMember }
                     && replacedMember.HasSymbol()) )
            {
                // Historical note: the incorrect "!" symbol removed from the above line cost me at least 8 hours of debugging.
                
                // There is no introduction, i.e. this is a user source symbol (or a promoted field) => reference the version present in source.
                var declaredInCurrentType = this._comparer.Equals( containingSemantic.Symbol.ContainingType, resolvedReferencedSymbol.ContainingType );

                var targetSemantic =
                    (!declaredInCurrentType && resolvedReferencedSymbol.IsVirtual)
                    || (declaredInCurrentType && resolvedReferencedSymbol is { IsOverride: true, IsSealed: false } or { IsVirtual: true }
                                              && overrideIndices.Count == 0)
                        ? resolvedReferencedSymbol.ToSemantic( IntermediateSymbolSemanticKind.Base )
                        : resolvedReferencedSymbol.ToSemantic( IntermediateSymbolSemanticKind.Default );

                return CreateResolved( targetSemantic );
            }
            else
            {
                // There is an introduction and this reference points to a state before that introduction.
                return CreateResolved( resolvedReferencedSymbol.ToSemantic( IntermediateSymbolSemanticKind.Base ) );
            }
        }
        else if ( targetIntroductionInjectedMember != null && resolvedIndex < targetIntroductionIndex )
        {
            // Resolved to a version before the symbol was introduced.
            // The only valid case are introduced promoted fields.
            if ( targetIntroductionInjectedMember.Transformation is IReplaceMemberTransformation { ReplacedMember: { } replacedMember }
                 && !replacedMember.HasSymbol() )
            {
                // This is the same as targeting the property.
                return CreateResolved( resolvedReferencedSymbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) );
            }
            else
            {
                throw new AssertionFailedException(
                    $"Resolving {resolvedReferencedSymbol} aspect reference to a non-initial state before the introduction is valid only for replaced introduced members." );
            }
        }
        else if ( targetIntroductionInjectedMember != null && resolvedIndex == targetIntroductionIndex )
        {
            // Targeting the introduced version of the symbol.
            // The only way to get here is for declarations with implicit implementation, everything else is not valid.

            if ( HasImplicitImplementation( resolvedReferencedSymbol ) )
            {
                return CreateResolved( resolvedReferencedSymbol.ToSemantic( IntermediateSymbolSemanticKind.Default ) );
            }
            else
            {
                throw new AssertionFailedException(
                    $"Resolving {resolvedReferencedSymbol} aspect reference to the introduction is not allowed because the declaration does not have implicit body." );
            }
        }
        else if ( resolvedIndex < new MemberLayerIndex( this._orderedLayers.Count, 0, 0 ) )
        {
            // One particular override.
            return CreateResolved(
                this.GetSymbolFromInjectedMember( resolvedReferencedSymbol, resolvedInjectedMember.AssertNotNull() )
                    .ToSemantic( IntermediateSymbolSemanticKind.Default ) );
        }
        else if ( resolvedIndex == new MemberLayerIndex( this._orderedLayers.Count, 0, 0 ) )
        {
            var targetSemantic =
                overrideIndices.Count > 0
                    ? resolvedReferencedSymbol.ToSemantic( IntermediateSymbolSemanticKind.Final )
                    : resolvedReferencedSymbol.ToSemantic( IntermediateSymbolSemanticKind.Default );

            // The version after all aspects.
            return CreateResolved( targetSemantic );
        }
        else
        {
            throw new AssertionFailedException( $"Resolving {resolvedReferencedSymbol} aspect reference to {resolvedIndex} is not supported." );
        }

        ResolvedAspectReference CreateResolved( IntermediateSymbolSemantic resolvedSemantic )
        {
            return new ResolvedAspectReference(
                containingSemantic,
                containingLocalFunction,
                resolvedReferencedSymbol,
                resolvedSemantic,
                expression,
                resolvedRootNode,
                resolvedReferencedSymbolSourceNode,
                targetKind,
                isInlineable,
                hasCustomReceiver );
        }
    }

    private void ResolveLayerIndex(
        AspectReferenceSpecification referenceSpecification,
        MemberLayerIndex annotationLayerIndex,
        ISymbol referencedSymbol,
        InjectedMember? targetIntroductionInjectedMember,
        MemberLayerIndex? targetIntroductionIndex,
        IReadOnlyList<OverrideIndex> overrideIndices,
        out MemberLayerIndex resolvedIndex,
        out InjectedMember? resolvedInjectedMember )
    {
        resolvedInjectedMember = null;

        switch ( referenceSpecification.Order )
        {
            case AspectReferenceOrder.Base:
                // TODO: optimize.

                var lowerOverride = overrideIndices.LastOrDefault( x => x.Index.LayerIndex < annotationLayerIndex.LayerIndex );

                if ( lowerOverride.Override != null )
                {
                    resolvedIndex = lowerOverride.Index;
                    resolvedInjectedMember = lowerOverride.Override;
                }
                else if ( targetIntroductionIndex != null 
                          && targetIntroductionIndex.Value.WithoutTransformationIndex() < annotationLayerIndex.WithoutTransformationIndex()
                          && HasImplicitImplementation( referencedSymbol ) )
                {
                    // We specifically want the index without the transformation index to test that the introduction happened in earlier aspect.
                    resolvedIndex = targetIntroductionIndex.Value;
                    resolvedInjectedMember = targetIntroductionInjectedMember;
                }
                else
                {
                    resolvedIndex = default;
                }

                break;

            case AspectReferenceOrder.Previous:

                var previousOverride = overrideIndices.LastOrDefault( x => x.Index < annotationLayerIndex );

                if ( previousOverride.Override != null )
                {
                    resolvedIndex = previousOverride.Index;
                    resolvedInjectedMember = previousOverride.Override;
                }
                else if ( targetIntroductionIndex != null && targetIntroductionIndex.Value < annotationLayerIndex
                                                          && HasImplicitImplementation( referencedSymbol ) )
                {
                    resolvedIndex = targetIntroductionIndex.Value;
                    resolvedInjectedMember = targetIntroductionInjectedMember;
                }
                else
                {
                    resolvedIndex = default;
                }

                break;

            case AspectReferenceOrder.Current:
                // TODO: optimize.

                var lowerOrEqualOverride = overrideIndices.LastOrDefault( x => x.Index.LayerIndex <= annotationLayerIndex.LayerIndex );

                if ( lowerOrEqualOverride.Override != null )
                {
                    resolvedIndex = lowerOrEqualOverride.Index;
                    resolvedInjectedMember = lowerOrEqualOverride.Override;
                }
                else if ( targetIntroductionIndex != null && targetIntroductionIndex.Value <= annotationLayerIndex )
                {
                    Invariant.Assert( HasImplicitImplementation( referencedSymbol ) );

                    resolvedIndex = targetIntroductionIndex.Value;
                    resolvedInjectedMember = targetIntroductionInjectedMember;
                }
                else
                {
                    resolvedIndex = default;
                }

                break;

            case AspectReferenceOrder.Final:
                resolvedIndex = new MemberLayerIndex( this._orderedLayers.Count, 0, 0 );

                break;

            default:
                throw new AssertionFailedException( $"Unexpected value for AspectReferenceOrder: {referenceSpecification.Order}." );
        }
    }

    private IReadOnlyList<OverrideIndex> GetOverrideIndices( ISymbol referencedSymbol )
    {
        // PERF: Caching prevents reallocation for every override.
        return this._overrideIndicesCache.GetOrAdd( referencedSymbol, Get, this );

        static IReadOnlyList<OverrideIndex> Get( ISymbol referencedSymbol, AspectReferenceResolver @this )
        {
            var referencedDeclarationOverrides = @this._injectionRegistry.GetOverridesForSymbol( referencedSymbol );

            // Order coming from transformation needs to be incremented by 1, because 0 represents state before the aspect layer.
            return
                referencedDeclarationOverrides
                    .SelectAsReadOnlyList(
                        x =>
                        {
                            var injectedMember = @this._injectionRegistry.GetInjectedMemberForSymbol( x ).AssertNotNull();

                            return
                                new OverrideIndex(
                                    @this.GetMemberLayerIndex( injectedMember ),
                                    injectedMember );
                        } )
                    .Materialize()
                    .AssertSorted( x => x.Index );
        }
    }

    private MemberLayerIndex? GetIntroductionLogicalIndex( InjectedMember? injectedMember )
    {
        // This supports only field promotions.
        if ( injectedMember == null )
        {
            return null;
        }

        if ( injectedMember.Transformation is IReplaceMemberTransformation { ReplacedMember: { } replacedMember } )
        {
            if ( replacedMember is IIntroducedRef { BuilderData: { } builderData } )
            {
                // This is introduced field, which is then promoted. Semantics of the field and of the property are the same.
                var fieldInjectionTransformation =
                    this._injectionRegistry.GetTransformationForBuilder( builderData )
                    ?? throw new AssertionFailedException( $"Could not find transformation for {builderData}" );

                // Order coming from transformation needs to be incremented by 1, because 0 represents state before the aspect layer.
                return
                    new MemberLayerIndex(
                        this._layerIndex[builderData.ParentAdvice.AspectLayerId],
                        fieldInjectionTransformation.OrderWithinPipelineStepAndType + 1,
                        fieldInjectionTransformation.OrderWithinPipelineStepAndTypeAndAspectInstance + 1 );
            }
            else
            {
                // This is promoted source declaration we treat it as being present from the beginning.
                return new MemberLayerIndex( 0, 0, 0 );
            }
        }

        return this.GetMemberLayerIndex( injectedMember );
    }

    private MemberLayerIndex GetAnnotationLayerIndex( ISymbol containingSymbol )
    {
        var containingInjectedMember =
            this._injectionRegistry.GetInjectedMemberForSymbol( containingSymbol )
            ?? throw new AssertionFailedException( $"Could not find injected member for {containingSymbol}." );

        return this.GetMemberLayerIndex( containingInjectedMember );
    }

    private MemberLayerIndex GetMemberLayerIndex( InjectedMember injectedMember )
        => injectedMember.Transformation != null
            ? new MemberLayerIndex(
                this._layerIndex[injectedMember.AspectLayerId.AssertNotNull()],
                injectedMember.Transformation.OrderWithinPipelineStepAndType + 1,
                injectedMember.Transformation.OrderWithinPipelineStepAndTypeAndAspectInstance + 1 )
            : new MemberLayerIndex( 0, 0, 0 );

    /// <summary>
    /// Resolves target symbol of the reference.
    /// </summary>
    /// <param name="containingSymbol">Symbol contains the reference.</param>
    /// <param name="referencedSymbol">Symbol that is referenced.</param>
    /// <param name="expression">Annotated expression.</param>
    /// <param name="semanticModel">Semantic model.</param>
    /// <param name="rootNode">Root of the reference that need to be rewritten (usually equal to the annotated expression).</param>
    /// <param name="targetSymbol">Symbol that the reference targets (the target symbol of the reference).</param>
    /// <param name="targetSymbolSource">Expression that identifies the target symbol (usually equal to the annotated expression).</param>
    private void ResolveTarget(
        ISymbol containingSymbol,
        ISymbol referencedSymbol,
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        out ExpressionSyntax rootNode,
        out ISymbol targetSymbol,
        out ExpressionSyntax targetSymbolSource )
    {
        // Check whether we are referencing explicit interface implementation.
        if ( (!this._comparer.Equals( containingSymbol.ContainingType, referencedSymbol.ContainingType )
              && referencedSymbol.ContainingType.TypeKind == TypeKind.Interface)
             || referencedSymbol.IsExplicitInterfaceMemberImplementation() )
        {
            // TODO: For some reason we get here in two ways (see the condition):
            //          1) The symbol is directly interface symbol (first condition matches).
            //          2) sometimes it is a "reference", i.e. contained within the current type (second condition matches).
            //       This may depend on the declaring assembly or on presence of compilation errors.

            // It's not possible to reference the introduced explicit interface implementation directly, so the reference expression
            // is in form "((<interface_type>)this).<member>", which means that the symbol points to interface member. We will transition
            // to the real member (explicit implementation) of the type before doing the rest of resolution.

            // Replace the referenced symbol with the overridden interface implementation.    
            rootNode = expression;
            targetSymbol = containingSymbol.ContainingType.AssertNotNull().FindImplementationForInterfaceMember( referencedSymbol ).AssertNotNull();
            targetSymbolSource = expression;

            return;
        }

        if ( expression.Parent?.Parent?.Parent?.Parent is InvocationExpressionSyntax { Expression: { } wrappingExpression }
             && semanticModel.GetSymbolInfo( wrappingExpression ).Symbol is IMethodSymbol
             {
                 ContainingType.Name: LinkerInjectionHelperProvider.HelperTypeName
             } wrappingHelperMethod )
        {
            // Wrapping helper methods are used in special cases where the generated expression needs to be additionally wrapped.

            switch ( wrappingHelperMethod )
            {
                case { Name: LinkerInjectionHelperProvider.AsyncVoidMethodMemberName }:
                    // Referencing async-void method.
                    rootNode = wrappingExpression;
                    targetSymbolSource = expression;
                    targetSymbol = referencedSymbol;

                    return;

                default:
                    throw new AssertionFailedException( $"Unexpected wrapping helper method: '{wrappingHelperMethod}'." );
            }
        }

        if ( referencedSymbol is IMethodSymbol { ContainingType.Name: LinkerInjectionHelperProvider.HelperTypeName } helperMethod )
        {
            switch ( helperMethod )
            {
                case { Name: LinkerInjectionHelperProvider.FinalizeMemberName }:
                    // Referencing type's finalizer.
                    rootNode = expression;
                    targetSymbolSource = expression;

                    targetSymbol = containingSymbol.ContainingType.GetMembers( "Finalize" )
                        .OfType<IMethodSymbol>()
                        .Single( m => m.Parameters.Length == 0 && m.TypeParameters.Length == 0 );

                    return;

                case { Name: LinkerInjectionHelperProvider.StaticConstructorMemberName }:
                    // Referencing type's constructor.
                    switch ( expression.Parent )
                    {
                        case InvocationExpressionSyntax { ArgumentList.Arguments: [] }:
                            rootNode = expression;
                            targetSymbol = containingSymbol.ContainingType.StaticConstructors.FirstOrDefault().AssertNotNull();
                            targetSymbolSource = expression;

                            return;

                        default:
                            throw new AssertionFailedException( $"Unexpected static constructor expression: '{expression.Parent}'." );
                    }

                case { Name: LinkerInjectionHelperProvider.ConstructorMemberName }:
                    // Referencing type's constructor.
                    switch ( expression.Parent )
                    {
                        case InvocationExpressionSyntax
                        {
                            ArgumentList.Arguments: [{ Expression: ObjectCreationExpressionSyntax objectCreation }]
                        } invocationExpression:

                            rootNode = invocationExpression;

                            // TODO: This is hacky - since we don't see any introduced parameter while expanding a template, the target symbol of the aspect
                            //       reference is not valid (either unresolved or pointing to a wrong constructor).
                            //       Using the override target (which is correctly resolved) is a temporary solution until we need to have constructor invokers.

                            var overrideTarget =
                                this._injectionRegistry.GetOverrideTarget( containingSymbol )
                                ?? throw new AssertionFailedException( $"Could not resolve override target for '{containingSymbol}'" );

                            targetSymbol = overrideTarget;
                            targetSymbolSource = objectCreation;

                            return;

                        default:
                            throw new AssertionFailedException( $"Unexpected constructor expression: '{expression.Parent}'." );
                    }

                case { Name: LinkerInjectionHelperProvider.PropertyMemberName }:
                    // Referencing a property.
                    switch ( expression.Parent )
                    {
                        case InvocationExpressionSyntax
                        {
                            ArgumentList.Arguments: [{ Expression: MemberAccessExpressionSyntax memberAccess }]
                        } invocationExpression:
                            var symbolInfo = semanticModel.GetSymbolInfo( memberAccess );

                            rootNode = invocationExpression;

                            // This should match AspectReferenceWalker's VisitCore method as it does the same symbol resolution.
                            targetSymbol =
                                symbolInfo switch
                                {
                                    // Normal situation with valid symbol.
                                    { Symbol: { } symbol } => symbol,

                                    // When the code is invalid, there are usually 
                                    { CandidateSymbols: [{ } symbol] } => symbol,

                                    // We should not get here because this reference would be skipped by AspectReferenceWalker.
                                    _ => throw new AssertionFailedException( $"Invalid symbol info: {symbolInfo}" )
                                };

                            targetSymbolSource = memberAccess;

                            return;

                        default:
                            throw new AssertionFailedException( $"Unexpected property expression: '{expression.Parent}'." );
                    }

                case not null when SymbolHelpers.GetOperatorKindFromName( helperMethod.Name ) is not OperatorKind.None and var operatorKind:
                    // Referencing an operator.
                    rootNode = expression;
                    targetSymbolSource = expression;

                    if ( operatorKind.GetCategory() == OperatorCategory.Binary )
                    {
                        targetSymbol = containingSymbol.ContainingType.GetMembers( referencedSymbol.Name )
                            .OfType<IMethodSymbol>()
                            .Single(
                                m =>
                                    m.Parameters.Length == 2
                                    && SignatureTypeComparer.Instance.Equals( m.Parameters[0].Type, helperMethod.Parameters[0].Type )
                                    && SignatureTypeComparer.Instance.Equals( m.Parameters[1].Type, helperMethod.Parameters[1].Type )
                                    && SignatureTypeComparer.Instance.Equals( m.ReturnType, helperMethod.ReturnType ) );
                    }
                    else
                    {
                        targetSymbol = containingSymbol.ContainingType.GetMembers( referencedSymbol.Name )
                            .OfType<IMethodSymbol>()
                            .Single(
                                m =>
                                    m.Parameters.Length == 1
                                    && SignatureTypeComparer.Instance.Equals( m.Parameters[0].Type, helperMethod.Parameters[0].Type )
                                    && SignatureTypeComparer.Instance.Equals( m.ReturnType, helperMethod.ReturnType ) );
                    }

                    return;

                default:
                    throw new AssertionFailedException( $"Unexpected helper method: '{helperMethod}'." );
            }
        }

        rootNode = expression;
        targetSymbol = referencedSymbol;
        targetSymbolSource = expression;
    }

    private static AspectReferenceTargetKind ResolveExpressionTarget( ISymbol referencedSymbol, ExpressionSyntax expression )
    {
        switch (referencedSymbol, expression)
        {
            case (IPropertySymbol, { Parent: AssignmentExpressionSyntax }):
                return AspectReferenceTargetKind.PropertySetAccessor;

            case (IPropertySymbol, _):
                return AspectReferenceTargetKind.PropertyGetAccessor;

            case (IFieldSymbol, { Parent: AssignmentExpressionSyntax }):
                return AspectReferenceTargetKind.PropertySetAccessor;

            case (IFieldSymbol, _):
                return AspectReferenceTargetKind.PropertyGetAccessor;

            case (IEventSymbol, { Parent: AssignmentExpressionSyntax { OperatorToken.RawKind: (int) SyntaxKind.AddAssignmentExpression } }):
                return AspectReferenceTargetKind.EventAddAccessor;

            case (IEventSymbol, { Parent: AssignmentExpressionSyntax { OperatorToken.RawKind: (int) SyntaxKind.SubtractAssignmentExpression } }):
                return AspectReferenceTargetKind.EventRemoveAccessor;

            case (IEventSymbol, _):
                return AspectReferenceTargetKind.EventRaiseAccessor;

            default:
                throw new AssertionFailedException( $"Unexpected referenced symbol: '{referencedSymbol}'" );
        }
    }

    private static bool HasImplicitImplementation( ISymbol symbol )
    {
        switch ( symbol )
        {
            case IFieldSymbol:
            case IPropertySymbol property when property.IsAutoProperty().GetValueOrDefault():
            case IEventSymbol @event when @event.IsExplicitInterfaceEventField() || @event.IsEventField().GetValueOrDefault():
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Translates the resolved injected member to the same kind of symbol as the referenced symbol.
    /// </summary>
    /// <param name="referencedSymbol"></param>
    /// <param name="resolvedInjectedMember"></param>
    /// <returns></returns>
    private ISymbol GetSymbolFromInjectedMember( ISymbol referencedSymbol, InjectedMember resolvedInjectedMember )
    {
        var symbol = this._injectionRegistry.GetSymbolForInjectedMember( resolvedInjectedMember );

        return GetCorrespondingSymbolForResolvedSymbol( referencedSymbol, symbol );
    }

    /// <summary>
    /// Gets a symbol that corresponds to the referenced symbol for the resolved symbol. 
    /// This has a meaning when referenced symbol was a property/event accessor and the resolved symbol is the property/event itself.
    /// </summary>
    /// <param name="referencedSymbol"></param>
    /// <param name="resolvedSymbol"></param>
    /// <returns></returns>
    private static ISymbol GetCorrespondingSymbolForResolvedSymbol( ISymbol referencedSymbol, ISymbol resolvedSymbol )
    {
        switch (referencedSymbol, resolvedSymbol)
        {
            case (IMethodSymbol { MethodKind: MethodKind.Constructor }, IMethodSymbol { MethodKind: MethodKind.Constructor }):
            case (IMethodSymbol { MethodKind: MethodKind.StaticConstructor }, IMethodSymbol { MethodKind: MethodKind.Ordinary }):
            case (IMethodSymbol { MethodKind: MethodKind.Ordinary }, IMethodSymbol { MethodKind: MethodKind.Ordinary }):
            case (IMethodSymbol { MethodKind: MethodKind.ExplicitInterfaceImplementation }, IMethodSymbol { MethodKind: MethodKind.Ordinary }):
            case (IMethodSymbol { MethodKind: MethodKind.ExplicitInterfaceImplementation },
                IMethodSymbol { MethodKind: MethodKind.ExplicitInterfaceImplementation }):
            case (IMethodSymbol { MethodKind: MethodKind.Destructor }, IMethodSymbol { MethodKind: MethodKind.Ordinary }):
            case (IMethodSymbol { MethodKind: MethodKind.Conversion or MethodKind.UserDefinedOperator }, IMethodSymbol { MethodKind: MethodKind.Ordinary }):
            case (IPropertySymbol, IPropertySymbol):
            case (IEventSymbol, IEventSymbol):
            case (IFieldSymbol, IFieldSymbol):
                return resolvedSymbol;

            case (IMethodSymbol { MethodKind: MethodKind.PropertyGet }, IPropertySymbol):
                // This seems to happen only in invalid compilations.
                throw new AssertionFailedException( Justifications.CoverageMissing );

            // return propertySymbol.GetMethod.AssertNotNull();

            case (IMethodSymbol { MethodKind: MethodKind.PropertySet }, IPropertySymbol):
                // This seems to happen only in invalid compilations.
                throw new AssertionFailedException( Justifications.CoverageMissing );

            // return propertySymbol.SetMethod.AssertNotNull();

            case (IMethodSymbol { MethodKind: MethodKind.EventAdd }, IEventSymbol):
                // This seems to happen only in invalid compilations.
                throw new AssertionFailedException( Justifications.CoverageMissing );

            // return eventSymbol.AddMethod.AssertNotNull();

            case (IMethodSymbol { MethodKind: MethodKind.EventRemove }, IEventSymbol):
                // This seems to happen only in invalid compilations.
                throw new AssertionFailedException( Justifications.CoverageMissing );

            // return eventSymbol.RemoveMethod.AssertNotNull();

            default:
                throw new AssertionFailedException( $"Unexpected combination: ('{referencedSymbol}', '{resolvedSymbol}')" );
        }
    }

    private record struct OverrideIndex( MemberLayerIndex Index, InjectedMember Override );
}