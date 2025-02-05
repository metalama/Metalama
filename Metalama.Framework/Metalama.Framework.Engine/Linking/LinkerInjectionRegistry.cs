// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Stores information about injections and intermediate compilation.
/// </summary>
internal sealed class LinkerInjectionRegistry
{
    private readonly TransformationLinkerOrderComparer _comparer;
    private readonly PartialCompilation _intermediateCompilation;
    private readonly IReadOnlyDictionary<SyntaxTree, SyntaxTree> _transformedSyntaxTreeMap;
    private readonly IReadOnlyList<InjectedMember> _injectedMembers;
    private readonly IReadOnlyCollection<ISymbol> _overrideTargets;
    private readonly IReadOnlyDictionary<DeclarationBuilderData, IIntroduceDeclarationTransformation> _builderToTransformationMap;
    private readonly IReadOnlyDictionary<ISymbol, IReadOnlyList<ISymbol>> _overrideTargetToOverrideListMap;
    private readonly IReadOnlyDictionary<ISymbol, InjectedMember> _symbolToInjectedMemberMap;
    private readonly IReadOnlyDictionary<InjectedMember, ISymbol> _injectedMemberToSymbolMap;
    private readonly IReadOnlyDictionary<ISymbol, ISymbol> _overrideToOverrideTargetMap;
    private readonly ISet<ISymbol> _auxiliarySourceMembers;

    // TODO: This is used only for mapping of constructors with introduced parameters (limitation of code model).
    private readonly IReadOnlyDictionary<IRef<IDeclaration>, IReadOnlyList<IntroduceParameterTransformation>> _introducedParametersByTargetDeclaration;

