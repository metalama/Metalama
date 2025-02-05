// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Extensibility;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline
{
    internal partial class SyntaxTreePipelineResult
    {
        /// <summary>
        /// Builds a <see cref="SyntaxTreePipelineResult"/>.
        /// </summary>
        public sealed class Builder
        {
            private readonly SyntaxTree? _syntaxTree;

#pragma warning disable SA1401 // Fields should be private
            public ImmutableArray<Diagnostic>.Builder? Diagnostics;
            public ImmutableArray<CacheableScopedSuppression>.Builder? Suppressions;
            public ImmutableArray<IntroducedSyntaxTree>.Builder? Introductions;
            public ImmutableArray<InheritableAspectInstance>.Builder? InheritableAspects;
            public ImmutableArray<IDesignTimeAspectPipelineResultExtension>.Builder? Extensions;
            public ImmutableArray<DesignTimeAspectInstance>.Builder? AspectInstances;
            public ImmutableArray<DesignTimeTransformation>.Builder? Transformations;
            public ImmutableArray<InheritableOptionsInstance>.Builder? InheritableOptions;
            public ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation>.Builder? Annotations;

#pragma warning restore SA1401 // Fields should be private

            public Builder( SyntaxTree? syntaxTree )
            {
                this._syntaxTree = syntaxTree;
            }

            public SyntaxTreePipelineResult ToImmutable( Compilation compilation )
            {
                ImmutableArray<string> dependencies;

                if ( this._syntaxTree != null )
                {
                    // Compute the default dependency graph.
                    var semanticModel = compilation.GetCachedSemanticModel( this._syntaxTree );

                    var declaredTypes = new List<INamedTypeSymbol>();
                    DependencyAnalysisHelper.FindDeclaredTypes( semanticModel, declaredTypes.Add );

                    dependencies = declaredTypes
                        .SelectMany( t => t.DeclaringSyntaxReferences )
                        .Select( r => r.SyntaxTree.FilePath )
                        .Where( p => p != this._syntaxTree.FilePath )
                        .Distinct()
                        .ToImmutableArray();
                }
                else
                {
                    dependencies = ImmutableArray<string>.Empty;
                }

                return new SyntaxTreePipelineResult(
                    this._syntaxTree?.FilePath,
                    this.Diagnostics?.ToImmutable(),
                    this.Suppressions?.ToImmutable(),
                    this.Introductions?.ToImmutable(),
                    dependencies,
                    this.InheritableAspects?.ToImmutable(),
                    this.Extensions?.ToImmutable(),
                    this.AspectInstances?.ToImmutable(),
                    this.Transformations?.ToImmutable(),
                    this.InheritableOptions?.ToImmutable(),
                    this.Annotations?.ToImmutable() );
            }
        }
    }
}