// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.ReferenceGraph;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Extensibility;

public sealed class DesignTimeAspectPipelineResultExtensionCollection
{
    private readonly ReferenceIndexerOptions _ownOptions;
    private readonly ImmutableDictionaryOfArray<SymbolDictionaryKey, IDesignTimeValidatorExtension> _ownValidators;
    private readonly ImmutableDictionaryOfArray<SymbolDictionaryKey, IDesignTimeValidatorExtension> _allValidators;
    private readonly HashSet<DesignTimeAspectPipelineResultExtensionCollection> _extensionsFromProjectReferences;

    public bool IsEmpty => this._allValidators.IsEmpty && this.Extensions.IsEmpty;

    public static DesignTimeAspectPipelineResultExtensionCollection Empty { get; } =
        new(
            ReferenceIndexerOptions.Empty,
            ImmutableDictionaryOfArray<SymbolDictionaryKey, IDesignTimeValidatorExtension>.Empty,
            ImmutableArray<IDesignTimePipelineResultExtension>.Empty,
            ImmutableArray<DesignTimeAspectPipelineResultExtensionCollection>.Empty );

    public ReferenceIndexerOptions Options { get; }

    public ImmutableArray<IDesignTimePipelineResultExtension> Extensions { get; }

    private DesignTimeAspectPipelineResultExtensionCollection(
        ReferenceIndexerOptions ownOptions,
        ImmutableDictionaryOfArray<SymbolDictionaryKey, IDesignTimeValidatorExtension> ownValidators,
        ImmutableArray<IDesignTimePipelineResultExtension> extensions,
        IEnumerable<DesignTimeAspectPipelineResultExtensionCollection> validatorsFromProjectReferences )
    {
        this._ownOptions = ownOptions;
        this._ownValidators = ownValidators;
        this.Extensions = extensions;

        // The reason of the structure of this class is to cope with project graphs, especially diamond-shared projects graphs, where we need to avoid duplicates
        // of projects or validators inside projects. The opinion taken here is that it is cheaper by orders of magnitude to deduplicate projects than validators,
        // so we deduplicate whole collections instead of individual validators. However this makes the data structure more complex.

        this._extensionsFromProjectReferences =
            validatorsFromProjectReferences.SelectManyRecursiveDistinct( x => x._extensionsFromProjectReferences, includeRoots: true );

        this._allValidators = this._ownValidators.Merge( this._extensionsFromProjectReferences.SelectAsReadOnlyCollection( x => x._ownValidators ) );

        this.Options = new ReferenceIndexerOptions(
            this._extensionsFromProjectReferences.SelectAsReadOnlyCollection( x => x.Options ).Concat( this._ownOptions ) );
    }

    public DesignTimeAspectPipelineResultExtensionCollection WithChildCollections(
        IEnumerable<DesignTimeAspectPipelineResultExtensionCollection> childCollections )
        => new( this._ownOptions, this._ownValidators, this.Extensions, childCollections );

    public IReadOnlyCollection<IDesignTimeValidatorExtension> GetValidatorsForSymbol( ISymbol symbol )
    {
        var symbolKey = SymbolDictionaryKey.CreateLookupKey( symbol );

        var validators = this._allValidators[symbolKey];

        if ( validators.IsDefault )
        {
            throw new AssertionFailedException();
        }

        return validators;
    }

    public Builder ToBuilder() => new( this._ownValidators.ToBuilder(), this.Extensions.ToBuilder() );

    public ImmutableArray<ITransitiveAspectsManifestExtension> ToTransitiveValidatorInstances( bool includeValidators )
        => includeValidators
            ? this.Extensions.SelectAsImmutableArray( x => x.ToTransitiveAspectManifestExtension() )
            : this.Extensions.Where( e => !e.ContributorKind.IsValidator ).Select( x => x.ToTransitiveAspectManifestExtension() ).ToImmutableArray();

    public sealed class Builder
    {
        private readonly ImmutableDictionaryOfArray<SymbolDictionaryKey, IDesignTimeValidatorExtension>.Builder _validatorsBuilder;
        private readonly ImmutableArray<IDesignTimePipelineResultExtension>.Builder _extensionsBuilder;

        public Builder(
            ImmutableDictionaryOfArray<SymbolDictionaryKey, IDesignTimeValidatorExtension>.Builder validatorsBuilder,
            ImmutableArray<IDesignTimePipelineResultExtension>.Builder extensionsBuilder )
        {
            this._validatorsBuilder = validatorsBuilder;
            this._extensionsBuilder = extensionsBuilder;
        }

        public void Remove( IDesignTimePipelineResultExtension extension )
        {
            if ( extension.ContributorKind.IsValidator && extension is IDesignTimeValidatorExtension validator )
            {
                if ( !this._validatorsBuilder.Remove( validator.ValidatedDeclaration, validator ) )
                {
#if DEBUG
                    throw new AssertionFailedException( "Cannot remove validator." );
#endif
                }
            }

            if ( !this._extensionsBuilder.Remove( extension ) )
            {
#if DEBUG
                throw new AssertionFailedException( "Cannot remove validator." );
#endif
            }
        }

        public void Add( IDesignTimePipelineResultExtension extension )
        {
            if ( extension.ContributorKind.IsValidator && extension is IDesignTimeValidatorExtension validator )
            {
                this._validatorsBuilder.Add( validator.ValidatedDeclaration, validator );
            }

            this._extensionsBuilder.Add( extension );
        }

        public DesignTimeAspectPipelineResultExtensionCollection ToImmutable( IEnumerable<DesignTimeAspectPipelineResultExtensionCollection> childCollections )
        {
            var validators = this._validatorsBuilder.ToImmutable();
            var transitiveExtensions = this._extensionsBuilder.ToImmutable();

            return new DesignTimeAspectPipelineResultExtensionCollection(
                new ReferenceIndexerOptions( validators.SelectMany( x => x ).Select( x => x.ReferenceIndexerRequirements ).WhereNotNull() ),
                validators,
                transitiveExtensions,
                childCollections );
        }
    }
}