    public LinkerInjectionRegistry(
        TransformationLinkerOrderComparer comparer,
        PartialCompilation intermediateCompilation,
        IEnumerable<SyntaxTreeTransformation> transformations,
        IEnumerable<InjectedMember> injectedMembers,
        IReadOnlyDictionary<DeclarationBuilderData, IIntroduceDeclarationTransformation> builderToTransformationMap,
        IReadOnlyDictionary<IRef<IDeclaration>, IReadOnlyList<IntroduceParameterTransformation>> introducedParametersByTargetDeclaration,
        ISet<ITransformation> transformationsCausingAuxiliaryOverrides,
        IConcurrentTaskRunner concurrentTaskRunner,
        CancellationToken cancellationToken )
    {
        ConcurrentQueue<ISymbol> overrideTargets;
        ConcurrentDictionary<ISymbol, IReadOnlyList<ISymbol>> overrideMap;
        ConcurrentDictionary<ISymbol, ISymbol> overrideTargetMap;
        ConcurrentDictionary<ISymbol, InjectedMember> symbolToInjectedMemberMap;
        ConcurrentDictionary<InjectedMember, ISymbol> injectedMemberToSymbolMap;
        HashSet<ISymbol> auxiliarySourceMembers;

        this._comparer = comparer;
        this._intermediateCompilation = intermediateCompilation;

        this._transformedSyntaxTreeMap = transformations
            .Where( m => m.Kind == SyntaxTreeTransformationKind.Replace )
            .ToDictionary( m => m.OldTree.AssertNotNull(), m => m.NewTree.AssertNotNull() );

        this._injectedMembers = injectedMembers.ToReadOnlyList();
        this._builderToTransformationMap = builderToTransformationMap;
        this._introducedParametersByTargetDeclaration = introducedParametersByTargetDeclaration;

        this._overrideTargets = overrideTargets = new ConcurrentQueue<ISymbol>();

        var auxiliarySourceMemberMap = new ConcurrentDictionary<ISymbol, ISymbol>( intermediateCompilation.CompilationContext.SymbolComparer );

        this._auxiliarySourceMembers = auxiliarySourceMembers = new HashSet<ISymbol>( intermediateCompilation.CompilationContext.SymbolComparer );

        this._overrideTargetToOverrideListMap = overrideMap =
            new ConcurrentDictionary<ISymbol, IReadOnlyList<ISymbol>>( intermediateCompilation.CompilationContext.SymbolComparer );

        this._overrideToOverrideTargetMap =
            overrideTargetMap = new ConcurrentDictionary<ISymbol, ISymbol>( intermediateCompilation.CompilationContext.SymbolComparer );

        this._symbolToInjectedMemberMap = symbolToInjectedMemberMap =
            new ConcurrentDictionary<ISymbol, InjectedMember>( intermediateCompilation.CompilationContext.SymbolComparer );

        this._injectedMemberToSymbolMap = injectedMemberToSymbolMap = new ConcurrentDictionary<InjectedMember, ISymbol>();

        var overriddenDeclarations = new ConcurrentDictionary<IFullRef<IDeclaration>, List<ISymbol>>( RefEqualityComparer<IDeclaration>.Default );
        var builderToInjectedMemberMap = new ConcurrentDictionary<DeclarationBuilderData, InjectedMember>();

        void ProcessInjectedMember( InjectedMember injectedMember )
        {
            var injectedMemberSymbol = GetCanonicalSymbolForInjectedMember( injectedMember );

            // Basic maps.
            symbolToInjectedMemberMap[injectedMemberSymbol] = injectedMember;
            injectedMemberToSymbolMap[injectedMember] = injectedMemberSymbol;

            if ( injectedMember.Transformation is IOverrideDeclarationTransformation overrideTransformation )
            {
                var list = overriddenDeclarations.GetOrAdd( overrideTransformation.OverriddenDeclaration, _ => new List<ISymbol>() );

                lock ( list )
                {
                    list.Add( injectedMemberSymbol );
                }
            }

            if ( injectedMember.Transformation != null && transformationsCausingAuxiliaryOverrides.Contains( injectedMember.Transformation ) )
            {
                var overriddenMember = injectedMember.Transformation switch
                {
                    IInsertStatementTransformation insertMember => insertMember.TargetMember,
                    _ => throw new AssertionFailedException( $"Unsupported transformation {injectedMember.Transformation}." )
                };

                var typeMember = overriddenMember.GetTypeMember();

                // These are auxiliary overrides created as a result of another transformation.
                var list = overriddenDeclarations.GetOrAdd( typeMember, _ => new List<ISymbol>() );

                lock ( list )
                {
                    list.Add( injectedMemberSymbol );
                }
            }

            if ( injectedMember.Transformation is IIntroduceDeclarationTransformation introduceTransformation )
            {
                builderToInjectedMemberMap.TryAdd( introduceTransformation.DeclarationBuilderData, injectedMember );
            }

            if ( injectedMember is { Transformation: null, Semantic: InjectedMemberSemantic.AuxiliaryBody } )
            {
                var originalDeclaration = (ISymbolRef<IDeclaration>) injectedMember.Declaration;
                ISymbol translatedSymbol;

                if ( this._introducedParametersByTargetDeclaration.TryGetValue( originalDeclaration, out var introducedParameters ) )
                {
                    // Constructors with introduced parameters cannot be found through normal translation.
                    // TODO: This is a hack, constructors with introduced parameters should be identifiable through code model.
                    Invariant.Assert( originalDeclaration is ISymbolRef<IConstructor> );

                    var originalConstructor = (IMethodSymbol) originalDeclaration.Symbol;

                    translatedSymbol =
                        TranslateConstructor( originalConstructor, introducedParameters )
                        ?? throw new AssertionFailedException(
                            $"Could not translate '{originalDeclaration}' with {introducedParameters.Count} introduced parameters." );
                }
                else
                {
                    translatedSymbol = intermediateCompilation.CompilationContext.SymbolTranslator.Translate( originalDeclaration.Symbol )
                        .AssertNotNull();
                }

                auxiliarySourceMemberMap[translatedSymbol] = injectedMemberSymbol;

                lock ( auxiliarySourceMembers )
                {
                    auxiliarySourceMembers.Add( injectedMemberSymbol );
                }
            }
        }

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        concurrentTaskRunner.RunConcurrentlyAsync( this._injectedMembers, ProcessInjectedMember, cancellationToken ).Wait( cancellationToken );
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

        void ProcessOverride( KeyValuePair<IFullRef<IDeclaration>, List<ISymbol>> value )
        {
            var declaration = value.Key;
            var overrides = value.Value;

            // Overrides are only supported for root type members, i.e. not for accessors.
            Invariant.Assert( declaration is not { ContainingDeclaration: IRef<IMember> } );

            var overrideTargetSymbol = GetOverrideTargetSymbol( declaration ).AssertNotNull();

            overrides.Sort( ( x, y ) => this._comparer.Compare( symbolToInjectedMemberMap[x].Transformation, symbolToInjectedMemberMap[y].Transformation ) );

            overrideTargets.Enqueue( overrideTargetSymbol );
            overrideMap.TryAdd( overrideTargetSymbol, overrides );

            foreach ( var overrideSymbol in overrides )
            {
                overrideTargetMap.TryAdd( overrideSymbol, overrideTargetSymbol );
            }
        }

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        concurrentTaskRunner.RunConcurrentlyAsync( overriddenDeclarations, ProcessOverride, cancellationToken ).Wait( cancellationToken );
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

        ISymbol GetCanonicalSymbolForInjectedMember( InjectedMember injectedMember )
        {
            var intermediateSyntaxTree = this._transformedSyntaxTreeMap[injectedMember.GetTargetSyntaxTree()];
            var intermediateSyntax = intermediateSyntaxTree.GetRoot().GetCurrentNode( injectedMember.Syntax ).AssertNotNull();

            SyntaxNode symbolSyntax = intermediateSyntax switch
            {
                EventFieldDeclarationSyntax eventFieldSyntax => eventFieldSyntax.Declaration.Variables.First(),
                FieldDeclarationSyntax fieldSyntax => fieldSyntax.Declaration.Variables.First(),
                _ => intermediateSyntax
            };

            return
                this._intermediateCompilation.CompilationContext.SemanticModelProvider.GetSemanticModel( intermediateSyntaxTree )
                    .GetDeclaredSymbol( symbolSyntax )
                    .AssertNotNull()
                    .GetCanonicalDefinition();
        }

        ISymbol? GetOverrideTargetSymbol( IRef<IDeclaration> overrideTarget )
        {
            switch ( overrideTarget )
            {
                case ISymbolRef symbolRef when this._introducedParametersByTargetDeclaration.TryGetValue( overrideTarget, out var introducedParameters ):
                    {
                        // Constructors with introduced parameters cannot be found through normal translation.
                        // TODO: This is a hack, constructors with introduced parameters should be identifiable through code model.
                        Invariant.Assert( overrideTarget is IRef<IConstructor> );

                        var originalConstructor = (IMethodSymbol) symbolRef.Symbol;

                        var translatedConstructor =
                            TranslateConstructor( originalConstructor, introducedParameters )
                            ?? throw new AssertionFailedException(
                                $"Could not translate '{overrideTarget}' with {introducedParameters.Count} introduced parameters." );

                        return translatedConstructor;
                    }

                case ISymbolRef symbolRef:
                    {
                        var symbol =
                            this._intermediateCompilation.CompilationContext.SymbolTranslator.Translate(
                                    symbolRef.Symbol.GetCanonicalDefinition().AssertNotNull(),
                                    true )
                                .AssertNotNull();

                        if ( auxiliarySourceMemberMap.TryGetValue( symbol, out var auxiliarySourceMemberSymbol ) )
                        {
                            return auxiliarySourceMemberSymbol;
                        }
                        else
                        {
                            return symbol;
                        }
                    }

                case IIntroducedRef builtMember:
                    return GetFromBuilder( builtMember.BuilderData );

                default:
                    throw new AssertionFailedException( $"Unexpected declaration: '{overrideTarget}'." );
            }

            ISymbol? GetFromBuilder( DeclarationBuilderData builder )
            {
                var introducedBuilder = builderToInjectedMemberMap[builder];
                var sourceSyntaxTree = builder.PrimarySyntaxTree.AssertNotNull();
                var intermediateSyntaxTree = this._transformedSyntaxTreeMap[sourceSyntaxTree];
                var intermediateNode = intermediateSyntaxTree.GetRoot().GetCurrentNode( introducedBuilder.Syntax );

                var intermediateSemanticModel =
                    this._intermediateCompilation.CompilationContext.SemanticModelProvider.GetSemanticModel( intermediateSyntaxTree );

                var symbolNode = intermediateNode.AssertNotNull() switch
                {
                    EventFieldDeclarationSyntax eventFieldNode => (SyntaxNode) eventFieldNode.Declaration.Variables.First(),
                    _ => intermediateNode
                };

                return intermediateSemanticModel.GetDeclaredSymbol( symbolNode ).GetCanonicalDefinition();
            }
        }

        IMethodSymbol? TranslateConstructor( IMethodSymbol originalConstructor, IReadOnlyList<IntroduceParameterTransformation> introducedParameters )
        {
            Invariant.Assert( originalConstructor is { MethodKind: MethodKind.Constructor } );

            var originalDeclaringType = originalConstructor.ContainingType;

            var translatedDeclaringType = this._intermediateCompilation.CompilationContext.SymbolTranslator.Translate( originalDeclaringType )
                .AssertNotNull();

            foreach ( var translatedMember in translatedDeclaringType.GetMembers() )
            {
                if ( translatedMember is not IMethodSymbol { MethodKind: MethodKind.Constructor } translatedConstructor )
                {
                    continue;
                }

                if ( translatedConstructor.Parameters.Length != originalConstructor.Parameters.Length + introducedParameters.Count )
                {
                    continue;
                }

                if ( symbolToInjectedMemberMap.ContainsKey( translatedMember ) )
                {
                    continue;
                }

                var matches = true;

                for ( var i = 0; i < originalConstructor.Parameters.Length; i++ )
                {
                    if ( !StructuralSymbolComparer.Default.Equals(
                             originalConstructor.Parameters[i].Type,
                             translatedConstructor.Parameters[i].Type )
                         || originalConstructor.Parameters[i].RefKind != translatedConstructor.Parameters[i].RefKind )
                    {
                        matches = false;

                        break;
                    }
                }

                if ( matches )
                {
                    if ( auxiliarySourceMemberMap.TryGetValue( translatedConstructor, out var auxiliarySourceMemberSymbol ) )
                    {
                        return (IMethodSymbol) auxiliarySourceMemberSymbol;
                    }
                    else
                    {
                        return translatedConstructor;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Gets introduced members representing overrides of a symbol.
    /// </summary>
    /// <param name="referencedSymbol">Symbol.</param>
    /// <returns>List of introduced members.</returns>
    public IReadOnlyList<ISymbol> GetOverridesForSymbol( ISymbol referencedSymbol )
    {
        referencedSymbol = referencedSymbol.GetCanonicalDefinition();

        if ( this._overrideTargetToOverrideListMap.TryGetValue( referencedSymbol, out var overrideSymbolList ) )
        {
            return overrideSymbolList;
        }
        else
        {
            return Array.Empty<ISymbol>();
        }
    }

    /// <summary>
    /// Gets an introduced member represented the declaration that resulted in the specified symbol in the intermediate compilation.
    /// </summary>
    /// <param name="symbol">Symbol.</param>
    /// <returns>An introduced member, or <c>null</c> if the declaration represented by this symbol was not introduced.</returns>
    public InjectedMember? GetInjectedMemberForSymbol( ISymbol symbol )
    {
        switch ( symbol )
        {
            case IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet } propertyAccessorSymbol:
                // Coverage: ignore (coverage is irrelevant, needed for correctness).
                return this.GetInjectedMemberForSymbol( propertyAccessorSymbol.AssociatedSymbol.AssertNotNull() );

            case IMethodSymbol { MethodKind: MethodKind.EventAdd or MethodKind.EventRemove } eventAccessorSymbol:
                // Coverage: ignore (coverage is irrelevant, needed for correctness).
                return this.GetInjectedMemberForSymbol( eventAccessorSymbol.AssociatedSymbol.AssertNotNull() );
        }

        if ( this._symbolToInjectedMemberMap.TryGetValue( symbol, out var injectedMember ) )
        {
            return injectedMember;
        }
        else
        {
            return null;
        }
    }

    public IIntroduceDeclarationTransformation? GetTransformationForBuilder( DeclarationBuilderData builder )
    {
        if ( this._builderToTransformationMap.TryGetValue( builder, out var transformation ) )
        {
            // Builder that was removed.
            return transformation;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a symbol in intermediate compilation that represents a declaration introduced by the introduced member.
    /// </summary>
    /// <param name="injectedMember"></param>
    /// <returns></returns>
    public ISymbol GetSymbolForInjectedMember( InjectedMember injectedMember ) => this._injectedMemberToSymbolMap[injectedMember];

    /// <summary>
    /// Gets introduced members for all transformations.
    /// </summary>
    /// <returns>Enumeration of introduced members.</returns>
    public IEnumerable<InjectedMember> GetInjectedMembers() => this._injectedMembers;

    /// <summary>
    /// Gets all symbols for overridden members.
    /// </summary>
    /// <returns>Enumeration of symbols.</returns>
    public IReadOnlyCollection<ISymbol> GetOverriddenMembers() => this._overrideTargets;

    public ISymbol? GetOverrideTarget( ISymbol overrideSymbol )
    {
        overrideSymbol = overrideSymbol.GetCanonicalDefinition();

        if ( this._overrideToOverrideTargetMap.TryGetValue( overrideSymbol, out var overrideTargetSymbol ) )
        {
            return overrideTargetSymbol;
        }
        else
        {
            return null;
        }
    }

    public bool IsAuxiliarySourceSymbol( ISymbol symbol ) => this._auxiliarySourceMembers.Contains( symbol );

    /// <summary>
    /// Gets the last (outermost) override of the method.
    /// </summary>
    /// <param name="symbol">Method symbol.</param>
    /// <returns>Symbol.</returns>
    public ISymbol GetLastOverride( ISymbol symbol )
    {
        symbol = symbol.GetCanonicalDefinition();

        switch ( symbol )
        {
            case IMethodSymbol { MethodKind: MethodKind.PropertyGet } getterSymbol:
                return ((IPropertySymbol) this.GetLastOverride( getterSymbol.AssociatedSymbol.AssertNotNull() )).GetMethod.AssertNotNull();

            case IMethodSymbol { MethodKind: MethodKind.PropertySet } setterSymbol:
                return ((IPropertySymbol) this.GetLastOverride( setterSymbol.AssociatedSymbol.AssertNotNull() )).SetMethod.AssertNotNull();

            case IMethodSymbol { MethodKind: MethodKind.EventAdd } adderSymbol:
                return ((IEventSymbol) this.GetLastOverride( adderSymbol.AssociatedSymbol.AssertNotNull() )).AddMethod.AssertNotNull();

            case IMethodSymbol { MethodKind: MethodKind.EventRemove } removerSymbol:
                return ((IEventSymbol) this.GetLastOverride( removerSymbol.AssociatedSymbol.AssertNotNull() )).RemoveMethod.AssertNotNull();

            default:
                var overrides = this.GetOverridesForSymbol( symbol );
                var lastOverride = overrides.Count > 0 ? overrides[^1] : null;

                if ( lastOverride == null )
                {
                    // Coverage: ignore (coverage is irrelevant, needed for correctness).
                    return symbol;
                }

                return lastOverride;
        }
    }

    /// <summary>
    /// Gets the last (outermost) override of the method.
    /// </summary>
    /// <param name="symbol">Method symbol.</param>
    /// <returns>Symbol.</returns>
    public IMethodSymbol GetLastOverride( IMethodSymbol symbol ) => (IMethodSymbol) this.GetLastOverride( (ISymbol) symbol );

    /// <summary>
    /// Determines whether the symbol was introduced.
    /// </summary>
    /// <param name="symbol">Symbol.</param>
    /// <returns><c>True</c> if the method is introduced, otherwise <c>false</c>.</returns>
    public bool IsIntroduced( ISymbol symbol )
    {
        if ( symbol is IMethodSymbol
            {
                MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove
            } methodSymbol )
        {
            return this.IsIntroduced( methodSymbol.AssociatedSymbol.AssertNotNull() );
        }

        symbol = symbol.GetCanonicalDefinition();

        var injectedMember = this.GetInjectedMemberForSymbol( symbol );

        if ( injectedMember == null )
        {
            return false;
        }

        return injectedMember.Semantic == InjectedMemberSemantic.Introduction;
    }

    /// <summary>
    /// Determines whether the symbol represents override target.
    /// </summary>
    /// <param name="symbol">Symbol.</param>
    /// <returns><c>True</c> if the method is override target, otherwise <c>false</c>.</returns>
    public bool IsOverrideTarget( ISymbol symbol )
    {
        if ( symbol is IMethodSymbol
            {
                MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove
            } methodSymbol )
        {
            return this.IsOverrideTarget( methodSymbol.AssociatedSymbol.AssertNotNull() );
        }

        symbol = symbol.GetCanonicalDefinition();

        return this._overrideTargetToOverrideListMap.ContainsKey( symbol );
    }

    /// <summary>
    /// Determines whether the symbol represents an override method.
    /// </summary>
    /// <param name="symbol">Symbol.</param>
    /// <returns></returns>
    public bool IsOverride( ISymbol symbol )
    {
        if ( symbol is IMethodSymbol
            {
                MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove
            } methodSymbol )
        {
            return this.IsOverride( methodSymbol.AssociatedSymbol.AssertNotNull() );
        }

        symbol = symbol.GetCanonicalDefinition();

        return this._overrideToOverrideTargetMap.ContainsKey( symbol );
    }

    // Resharper disable once UnusedMember.Global
    public bool IsLastOverride( ISymbol symbol )
    {
        symbol = symbol.GetCanonicalDefinition();

        return
            this.IsOverride( symbol ) &&
            this._intermediateCompilation.CompilationContext.SymbolComparer.Equals(
                symbol,
                this.GetLastOverride( this.GetOverrideTarget( symbol ).AssertNotNull() ) );
    }

    public IAspectClass? GetSourceAspect( ISymbol symbol )
    {
        symbol = symbol.GetCanonicalDefinition();

        var injectedMember = this.GetInjectedMemberForSymbol( symbol );

        return injectedMember?.Transformation?.AspectInstance.AspectClass;
    }
}