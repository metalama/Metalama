// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    /// <summary>
    /// Collects aspect reference in the intermediate compilation.
    /// </summary>
    private sealed class AspectReferenceCollector
    {
        private readonly PartialCompilation _intermediateCompilation;
        private readonly IConcurrentTaskRunner _concurrentTaskRunner;
        private readonly LinkerInjectionRegistry _injectionRegistry;
        private readonly AspectReferenceResolver _referenceResolver;
        private readonly SemanticModelProvider _semanticModelProvider;

        public AspectReferenceCollector(
            ProjectServiceProvider serviceProvider,
            PartialCompilation intermediateCompilation,
            LinkerInjectionRegistry injectionRegistry,
            AspectReferenceResolver referenceResolver )
        {
            this._semanticModelProvider = intermediateCompilation.Compilation.GetSemanticModelProvider();
            this._injectionRegistry = injectionRegistry;
            this._referenceResolver = referenceResolver;
            this._concurrentTaskRunner = serviceProvider.GetRequiredService<IConcurrentTaskRunner>();
            this._intermediateCompilation = intermediateCompilation;
        }

        public async Task<IReadOnlyDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyCollection<ResolvedAspectReference>>> RunAsync(
            CancellationToken cancellationToken )
        {
            var aspectReferences =
                new ConcurrentDictionary<IntermediateSymbolSemantic<IMethodSymbol>, IReadOnlyCollection<ResolvedAspectReference>>(
                    IntermediateSymbolSemanticEqualityComparer<IMethodSymbol>.ForCompilation( this._intermediateCompilation.CompilationContext ) );

            // Add implicit references going from final semantic to the last override.
            var overriddenMembers = this._injectionRegistry.GetOverriddenMembers();
            await this._concurrentTaskRunner.RunConcurrentlyAsync( overriddenMembers, ProcessOverriddenMember, cancellationToken );

            void ProcessOverriddenMember( ISymbol overriddenMember )
            {
                switch ( overriddenMember.Kind )
                {
                    case SymbolKind.Method when overriddenMember is IMethodSymbol method:
                        AddImplicitReference(
                            method.ToSemantic( IntermediateSymbolSemanticKind.Final ),
                            method,
                            this._injectionRegistry.GetLastOverride( method ).ToSemantic( IntermediateSymbolSemanticKind.Default ),
                            AspectReferenceTargetKind.Self );

                        break;

                    case SymbolKind.Property when overriddenMember is IPropertySymbol property:
                        if ( property.GetMethod != null )
                        {
                            AddImplicitReference(
                                property.GetMethod.ToSemantic( IntermediateSymbolSemanticKind.Final ),
                                property,
                                this._injectionRegistry.GetLastOverride( property ).ToSemantic( IntermediateSymbolSemanticKind.Default ),
                                AspectReferenceTargetKind.PropertyGetAccessor );
                        }

                        if ( property.SetMethod != null )
                        {
                            AddImplicitReference(
                                property.SetMethod.ToSemantic( IntermediateSymbolSemanticKind.Final ),
                                property,
                                this._injectionRegistry.GetLastOverride( property ).ToSemantic( IntermediateSymbolSemanticKind.Default ),
                                AspectReferenceTargetKind.PropertySetAccessor );
                        }

                        break;

                    case SymbolKind.Event when overriddenMember is IEventSymbol @event:
                        var lastOverride = this._injectionRegistry.GetLastOverride( @event );

                        AddImplicitReference(
                            @event.AddMethod.AssertNotNull().ToSemantic( IntermediateSymbolSemanticKind.Final ),
                            @event,
                            lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                            AspectReferenceTargetKind.EventAddAccessor );

                        AddImplicitReference(
                            @event.RemoveMethod.AssertNotNull().ToSemantic( IntermediateSymbolSemanticKind.Final ),
                            @event,
                            lastOverride.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                            AspectReferenceTargetKind.EventRemoveAccessor );

                        if ( this._injectionRegistry.HasEventRaiseOverride( @event ) )
                        {
                            var overrides = this._injectionRegistry.GetOverridesForSymbol( @event );

                            var previousSemantic = @event.ToSemantic( IntermediateSymbolSemanticKind.Final );

                            for ( var i = overrides.Count - 1; i >= 0; i-- )
                            {
                                if ( this._injectionRegistry.HasEventRaiseOverride( overrides[i] ) )
                                {
                                    var eventRaiseOverride =
                                        (IMethodSymbol?) this._injectionRegistry.GetSatelliteOverrideMembers( overrides[i] ).SingleOrDefault();

                                    var sourceAddSemantic = previousSemantic.Symbol.AddMethod.AssertNotNull().ToSemantic( previousSemantic.Kind );
                                    var sourceRemoveSemantic = previousSemantic.Symbol.RemoveMethod.AssertNotNull().ToSemantic( previousSemantic.Kind );

                                    AddImplicitReference(
                                        sourceAddSemantic,
                                        @event,
                                        overrides[i].ToSemantic( IntermediateSymbolSemanticKind.Default ),
                                        AspectReferenceTargetKind.EventRaiseAccessor,
                                        eventRaiseOverride,
                                        false,
                                        i != overrides.Count - 1 );

                                    AddImplicitReference(
                                        sourceRemoveSemantic,
                                        @event,
                                        overrides[i].ToSemantic( IntermediateSymbolSemanticKind.Default ),
                                        AspectReferenceTargetKind.EventRaiseAccessor,
                                        eventRaiseOverride,
                                        false,
                                        i != overrides.Count - 1 );
                                }

                                previousSemantic = overrides[i].ToSemantic( IntermediateSymbolSemanticKind.Default ).ToTyped<IEventSymbol>();
                            }
                        }

                        break;
                }

                void AddImplicitReference(
                    IntermediateSymbolSemantic<IMethodSymbol> containingSemantic,
                    ISymbol contextSymbol,
                    IntermediateSymbolSemantic targetSemantic,
                    AspectReferenceTargetKind targetKind,
                    IMethodSymbol? explicitSemanticBody = null,
                    bool? isInlineable = null,
                    bool? isVirtual = null )
                {
                    var declarationSyntax = containingSemantic.Symbol.GetPrimaryDeclarationSyntax();

                    // For synthesized record property accessors (e.g. get_EqualityContract), the accessor method has
                    // no syntax of its own. Fall back to the containing type's syntax (the record declaration).
                    if ( declarationSyntax == null && containingSemantic.Symbol.ContainingType != null )
                    {
                        declarationSyntax = containingSemantic.Symbol.ContainingType.GetPrimaryDeclarationSyntax();
                    }

                    var sourceNode =
                        declarationSyntax?.Kind() switch
                        {
                            SyntaxKind.ConstructorDeclaration when declarationSyntax is ConstructorDeclarationSyntax constructor
                                => constructor.Body ?? (SyntaxNode?) constructor.ExpressionBody ?? constructor,
                            SyntaxKind.MethodDeclaration when declarationSyntax is MethodDeclarationSyntax method
                                => method.Body ?? (SyntaxNode?) method.ExpressionBody ?? method,
                            SyntaxKind.DestructorDeclaration when declarationSyntax is DestructorDeclarationSyntax destructor
                                => destructor.Body
                                   ?? (SyntaxNode?) destructor.ExpressionBody
                                   ?? throw new AssertionFailedException(
                                       $"'{containingSemantic.Symbol}' has no implementation." ),
                            SyntaxKind.OperatorDeclaration when declarationSyntax is OperatorDeclarationSyntax @operator
                                => @operator.Body
                                   ?? (SyntaxNode?) @operator.ExpressionBody
                                   ?? @operator,
                            SyntaxKind.ConversionOperatorDeclaration when declarationSyntax is ConversionOperatorDeclarationSyntax conversionOperator
                                => conversionOperator.Body
                                   ?? (SyntaxNode?) conversionOperator.ExpressionBody
                                   ?? conversionOperator,
                            SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration
                                or SyntaxKind.AddAccessorDeclaration or SyntaxKind.RemoveAccessorDeclaration
                                when declarationSyntax is AccessorDeclarationSyntax accessor
                                => accessor.Body
                                   ?? (SyntaxNode?) accessor.ExpressionBody
                                   ?? accessor ?? throw new AssertionFailedException(
                                       $"'{containingSemantic.Symbol}' has no implementation." ),
                            SyntaxKind.VariableDeclarator when declarationSyntax is VariableDeclaratorSyntax declarator
                                => declarator
                                   ?? throw new AssertionFailedException(
                                       $"'{containingSemantic.Symbol}' has no implementation." ),
                            SyntaxKind.ArrowExpressionClause when declarationSyntax is ArrowExpressionClauseSyntax arrowExpressionClause
                                => arrowExpressionClause,
                            SyntaxKind.Parameter when declarationSyntax is ParameterSyntax { Parent: ParameterListSyntax { Parent: RecordDeclarationSyntax } } recordParameter
                                => recordParameter,
                            SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration
                                when declarationSyntax is RecordDeclarationSyntax recordDeclaration
                                => recordDeclaration,
                            _ => throw new AssertionFailedException( $"Unexpected syntax for '{containingSemantic.Symbol}'." )
                        };

                    var resolvedReference =
                        new ResolvedAspectReference(
                            containingSemantic,
                            null,
                            contextSymbol,
                            targetSemantic,
                            explicitSemanticBody?.ToSemantic( IntermediateSymbolSemanticKind.Default ),
                            sourceNode,
                            sourceNode,
                            sourceNode,
                            targetKind,
                            isInlineable ?? true,
                            true,
                            isVirtual ?? false );

                    var referencesForContainingSemantic = (List<ResolvedAspectReference>) aspectReferences.GetOrAdd(
                        containingSemantic,
                        _ => new List<ResolvedAspectReference>() );

                    lock ( referencesForContainingSemantic )
                    {
                        referencesForContainingSemantic.Add( resolvedReference );
                    }

                    // In case of duplicate declarations (which can happen at design time), the aspect may not be added here.
                }
            }

            // Analyze introduced method bodies.
            var injectedMembers = this._injectionRegistry.GetInjectedMembers();
            await this._concurrentTaskRunner.RunConcurrentlyAsync( injectedMembers, ProcessInjectedMember, cancellationToken );

            void ProcessInjectedMember( InjectedMember injectedMember )
            {
                var symbol = this._injectionRegistry.GetSymbolForInjectedMember( injectedMember );

                switch ( symbol.Kind )
                {
                    case SymbolKind.Method when symbol is IMethodSymbol methodSymbol:
                        AnalyzeIntroducedBody( methodSymbol );

                        break;

                    case SymbolKind.Property when symbol is IPropertySymbol propertySymbol:
                        if ( propertySymbol.GetMethod != null )
                        {
                            AnalyzeIntroducedBody( propertySymbol.GetMethod );
                        }

                        if ( propertySymbol.SetMethod != null )
                        {
                            AnalyzeIntroducedBody( propertySymbol.SetMethod );
                        }

                        break;

                    case SymbolKind.Event when symbol is IEventSymbol eventSymbol:
                        AnalyzeIntroducedBody( eventSymbol.AddMethod.AssertNotNull() );
                        AnalyzeIntroducedBody( eventSymbol.RemoveMethod.AssertNotNull() );

                        break;

                    case SymbolKind.Field:
                        // NOP.
                        break;

                    case SymbolKind.NamedType:
                        // NOP.
                        break;

                    case SymbolKind.Namespace:
                        // NOP.
                        break;

                    default:
                        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                        throw new AssertionFailedException( $"Don't know how to process '{symbol}'." );
                }
            }

            return aspectReferences;

            void AnalyzeIntroducedBody( IMethodSymbol symbol )
            {
                var semantic = symbol.ToSemantic( IntermediateSymbolSemanticKind.Default );
                var syntax = symbol.GetPrimaryDeclarationSyntax().AssertNotNull();

                var nodesWithAspectReference = syntax.GetAnnotatedNodes( AspectReferenceAnnotationExtensions.AnnotationKind );

                var nodesContainingAspectReferences = new HashSet<SyntaxNode>();

                foreach ( var nodeWithAspectReference in nodesWithAspectReference )
                {
                    var currentNode = nodeWithAspectReference.Parent.AssertNotNull();

                    while ( currentNode != syntax.Parent )
                    {
                        nodesContainingAspectReferences.Add( currentNode );

                        currentNode = currentNode.Parent.AssertNotNull();
                    }
                }

                var aspectReferenceWalker = new AspectReferenceWalker(
                    this._referenceResolver,
                    this._semanticModelProvider.GetSemanticModel( syntax.SyntaxTree ),
                    symbol,
                    nodesContainingAspectReferences );

                aspectReferenceWalker.Visit( syntax );

                var existingReferences = (List<ResolvedAspectReference>) aspectReferences.GetOrAdd( semantic, _ => new List<ResolvedAspectReference>() );

                existingReferences.AddRange( aspectReferenceWalker.AspectReferences );
            }
        }
    }
}