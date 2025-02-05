// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using INamedType = Metalama.Framework.Code.INamedType;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerInjectionStep
{
    /// <summary>
    /// Mutable collection of data representing processed transformations. Used during rewriting and when creating injection registry.
    /// </summary>
    private sealed class TransformationCollection
    {
        private readonly CompilationModel _finalCompilationModel;
        private readonly TransformationLinkerOrderComparer _comparer;
        private readonly ConcurrentQueue<InjectedMember> _injectedMembers;
        private readonly ConcurrentDictionary<InsertPosition, List<InjectedMember>> _injectedMembersByInsertPosition;
        private readonly ConcurrentDictionary<BaseTypeDeclarationSyntax, List<LinkerInjectedInterface>> _injectedInterfacesByTargetTypeDeclaration;
        private readonly ConcurrentDictionary<NamedTypeBuilderData, List<LinkerInjectedInterface>> _injectedInterfacesByTargetTypeBuilder;
        private readonly HashSet<VariableDeclaratorSyntax> _removedVariableDeclaratorSyntax;
        private readonly HashSet<PropertyDeclarationSyntax> _autoPropertyWithSynthesizedSetterSyntax;
        private readonly HashSet<PropertyBuilderData> _autoPropertyWithSynthesizedSetterBuilders;
        private readonly ConcurrentDictionary<PropertyDeclarationSyntax, List<AspectLinkerDeclarationFlags>> _additionalDeclarationFlags;
        private readonly HashSet<SyntaxNode> _nodesWithModifiedAttributes;
        private readonly ConcurrentDictionary<SyntaxNode, MemberLevelTransformations> _symbolMemberLevelTransformations;
        private readonly ConcurrentDictionary<DeclarationBuilderData, MemberLevelTransformations> _introductionMemberLevelTransformations;
        private readonly ConcurrentDictionary<DeclarationBuilderData, IIntroduceDeclarationTransformation> _builderToTransformationMap;

        private readonly ConcurrentDictionary<IRef<IMethodBase>, List<InsertedStatement>> _insertedStatementsByTargetMethodBase;

        private readonly ConcurrentDictionary<IRef<IDeclaration>, List<InjectedMember>> _injectedMembersByTargetDeclaration;
        private readonly ConcurrentDictionary<IRef<IDeclaration>, IReadOnlyList<IntroduceParameterTransformation>> _introducedParametersByTargetDeclaration;

        private readonly ConcurrentDictionary<ISymbolRef<INamedType>, LateTypeLevelTransformations> _lateTypeLevelTransformations;

        private readonly HashSet<ITransformation> _transformationsCausingAuxiliaryOverrides;

        private readonly Dictionary<string, SyntaxTree> _introducedSyntaxTrees;
        private readonly InjectedMemberComparer _injectedMemberComparer;

        public IReadOnlyCollection<InjectedMember> InjectedMembers => this._injectedMembers;

        public IReadOnlyDictionary<DeclarationBuilderData, IIntroduceDeclarationTransformation> BuilderToTransformationMap => this._builderToTransformationMap;

        public IReadOnlyDictionary<IRef<IDeclaration>, IReadOnlyList<IntroduceParameterTransformation>> IntroducedParametersByTargetDeclaration
            => this._introducedParametersByTargetDeclaration;

        public IReadOnlyDictionary<ISymbolRef<INamedType>, LateTypeLevelTransformations> LateTypeLevelTransformations => this._lateTypeLevelTransformations;

        // ReSharper disable once InconsistentlySynchronizedField
        public ISet<ITransformation> TransformationsCausingAuxiliaryOverrides => this._transformationsCausingAuxiliaryOverrides;

        // ReSharper disable once InconsistentlySynchronizedField
        public IReadOnlyCollection<SyntaxTree> IntroducedSyntaxTrees => this._introducedSyntaxTrees.Values;

        public TransformationCollection( CompilationModel finalCompilationModel, TransformationLinkerOrderComparer comparer )
        {
            this._finalCompilationModel = finalCompilationModel;
            this._comparer = comparer;
            this._injectedMembers = new ConcurrentQueue<InjectedMember>();
            this._injectedMembersByInsertPosition = new ConcurrentDictionary<InsertPosition, List<InjectedMember>>();
            this._injectedInterfacesByTargetTypeDeclaration = new ConcurrentDictionary<BaseTypeDeclarationSyntax, List<LinkerInjectedInterface>>();
            this._injectedInterfacesByTargetTypeBuilder = new ConcurrentDictionary<NamedTypeBuilderData, List<LinkerInjectedInterface>>();
            this._removedVariableDeclaratorSyntax = [];
            this._autoPropertyWithSynthesizedSetterSyntax = [];
            this._autoPropertyWithSynthesizedSetterBuilders = [];
            this._additionalDeclarationFlags = new ConcurrentDictionary<PropertyDeclarationSyntax, List<AspectLinkerDeclarationFlags>>();
            this._nodesWithModifiedAttributes = [];
            this._symbolMemberLevelTransformations = new ConcurrentDictionary<SyntaxNode, MemberLevelTransformations>();
            this._introductionMemberLevelTransformations = new ConcurrentDictionary<DeclarationBuilderData, MemberLevelTransformations>();
            this._builderToTransformationMap = new ConcurrentDictionary<DeclarationBuilderData, IIntroduceDeclarationTransformation>();

            this._insertedStatementsByTargetMethodBase =
                new ConcurrentDictionary<IRef<IMethodBase>, List<InsertedStatement>>( RefEqualityComparer<IMethodBase>.Default );

            this._injectedMembersByTargetDeclaration =
                new ConcurrentDictionary<IRef<IDeclaration>, List<InjectedMember>>( RefEqualityComparer<IDeclaration>.Default );

            this._introducedParametersByTargetDeclaration =
                new ConcurrentDictionary<IRef<IDeclaration>, IReadOnlyList<IntroduceParameterTransformation>>( RefEqualityComparer<IDeclaration>.Default );

            this._lateTypeLevelTransformations =
                new ConcurrentDictionary<ISymbolRef<INamedType>, LateTypeLevelTransformations>( RefEqualityComparer<INamedType>.Default );

            this._transformationsCausingAuxiliaryOverrides = [];
            this._introducedSyntaxTrees = [];
            this._injectedMemberComparer = new InjectedMemberComparer();
        }

        public void AddInjectedMember( InjectedMember injectedMember )
            => this.AddInjectedMember( injectedMember.Declaration.ToInsertPosition(), injectedMember );

        public void AddInjectedMembers( IInjectMemberTransformation injectMemberTransformation, IEnumerable<InjectedMember> injectedMembers )
        {
            foreach ( var injectedMember in injectedMembers )
            {
                this.AddInjectedMember( injectMemberTransformation.InsertPosition, injectedMember );
            }
        }

        private void AddInjectedMember( InsertPosition insertPosition, InjectedMember injectedMember )
        {
            // Injected member should always be root type member (not an accessor).
            Invariant.Assert( injectedMember.Declaration is not { ContainingDeclaration: IRef<IMember> } );

            this._injectedMembers.Enqueue( injectedMember );

            var nodes = this._injectedMembersByInsertPosition.GetOrAdd( insertPosition, _ => [] );

            lock ( nodes )
            {
                nodes.Add( injectedMember );
            }

            var declarationInjectedMembers =
                this._injectedMembersByTargetDeclaration.GetOrAdd( injectedMember.Declaration, _ => new List<InjectedMember>() );

            lock ( declarationInjectedMembers )
            {
                declarationInjectedMembers.Add( injectedMember );
            }
        }

        public void AddInjectedInterface( IRef<INamedType> targetType, LinkerInjectedInterface injectedInterface )
        {
            switch ( targetType )
            {
                case ISymbolRef symbolRef:
                    this.AddInjectedInterface( (BaseTypeDeclarationSyntax) symbolRef.Symbol.GetPrimaryDeclarationSyntax().AssertNotNull(), injectedInterface );

                    break;

                case IIntroducedRef builtDeclarationRef:
                    this.AddInjectedInterface( (NamedTypeBuilderData) builtDeclarationRef.BuilderData, injectedInterface );

                    break;

                default:
                    throw new AssertionFailedException();
            }
        }

        private void AddInjectedInterface( BaseTypeDeclarationSyntax targetType, LinkerInjectedInterface injectedInterface )
        {
            var interfaceList =
                this._injectedInterfacesByTargetTypeDeclaration.GetOrAdd(
                    targetType,
                    _ => [] );

            lock ( interfaceList )
            {
                interfaceList.Add( injectedInterface );
            }
        }

        private void AddInjectedInterface( NamedTypeBuilderData targetTypeBuilder, LinkerInjectedInterface injectedInterface )
        {
            var interfaceList =
                this._injectedInterfacesByTargetTypeBuilder.GetOrAdd(
                    targetTypeBuilder,
                    _ => [] );

            lock ( interfaceList )
            {
                interfaceList.Add( injectedInterface );
            }
        }

        public void AddAutoPropertyWithSynthesizedSetter( IRef<IProperty> declaration )
        {
            switch ( declaration )
            {
                case ISymbolRef codeProperty:
                    this.AddAutoPropertyWithSynthesizedSetter( (PropertyDeclarationSyntax) codeProperty.Symbol.GetPrimaryDeclarationSyntax().AssertNotNull() );

                    break;

                case IIntroducedRef builtDeclarationRef:
                    this.AddAutoPropertyWithSynthesizedSetter( (PropertyBuilderData) builtDeclarationRef.BuilderData );

                    break;

                default:
                    throw new AssertionFailedException( $"Unexpected declaration: '{declaration}'." );
            }
        }

        private void AddAutoPropertyWithSynthesizedSetter( PropertyDeclarationSyntax declaration )
        {
            Invariant.Assert( declaration.IsAutoPropertyDeclaration() && !declaration.HasSetterAccessorDeclaration() );

            lock ( this._autoPropertyWithSynthesizedSetterSyntax )
            {
                this._autoPropertyWithSynthesizedSetterSyntax.Add( declaration );
            }
        }

        private void AddAutoPropertyWithSynthesizedSetter( PropertyBuilderData property )
        {
            Invariant.Assert( property is { IsAutoPropertyOrField: true, Writeability: Writeability.ConstructorOnly } );

            lock ( this._autoPropertyWithSynthesizedSetterBuilders )
            {
                this._autoPropertyWithSynthesizedSetterBuilders.Add( property );
            }
        }

        // ReSharper disable once UnusedMember.Local
        public void AddDeclarationWithAdditionalFlags( PropertyDeclarationSyntax declaration, AspectLinkerDeclarationFlags flags )
        {
            var list = this._additionalDeclarationFlags.GetOrAdd( declaration, _ => [] );

            lock ( list )
            {
                list.Add( flags );
            }
        }

        public void AddInsertedStatements( IRef<IMethodBase> targetMethod, IEnumerable<InsertedStatement> statements )
        {
            // PERF: Synchronization should not be needed because we are in the same syntax tree (if not, this would be non-deterministic and thus wrong).
            //       Assertions should be added first.
            var statementList = this._insertedStatementsByTargetMethodBase.GetOrAdd( targetMethod, _ => [] );

            lock ( statementList )
            {
                statementList.AddRange( statements );
            }
        }

        public void AddRemovedSyntax( SyntaxNode removedSyntax )
        {
            switch ( removedSyntax )
            {
                case VariableDeclaratorSyntax variableDeclarator:
                    lock ( this._removedVariableDeclaratorSyntax )
                    {
                        this._removedVariableDeclaratorSyntax.Add( variableDeclarator );
                    }

                    break;

                default:
                    throw new AssertionFailedException( $"Not supported removed syntax: {removedSyntax}" );
            }
        }

        public void AddNodeWithModifiedAttributes( SyntaxNode node )
        {
            lock ( this._nodesWithModifiedAttributes )
            {
                this._nodesWithModifiedAttributes.Add( node );
            }
        }

        internal void AddIntroducedParameter( IntroduceParameterTransformation introduceParameterTransformation )
        {
            var parameterList = this._introducedParametersByTargetDeclaration.GetOrAdd(
                introduceParameterTransformation.TargetDeclaration,
                _ => new List<IntroduceParameterTransformation>() );

            lock ( parameterList )
            {
                ((List<IntroduceParameterTransformation>) parameterList).Add( introduceParameterTransformation );
            }
        }

        // ReSharper disable once InconsistentlySynchronizedField
        public bool IsRemovedSyntax( VariableDeclaratorSyntax variableDeclarator ) => this._removedVariableDeclaratorSyntax.Contains( variableDeclarator );

        // ReSharper disable once InconsistentlySynchronizedField
        public bool IsAutoPropertyWithSynthesizedSetter( PropertyDeclarationSyntax propertyDeclaration )
            => this._autoPropertyWithSynthesizedSetterSyntax.Contains( propertyDeclaration );

        // ReSharper disable once InconsistentlySynchronizedField
        public bool IsAutoPropertyWithSynthesizedSetter( PropertyBuilderData propertyBuilder )
            => this._autoPropertyWithSynthesizedSetterBuilders.Contains( propertyBuilder );

        // ReSharper disable once InconsistentlySynchronizedField
        public bool IsNodeWithModifiedAttributes( SyntaxNode node ) => this._nodesWithModifiedAttributes.Contains( node );

        public IEnumerable<InjectedMember> GetInjectedMembersOnPosition( InsertPosition position )
        {
            if ( this._injectedMembersByInsertPosition.TryGetValue( position, out var injectedMembers ) )
            {
                // IMPORTANT - do not change the introduced node here.
                injectedMembers.Sort( this._injectedMemberComparer );

                return injectedMembers;
            }

            return Array.Empty<InjectedMember>();
        }

        public IReadOnlyList<LinkerInjectedInterface> GetIntroducedInterfacesForTypeDeclaration( BaseTypeDeclarationSyntax typeDeclaration )
        {
            if ( this._injectedInterfacesByTargetTypeDeclaration.TryGetValue( typeDeclaration, out var interfaceList ) )
            {
                interfaceList.Sort( ( x, y ) => this._comparer.Compare( x.Transformation, y.Transformation ) );

                return interfaceList;
            }

            return Array.Empty<LinkerInjectedInterface>();
        }

        public IReadOnlyList<LinkerInjectedInterface> GetIntroducedInterfacesForTypeBuilder( NamedTypeBuilderData typeBuilder )
        {
            if ( this._injectedInterfacesByTargetTypeBuilder.TryGetValue( typeBuilder, out var interfaceList ) )
            {
                interfaceList.Sort( ( x, y ) => this._comparer.Compare( x.Transformation, y.Transformation ) );

                return interfaceList;
            }

            return Array.Empty<LinkerInjectedInterface>();
        }

        public AspectLinkerDeclarationFlags GetAdditionalDeclarationFlags( PropertyDeclarationSyntax declaration )
        {
            if ( this._additionalDeclarationFlags.TryGetValue( declaration, out var list ) )
            {
                var finalFlags = AspectLinkerDeclarationFlags.None;

                foreach ( var flags in list )
                {
                    finalFlags |= flags;
                }

                return finalFlags;
            }

            return AspectLinkerDeclarationFlags.None;
        }

        public bool TryGetMemberLevelTransformations( SyntaxNode node, [NotNullWhen( true )] out MemberLevelTransformations? memberLevelTransformations )
            => this._symbolMemberLevelTransformations.TryGetValue( node, out memberLevelTransformations );

        public bool TryGetMemberLevelTransformations(
            DeclarationBuilderData builder,
            [NotNullWhen( true )] out MemberLevelTransformations? memberLevelTransformations )
            => this._introductionMemberLevelTransformations.TryGetValue( builder, out memberLevelTransformations );

        public async Task FinalizeAsync(
            IConcurrentTaskRunner concurrentTaskRunner,
            CancellationToken cancellationToken )
        {
            await concurrentTaskRunner.RunConcurrentlyAsync(
                this._introductionMemberLevelTransformations.Values,
                t => t.Sort(),
                cancellationToken );

            await concurrentTaskRunner.RunConcurrentlyAsync(
                this._symbolMemberLevelTransformations.Values,
                t => t.Sort(),
                cancellationToken );
        }

        public void AddIntroduceTransformation(
            DeclarationBuilderData declarationBuilder,
            IIntroduceDeclarationTransformation introduceDeclarationTransformation )
        {
            var wasAdded = this._builderToTransformationMap.TryAdd( declarationBuilder, introduceDeclarationTransformation );

            Invariant.Assert( wasAdded );
        }

        public void AddTransformationCausingAuxiliaryOverride( ITransformation causalTransformation )
        {
            lock ( this._transformationsCausingAuxiliaryOverrides )
            {
                this._transformationsCausingAuxiliaryOverrides.Add( causalTransformation );
            }
        }

        public bool TryGetIntroduceDeclarationTransformation(
            DeclarationBuilderData replacedBuilder,
            [NotNullWhen( true )] out IIntroduceDeclarationTransformation? introduceDeclarationTransformation )
            => this._builderToTransformationMap.TryGetValue( replacedBuilder, out introduceDeclarationTransformation );

        public MemberLevelTransformations GetOrAddMemberLevelTransformations( IRef<IDeclaration> declaration )
            => declaration switch
            {
                ISymbolRef symbolRef when symbolRef.Symbol.GetPrimaryDeclarationSyntax() is { } syntax => this.GetOrAddMemberLevelTransformations( syntax ),
                IIntroducedRef builtDeclarationRef => this.GetOrAddMemberLevelTransformations( builtDeclarationRef.BuilderData ),
                _ when this._finalCompilationModel.TryGetRedirectedDeclaration( declaration, out var redirectedDeclaration ) => this
                    .GetOrAddMemberLevelTransformations( redirectedDeclaration ),
                _ => throw new AssertionFailedException()
            };

        private MemberLevelTransformations GetOrAddMemberLevelTransformations( SyntaxNode declarationSyntax )
            => this._symbolMemberLevelTransformations.GetOrAdd( declarationSyntax, static _ => new MemberLevelTransformations() );

        private MemberLevelTransformations GetOrAddMemberLevelTransformations( DeclarationBuilderData declarationBuilder )
            => this._introductionMemberLevelTransformations.GetOrAdd( declarationBuilder, static _ => new MemberLevelTransformations() );

        public LateTypeLevelTransformations GetOrAddLateTypeLevelTransformations( ISymbolRef<INamedType> type )
            => this._lateTypeLevelTransformations.GetOrAdd( type, static _ => new LateTypeLevelTransformations() );

        internal IReadOnlyList<StatementSyntax> GetInjectedEntryStatements( InjectedMember injectedMember )
        {
            var targetMethod = injectedMember.Declaration.As<IMethodBase>();

            return this.GetInjectedEntryStatements( targetMethod, targetMethod, injectedMember );
        }

        internal IReadOnlyList<StatementSyntax> GetInjectedEntryStatements( IRef<IMethodBase> targetMethod, InjectedMember? targetInjectedMember = null )
            => this.GetInjectedEntryStatements( targetMethod, targetMethod, targetInjectedMember );

        /// <param name="targetTypeMember">In case of accessors, the property or event.</param>
        internal IReadOnlyList<StatementSyntax> GetInjectedEntryStatements(
            IRef<IMethodBase> targetMethod,
            IRef<IMember> targetTypeMember,
            InjectedMember? targetInjectedMember = null )
        {
            // PERF: Iterating and reversing should be avoided.
            if ( !this._insertedStatementsByTargetMethodBase.TryGetValue( targetMethod, out var insertedStatements ) )
            {
                return ImmutableArray<StatementSyntax>.Empty;
            }

            bool hasInjectedMembers;
            MemberLayerIndex? bottomBound;
            MemberLayerIndex? topBound;

            // If trying to get inserted statements for a source declaration, we need to first find the first injected member.
            if ( !this._injectedMembersByTargetDeclaration.TryGetValue( targetTypeMember, out var injectedMembers ) )
            {
                hasInjectedMembers = false;

                if ( targetInjectedMember == null )
                {
                    bottomBound = null;
                    topBound = null;
                }
                else
                {
                    throw new AssertionFailedException( $"Missing injected member for {targetTypeMember}" );
                }
            }
            else
            {
                injectedMembers = injectedMembers.ToOrderedList( x => GetTransformationMemberLayerIndex( x.Transformation ) );

                hasInjectedMembers = true;

                if ( targetInjectedMember == null )
                {
                    bottomBound = null;
                    topBound = GetTransformationMemberLayerIndex( injectedMembers.First().Transformation );
                }
                else
                {
                    var targetInjectedMemberIndex = injectedMembers.IndexOf( targetInjectedMember );

                    if ( targetInjectedMemberIndex < 0 )
                    {
                        throw new AssertionFailedException( $"Missing injected members for {targetMethod}" );
                    }

                    bottomBound = GetTransformationMemberLayerIndex( targetInjectedMember.Transformation );

                    topBound =
                        targetInjectedMemberIndex >= injectedMembers.Count - 1
                            ? null
                            : GetTransformationMemberLayerIndex( injectedMembers[targetInjectedMemberIndex + 1].Transformation );
                }
            }

            var statements = new List<StatementSyntax>();

            if ( targetMethod is IRef<IConstructor> )
            {
                if ( (!hasInjectedMembers && targetInjectedMember == null) || (hasInjectedMembers && targetInjectedMember == injectedMembers![^1]) )
                {
                    // Return initializer statements source members with no overrides or to the last override.
                    var initializerStatements =
                        insertedStatements
                            .Where( s => s.Kind == InsertedStatementKind.Initializer )
                            .Select( s => s );

                    var orderedInitializerStatements = OrderInitializerStatements( initializerStatements );

                    statements.AddRange( orderedInitializerStatements.Select( s => s.Statement ) );
                }
            }

            // For non-initializer statements we have to select a range of statements that fits this injected member.
            var inputContractStatements =
                insertedStatements
                    .Where(
                        s =>
                            s.Kind == InsertedStatementKind.InputContract
                            && (bottomBound == null || GetTransformationMemberLayerIndex( s.Transformation ) >= bottomBound)
                            && (topBound == null || GetTransformationMemberLayerIndex( s.Transformation ) < topBound) );

            var orderedInputContractStatements = OrderInputContractStatements( inputContractStatements );

            statements.AddRange(
                orderedInputContractStatements.Select(
                    s =>
                        s.Statement switch
                        {
                            BlockSyntax block => block.WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                            _ => s.Statement
                        } ) );

            return statements;
        }

        internal IReadOnlyList<StatementSyntax> GetInjectedExitStatements( InjectedMember injectedMember )
        {
            var targetMethod = injectedMember.Declaration.As<IMethodBase>();

            return this.GetInjectedExitStatements( targetMethod, targetMethod, injectedMember );
        }

        internal IReadOnlyList<StatementSyntax> GetInjectedExitStatements(
            IRef<IMethodBase> targetMethod,
            IRef<IMember> targetTypeMember,
            InjectedMember targetInjectedMember )
        {
            // PERF: Iterating and reversing should be avoided.
            if ( !this._insertedStatementsByTargetMethodBase.TryGetValue( targetMethod, out var insertedStatements ) )
            {
                return ImmutableArray<StatementSyntax>.Empty;
            }

            MemberLayerIndex bottomBound;
            MemberLayerIndex? topBound;

            // If trying to get inserted statements for a source declaration, we need to first find the first injected member.
            if ( !this._injectedMembersByTargetDeclaration.TryGetValue( targetTypeMember, out var injectedMembers ) )
            {
                throw new AssertionFailedException( $"Missing injected member for {targetMethod} (exit statements are not supported on source members)." );
            }
            else
            {
                injectedMembers = injectedMembers.ToOrderedList( x => GetTransformationMemberLayerIndex( x.Transformation ) );

                var targetInjectedMemberIndex = injectedMembers.IndexOf( targetInjectedMember );

                if ( targetInjectedMemberIndex < 0 )
                {
                    throw new AssertionFailedException( $"Missing injected members for {targetMethod}" );
                }

                bottomBound = GetTransformationMemberLayerIndex( targetInjectedMember.Transformation );

                topBound =
                    targetInjectedMemberIndex >= injectedMembers.Count - 1
                        ? null
                        : GetTransformationMemberLayerIndex( injectedMembers[targetInjectedMemberIndex + 1].Transformation );
            }

            var statements = new List<StatementSyntax>();

            // For non-initializer statements we have to select a range of statements that fits this injected member.
            var outputContractStatements =
                insertedStatements
                    .Where(
                        s =>
                            s.Kind == InsertedStatementKind.OutputContract
                            && GetTransformationMemberLayerIndex( s.Transformation ) >= bottomBound
                            && (topBound == null || GetTransformationMemberLayerIndex( s.Transformation ) < topBound) );

            var orderedOutputContractStatements = OrderOutputContractStatements( outputContractStatements );

            statements.AddRange(
                orderedOutputContractStatements.Select(
                    s =>
                        s.Statement switch
                        {
                            BlockSyntax block => block.WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                            _ => s.Statement
                        } ) );

            return statements;
        }

        private static IEnumerable<InsertedStatement> OrderInitializerStatements( IEnumerable<InsertedStatement> statements )

            // Initializers of separate declarations should precede initializers of the type.
            => statements
                .OrderBy(
                    s => s.ContextDeclaration switch
                    {
                        IMember => 0,
                        INamedType => 1,
                        _ => throw new AssertionFailedException( $"Unexpected declaration: '{s.ContextDeclaration}'." )
                    } )
                .ThenBy( s => (s.ContextDeclaration as IMember)?.ToDisplayString() );

        private static IEnumerable<InsertedStatement> OrderInputContractStatements( IEnumerable<InsertedStatement> statements )
            =>

                // Makes sure that the order is not changed when override is added in the middle of aspects that insert statements.
                statements
                    .OrderBy(
                        s => s.ContextDeclaration switch
                        {
                            IParameter { IsReturnParameter: false } parameter => parameter.Index, // Parameters are checked in order they appear in code.
                            _ => throw new AssertionFailedException( $"Unexpected declaration: '{s.ContextDeclaration}'." )
                        } )
                    .ThenByDescending( s => s.Transformation.OrderWithinPipeline )
                    .ThenByDescending( s => s.Transformation.OrderWithinPipelineStepAndType )
                    .ThenBy( s => s.Transformation.OrderWithinPipelineStepAndTypeAndAspectInstance );

        private static IEnumerable<InsertedStatement> OrderOutputContractStatements( IEnumerable<InsertedStatement> statements )
            =>

                // Makes sure that the order is not changed when override is added in the middle of aspects that insert statements.
                statements
                    .OrderBy(
                        s => s.ContextDeclaration switch
                        {
                            IParameter { IsReturnParameter: false } parameter => parameter.Index, // Parameters are checked in order they appear in code.
                            IParameter { IsReturnParameter: true, ContainingDeclaration: IMethod method } =>
                                method.Parameters.Count, // Method return value contracts are ordered after other parameters
                            _ => throw new AssertionFailedException( $"Unexpected declaration: '{s.ContextDeclaration}'." )
                        } )
                    .ThenByDescending( s => s.Transformation.OrderWithinPipeline )
                    .ThenByDescending( s => s.Transformation.OrderWithinPipelineStepAndType )
                    .ThenBy( s => s.Transformation.OrderWithinPipelineStepAndTypeAndAspectInstance );

        private static MemberLayerIndex GetTransformationMemberLayerIndex( ITransformation? transformation )
            => transformation != null
                ? new MemberLayerIndex(
                    transformation.OrderWithinPipeline,
                    transformation.OrderWithinPipelineStepAndType,
                    transformation.OrderWithinPipelineStepAndTypeAndAspectInstance )
                : new MemberLayerIndex( 0, 0, 0 );

        public void AddIntroducedSyntaxTree( SyntaxTree transformedSyntaxTree )
        {
            // HACK: When dependencies are incorrectly computed, the partial compilation may fail to contain all observed syntax trees.
            // In this case, there will be an attempt to add an existing syntax tree to the compilation. Here we work around this issue,
            // however the problem is upstream. Even if we solve the issue, it may be good to be tolerant of upstream bugs in this code.

            if ( this._finalCompilationModel.RoslynCompilation.GetIndexedSyntaxTrees().ContainsKey( transformedSyntaxTree.FilePath ) )
            {
                return;
            }

            lock ( this._introducedSyntaxTrees )
            {
                var added = this._introducedSyntaxTrees.TryAdd( transformedSyntaxTree.FilePath, transformedSyntaxTree );

                // If the tree was not added, check that it was identically the same as the existing one with the same path.
                Invariant.Assert( added || this._introducedSyntaxTrees[transformedSyntaxTree.FilePath] == transformedSyntaxTree );
            }
        }
    }
}