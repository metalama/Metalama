// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Aspects
{
    /// <summary>
    /// Represents an instance of an aspect and its target declaration.
    /// </summary>
    public sealed class AspectInstance : IAspectInstanceInternal, IComparable<AspectInstance>
    {
        /// <summary>
        /// Gets the aspect instance.
        /// </summary>
        public IAspect Aspect { get; }

        IAspectClass IAspectInstance.AspectClass => this.AspectClass;

        internal IAspectClassImpl AspectClass { get; }

        IAspectClassImpl IAspectInstanceInternal.AspectClass => this.AspectClass;

        IRef<IDeclaration> IAspectPredecessor.TargetDeclaration => this.TargetDeclaration;

        internal IRef<IDeclaration> TargetDeclaration { get; }

        public bool IsSkipped { get; private set; }

        public bool IsInheritable { get; }

        public ImmutableArray<IAspectInstance> SecondaryInstances => ImmutableArray<IAspectInstance>.Empty;

        public void Skip() { this.IsSkipped = true; }

        internal ImmutableDictionary<TemplateClass, TemplateClassInstance> TemplateInstances { get; }

        ImmutableDictionary<TemplateClass, TemplateClassInstance> IAspectInstanceInternal.TemplateInstances => this.TemplateInstances;

        public ImmutableArray<AspectPredecessor> Predecessors { get; }

        public IAspectState? AspectState { get; internal set; }

        void IAspectInstanceInternal.SetState( IAspectState? value ) => this.AspectState = value;

        public int TargetDeclarationDepth { get; }

        internal AspectInstance( IAspect aspect, IDeclaration targetDeclaration, AspectClass aspectClass, in AspectPredecessor predecessor ) :
            this( aspect, targetDeclaration, aspectClass, ImmutableArray.Create( predecessor ) ) { }

        internal AspectInstance(
            IAspect aspect,
            IDeclaration targetDeclaration,
            AspectClass aspectClass,
            ImmutableArray<AspectPredecessor> predecessors )
        {
            this.Aspect = aspect;
            this.TargetDeclaration = targetDeclaration.ToRef();
            this.AspectClass = aspectClass;
            this.Predecessors = predecessors;
            this.TargetDeclarationDepth = targetDeclaration.Depth;

            this.TemplateInstances = ImmutableDictionary.Create<TemplateClass, TemplateClassInstance>()
                .Add( aspectClass, new TemplateClassInstance( TemplateProvider.FromInstance( aspect ), aspectClass ) );

#if DEBUG
            if ( !predecessors.IsDefaultOrEmpty )
            {
                foreach ( var predecessor in predecessors )
                {
                    predecessor.Instance.AssertNotNull();
                }
            }
#endif

            this.IsInheritable = aspectClass.IsInheritable
                                 ?? ((IConditionallyInheritableAspect) aspect).IsInheritable( targetDeclaration, this );
        }

        internal AspectInstance(
            IAspect aspect,
            IDeclaration targetDeclaration,
            IAspectClassImpl aspectClass,
            IEnumerable<TemplateClassInstance> templateInstances,
            ImmutableArray<AspectPredecessor> predecessors )
        {
            this.Aspect = aspect;
            this.TargetDeclaration = targetDeclaration.ToRef();
            this.AspectClass = aspectClass;
            this.Predecessors = predecessors;
            this.TargetDeclarationDepth = targetDeclaration.GetCompilationModel().GetDepth( targetDeclaration );

            this.TemplateInstances = templateInstances.ToImmutableDictionary( t => t.TemplateClass, t => t );

            this.IsInheritable = aspectClass.IsInheritable
                                 ?? ((IConditionallyInheritableAspect) aspect).IsInheritable( targetDeclaration, this );
        }

        internal EligibleScenarios ComputeEligibility( IDeclaration declaration )
        {
            var eligibility = this.AspectClass.GetEligibility( declaration, this.IsInheritable );

            if ( (eligibility & EligibleScenarios.Inheritance) != 0 && !((IDeclarationImpl) declaration).CanBeInherited )
            {
                eligibility &= ~EligibleScenarios.Inheritance;
            }

            return eligibility;
        }

        public override string ToString() => this.AspectClass.ShortName + "@" + this.TargetDeclaration;

        public FormattableString FormatPredecessor( ICompilation compilation )
            => $"aspect '{this.AspectClass.ShortName}' applied to '{this.TargetDeclaration.GetTarget( compilation )}'";

        public Location? GetDiagnosticLocation( Compilation compilation )
            => compilation.GetTypeByMetadataName( this.AspectClass.FullName )?.GetDiagnosticLocation();

        public int CompareTo( AspectInstance? other )
        {
            if ( other == null )
            {
                return 1;
            }

            // Compare by degree of predecessor. Shorter causality chains take precedence.
            var degreeComparison = this.PredecessorDegree.CompareTo( other.PredecessorDegree );

            if ( degreeComparison != 0 )
            {
                return degreeComparison;
            }

            // Compare by declaration depth of the root attribute or fabric. Higher depths takes precedence.
            static int GetMaxRootDepth( IAspectPredecessor aspectInstance )
                => aspectInstance.GetRoots()
                    .Max( p => ((IAspectPredecessorImpl) p).TargetDeclarationDepth );

            var depthComparison = GetMaxRootDepth( this ).CompareTo( GetMaxRootDepth( other ) );

            if ( depthComparison != 0 )
            {
                return -1 * depthComparison;
            }

            // Order ChildAspect before RequireAspect.
            static int GetKindOrder2( AspectInstance aspectInstance )
                => aspectInstance.Predecessors.IsDefaultOrEmpty
                    ? -1
                    : aspectInstance.Predecessors.Min(
                        x => x.Kind switch
                        {
                            AspectPredecessorKind.Attribute => 0,
                            AspectPredecessorKind.ChildAspect => 1,
                            AspectPredecessorKind.RequiredAspect => 2,
                            AspectPredecessorKind.Inherited => 3,
                            AspectPredecessorKind.Fabric => 4,
                            _ => throw new AssertionFailedException( $"Unexpected value: {x.Kind}" )
                        } );

            var predecessorKindComparison2 = GetKindOrder2( this ).CompareTo( GetKindOrder2( other ) );

            if ( predecessorKindComparison2 != 0 )
            {
                return predecessorKindComparison2;
            }

            // At this point, ordering is no longer deterministic. If the aspect needs better ordering, it must implement it by itself.

            return 0;
        }

        internal AspectInstance CreateDerivedInstance( IDeclaration target )
            => new(
                this.Aspect,
                target,
                (AspectClass) this.AspectClass,
                new AspectPredecessor( AspectPredecessorKind.Inherited, this ) );

        public int PredecessorDegree => this.Predecessors.IsDefaultOrEmpty ? 0 : this.Predecessors.Min( p => p.Instance.PredecessorDegree ) + 1;

        [Memo]
        public string DiagnosticSourceDescription => MetalamaStringFormatter.Format( $"aspect [{this.Aspect}] applied to '{this.TargetDeclaration}'" );

        [Memo]
        public ImmutableArray<SyntaxTree> PredecessorTreeClosure
            => this.Predecessors.SelectMany( p => (p.Instance as IAspectPredecessorImpl)?.PredecessorTreeClosure ?? ImmutableArray<SyntaxTree>.Empty )
                .Distinct()
                .ToImmutableArray();

        DeclarationOriginKind IDeclarationOrigin.Kind => DeclarationOriginKind.Aspect;

        bool IDeclarationOrigin.IsCompilerGenerated => true;

        IAspectInstance IAspectDeclarationOrigin.AspectInstance => this;
    }
}