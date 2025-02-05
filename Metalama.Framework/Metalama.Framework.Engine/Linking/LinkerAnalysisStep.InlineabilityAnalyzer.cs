// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Linking.Inlining;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    /// <summary>
    /// Analyzes inlineability of semantics and references. Determines which semantics and references will be inlined.
    /// </summary>
    public sealed class InlineabilityAnalyzer
    {
        private readonly IConcurrentTaskRunner _concurrentTaskRunner;
        private readonly HashSet<IntermediateSymbolSemantic> _reachableSymbolSemantics;
        private readonly CompilationContext _intermediateCompilationContext;
        private readonly InlinerProvider _inlinerProvider;
        private readonly IReadOnlyDictionary<AspectReferenceTarget, IReadOnlyCollection<ResolvedAspectReference>> _reachableReferencesByTarget;

        public InlineabilityAnalyzer(
            ProjectServiceProvider serviceProvider,
            CompilationContext intermediateCompilationContext,
            HashSet<IntermediateSymbolSemantic> reachableSymbolSemantics,
            InlinerProvider inlinerProvider,
            IReadOnlyDictionary<AspectReferenceTarget, IReadOnlyCollection<ResolvedAspectReference>> reachableReferencesByTarget )
        {
            this._reachableSymbolSemantics = reachableSymbolSemantics;
            this._intermediateCompilationContext = intermediateCompilationContext;
            this._inlinerProvider = inlinerProvider;
            this._reachableReferencesByTarget = reachableReferencesByTarget;
            this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
        }

        private IReadOnlyCollection<ResolvedAspectReference> GetReachableReferencesByTarget( AspectReferenceTarget target )
        {
            if ( !this._reachableReferencesByTarget.TryGetValue( target, out var references ) )
            {
                return Array.Empty<ResolvedAspectReference>();
            }

            return references;
        }

        /// <summary>
        /// Gets semantics that are inlineable, i.e. are not referenced too many times and don't have inlineability explicitly suppressed.
        /// </summary>
        /// <returns></returns>
        public async Task<HashSet<IntermediateSymbolSemantic>> GetInlineableSemanticsAsync(
            IReadOnlyDictionary<ISymbol, IntermediateSymbolSemantic> redirectedSymbols,
            CancellationToken cancellationToken )
        {
            var redirectionTargets =
                new HashSet<IntermediateSymbolSemantic>(
                    redirectedSymbols.Values,
                    IntermediateSymbolSemanticEqualityComparer.ForCompilation( this._intermediateCompilationContext ) );

            var inlineableSemantics = new HashSet<IntermediateSymbolSemantic>(
                IntermediateSymbolSemanticEqualityComparer.ForCompilation( this._intermediateCompilationContext ) );

            void ProcessSemantic( IntermediateSymbolSemantic semantic )
            {
                if ( IsInlineable( semantic ) )
                {
                    lock ( inlineableSemantics )
                    {
                        inlineableSemantics.Add( semantic );
                    }
                }
            }

            await this._concurrentTaskRunner.RunConcurrentlyAsync( this._reachableSymbolSemantics, ProcessSemantic, cancellationToken );

            return inlineableSemantics;

            bool IsInlineable( IntermediateSymbolSemantic semantic )
            {
                if ( semantic.Symbol.GetDeclarationFlags().HasFlagFast( AspectLinkerDeclarationFlags.NotInlineable ) )
                {
                    // Semantics marked as non-inlineable are not inlineable.
                    return false;
                }

                if ( semantic.Symbol is IPropertySymbol { SetMethod: null, OverriddenProperty: not null } getOnlyOverrideProperty
                     && getOnlyOverrideProperty.IsAutoProperty().GetValueOrDefault() )
                {
                    // TODO: Temporary limitation, we need virtualized IntermediateSymbolSemantics.
                    //       There is no Setter, but we need to analyze it's inlineability.
                    return false;
                }

                if ( redirectionTargets.Contains( semantic ) )
                {
                    // Redirection targets are not inlineable.
                    return false;
                }

                if ( semantic.Kind is IntermediateSymbolSemanticKind.Final )
                {
                    // Final semantics are never inlineable.
                    return false;
                }

                if ( semantic.Kind is IntermediateSymbolSemanticKind.Base
                     && (semantic.Symbol.IsOverride || semantic.Symbol.TryGetHiddenSymbol( this._intermediateCompilationContext.Compilation, out _ )) )
                {
                    // Base semantics are not inlineable if they point to a base member.
                    return false;
                }

                switch ( semantic.Symbol )
                {
                    case IMethodSymbol:
                        return IsInlineableMethod( semantic.ToTyped<IMethodSymbol>() );

                    case IPropertySymbol:
                        return IsInlineableProperty( semantic.ToTyped<IPropertySymbol>() );

                    case IEventSymbol:
                        return IsInlineableEvent( semantic.ToTyped<IEventSymbol>() );

                    case IFieldSymbol:
                        // Fields are never inlineable.
                        return false;

                    default:
                        throw new AssertionFailedException( $"Unexpected symbol: '{semantic.Symbol}'" );
                }
            }

            bool IsInlineableMethod( IntermediateSymbolSemantic<IMethodSymbol> semantic )
            {
                switch ( semantic.Symbol.MethodKind )
                {
                    case MethodKind.Ordinary:
                    case MethodKind.Constructor:
                    case MethodKind.StaticConstructor:
                    case MethodKind.ExplicitInterfaceImplementation:
                    case MethodKind.Destructor:
                    case MethodKind.UserDefinedOperator:
                    case MethodKind.Conversion:
                        var aspectReferences = this.GetReachableReferencesByTarget( semantic.ToAspectReferenceTarget() );

                        if ( aspectReferences.Count != 1 )
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }

                    case MethodKind.EventAdd:
                    case MethodKind.EventRemove:
                    case MethodKind.PropertyGet:
                    case MethodKind.PropertySet:
                        // Accessor methods are inlineable by themselves, further conditions are evaluated under inlineability of property/event.
                        return true;

                    default:
                        throw new AssertionFailedException( $"Unexpected method kind for '{semantic.Symbol}'." );
                }
            }

            bool IsInlineableProperty( IntermediateSymbolSemantic<IPropertySymbol> semantic )
            {
                if ( semantic.Symbol.IsAutoProperty() == true && semantic.Kind == IntermediateSymbolSemanticKind.Default )
                {
                    // Override target that is auto property is never inlineable.
                    return false;
                }
                else
                {
                    var getAspectReferences =
                        this.GetReachableReferencesByTarget( semantic.ToAspectReferenceTarget( AspectReferenceTargetKind.PropertyGetAccessor ) );

                    var setAspectReferences =
                        this.GetReachableReferencesByTarget( semantic.ToAspectReferenceTarget( AspectReferenceTargetKind.PropertySetAccessor ) );

                    if ( getAspectReferences.Count > 1 || setAspectReferences.Count > 1
                                                       || (getAspectReferences.Count == 0 && setAspectReferences.Count == 0) )
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            bool IsInlineableEvent( IntermediateSymbolSemantic<IEventSymbol> semantic )
            {
                if ( semantic.Symbol.IsEventFieldIntroduction() )
                {
                    // Override target that is event field is never inlineable.
                    return false;
                }
                else
                {
                    var addAspectReferences =
                        this.GetReachableReferencesByTarget( semantic.ToAspectReferenceTarget( AspectReferenceTargetKind.EventAddAccessor ) );

                    var removeAspectReferences =
                        this.GetReachableReferencesByTarget( semantic.ToAspectReferenceTarget( AspectReferenceTargetKind.EventRemoveAccessor ) );

                    if ( addAspectReferences.Count > 1 || removeAspectReferences.Count > 1
                                                       || (addAspectReferences.Count == 0 && removeAspectReferences.Count == 0) )
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Determines which aspect references can be inlined.
        /// </summary>
        /// <returns>Aspect references with selected inliners.</returns>
        public async Task<IReadOnlyDictionary<ResolvedAspectReference, Inliner>> GetInlineableReferencesAsync(
            HashSet<IntermediateSymbolSemantic> inlineableSemantics,
            CancellationToken cancellationToken )
        {
            var inlineableReferences = new ConcurrentDictionary<ResolvedAspectReference, Inliner>();

            void ProcessSemantic( IntermediateSymbolSemantic inlineableSemantic )
            {
                foreach ( var reference in this.GetReachableReferencesByTarget( inlineableSemantic.ToAspectReferenceTarget() ) )
                {
                    if ( IsInlineable( reference, out var inliner ) )
                    {
                        inlineableReferences.TryAdd( reference, inliner );
                    }
                }
            }

            await this._concurrentTaskRunner.RunConcurrentlyAsync( inlineableSemantics, ProcessSemantic, cancellationToken );

            return inlineableReferences;

            bool IsInlineable( ResolvedAspectReference reference, [NotNullWhen( true )] out Inliner? inliner )
            {
                if ( reference.ContainingSemantic.Symbol.GetDeclarationFlags().HasFlagFast( AspectLinkerDeclarationFlags.NotInliningDestination ) )
                {
                    // If containing semantic is marked as not being destination of inlining, the reference is not inlineable.
                    inliner = null;

                    return false;
                }

                if ( !reference.IsInlineable )
                {
                    // References that are not marked as inlineable cannot be inlined.
                    inliner = null;

                    return false;
                }

                if ( !this._intermediateCompilationContext.SymbolComparer.Equals(
                        reference.ContainingSemantic.Symbol.ContainingType,
                        reference.ResolvedSemantic.Symbol.ContainingType ) )
                {
                    // References between types cannot be inlined.
                    inliner = null;

                    return false;
                }

                if ( reference.SymbolSourceNode is not ExpressionSyntax )
                {
                    // Use a special inliner for non-expression references.
                    inliner = ImplicitLastOverrideReferenceInliner.Instance;

                    return true;
                }

                var semanticModel = this._intermediateCompilationContext.SemanticModelProvider.GetSemanticModel( reference.RootExpression.SyntaxTree );

                return this._inlinerProvider.TryGetInliner( reference, semanticModel, out inliner );
            }
        }

        /// <summary>
        /// Gets all semantics that are inlined.
        /// </summary>
        /// <param name="inlineableSemantics"></param>
        /// <param name="inlineableReferences"></param>
        /// <returns></returns>
        public async Task<HashSet<IntermediateSymbolSemantic>> GetInlinedSemanticsAsync(
            HashSet<IntermediateSymbolSemantic> inlineableSemantics,
            IReadOnlyDictionary<ResolvedAspectReference, Inliner> inlineableReferences,
            CancellationToken cancellationToken )
        {
            var inlinedSemantics = new HashSet<IntermediateSymbolSemantic>(
                IntermediateSymbolSemanticEqualityComparer.ForCompilation( this._intermediateCompilationContext ) );

            void ProcessSemantic( IntermediateSymbolSemantic inlineableSemantic )
            {
                if ( IsInlinedSemantic( inlineableSemantic ) )
                {
                    lock ( inlineableSemantics )
                    {
                        inlinedSemantics.Add( inlineableSemantic );
                    }
                }
            }

            await this._concurrentTaskRunner.RunConcurrentlyAsync( inlineableSemantics, ProcessSemantic, cancellationToken );

            return inlinedSemantics;

            bool IsInlinedSemanticBody( IntermediateSymbolSemantic<IMethodSymbol> semanticBody )
            {
                if ( !this._reachableReferencesByTarget.TryGetValue( semanticBody.ToAspectReferenceTarget(), out var aspectReferences ) )
                {
                    // This semantic does not have any incoming reference.
                    return false;
                }

                var anyNonInlineableReference = false;

                foreach ( var reference in aspectReferences )
                {
                    if ( !inlineableReferences.ContainsKey( reference ) )
                    {
                        anyNonInlineableReference = true;

                        break;
                    }
                }

                return !anyNonInlineableReference;
            }

            bool IsInlinedSemantic( IntermediateSymbolSemantic semantic )
            {
                switch ( semantic.Symbol )
                {
                    case IMethodSymbol
                    {
                        MethodKind: MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.UserDefinedOperator
                        or MethodKind.Conversion or MethodKind.Destructor or MethodKind.Constructor or MethodKind.StaticConstructor
                    }:
                        return IsInlinedSemanticBody( semantic.ToTyped<IMethodSymbol>() );

                    case IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet } propertyAccessor:
                        return IsInlinedSemantic( semantic.WithSymbol( propertyAccessor.AssociatedSymbol.AssertNotNull() ) );

                    case IMethodSymbol { MethodKind: MethodKind.EventAdd or MethodKind.EventRemove } eventAccessor:
                        return IsInlinedSemantic( semantic.WithSymbol( eventAccessor.AssociatedSymbol.AssertNotNull() ) );

                    case IPropertySymbol property:
                        // Property is inlined if at least one of the accessors is reachable and not inlineable.
                        var hasNonInlinedGet =
                            property.GetMethod != null
                            && !IsInlinedSemanticBody( semantic.WithSymbol( property.GetMethod ) )
                            && this._reachableSymbolSemantics.Contains( semantic.WithSymbol( property.GetMethod ) );

                        var hasNonInlinedSet =
                            property.SetMethod != null
                            && !IsInlinedSemanticBody( semantic.WithSymbol( property.SetMethod ) )
                            && this._reachableSymbolSemantics.Contains( semantic.WithSymbol( property.SetMethod ) );

                        return inlineableSemantics.Contains( semantic ) && !hasNonInlinedGet && !hasNonInlinedSet;

                    case IEventSymbol @event:
                        // Event is inlined if at least one of the accessors is reachable and not inlineable.
                        var hasNonInlinedAdd =
                            !IsInlinedSemanticBody( semantic.WithSymbol( @event.AddMethod.AssertNotNull() ) )
                            && this._reachableSymbolSemantics.Contains( semantic.WithSymbol( @event.AddMethod.AssertNotNull() ) );

                        var hasNonInlinedRemove =
                            !IsInlinedSemanticBody( semantic.WithSymbol( @event.RemoveMethod.AssertNotNull() ) )
                            && this._reachableSymbolSemantics.Contains( semantic.WithSymbol( @event.RemoveMethod.AssertNotNull() ) );

                        return inlineableSemantics.Contains( semantic ) && !hasNonInlinedAdd && !hasNonInlinedRemove;

                    default:
                        throw new AssertionFailedException( $"Unexpected symbol: '{semantic.Symbol}'." );
                }
            }
        }

        /// <summary>
        /// Gets all references that are inlined.
        /// </summary>
        /// <param name="inlineableReferences"></param>
        /// <param name="inlinedSemantics"></param>
        /// <returns></returns>
        public IReadOnlyDictionary<ResolvedAspectReference, Inliner> GetInlinedReferences(
            IReadOnlyDictionary<ResolvedAspectReference, Inliner> inlineableReferences,
            HashSet<IntermediateSymbolSemantic> inlinedSemantics )
        {
            var inlinedReferences = new Dictionary<ResolvedAspectReference, Inliner>();

            foreach ( var inlinedSemantic in inlinedSemantics )
            {
                foreach ( var reference in this.GetReachableReferencesByTarget( inlinedSemantic.ToAspectReferenceTarget() ) )
                {
                    if ( !inlineableReferences.TryGetValue( reference, out var inliner ) )
                    {
                        throw new AssertionFailedException( $"Cannot get the inlineable reference for '{reference.OriginalSymbol}'." );
                    }

                    lock ( inlinedReferences )
                    {
                        inlinedReferences.Add( reference, inliner );
                    }
                }
            }

            return inlinedReferences;
        }
    }
}