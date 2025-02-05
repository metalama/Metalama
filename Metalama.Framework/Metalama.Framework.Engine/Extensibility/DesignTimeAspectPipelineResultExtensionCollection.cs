// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
    private readonly ImmutableDictionaryOfArray<SymbolDictionaryKey, IDesignTimeAspectPipelineResultExtension> _ownExtensions;
    private readonly ImmutableDictionaryOfArray<SymbolDictionaryKey, IDesignTimeAspectPipelineResultExtension> _allExtensions;
    private readonly HashSet<DesignTimeAspectPipelineResultExtensionCollection> _extensionsFromProjectReferences;

    public bool IsEmpty => this._allExtensions.IsEmpty;

    public static DesignTimeAspectPipelineResultExtensionCollection Empty { get; } =
        new(
            ReferenceIndexerOptions.Empty,
            ImmutableDictionaryOfArray<SymbolDictionaryKey, IDesignTimeAspectPipelineResultExtension>.Empty,
            ImmutableArray<DesignTimeAspectPipelineResultExtensionCollection>.Empty );

    public ReferenceIndexerOptions Options { get; }

    private DesignTimeAspectPipelineResultExtensionCollection(
        ReferenceIndexerOptions ownOptions,
        ImmutableDictionaryOfArray<SymbolDictionaryKey, IDesignTimeAspectPipelineResultExtension> ownExtensions,
        IEnumerable<DesignTimeAspectPipelineResultExtensionCollection> validatorsFromProjectReferences )
    {
        this._ownOptions = ownOptions;
        this._ownExtensions = ownExtensions;

        // The reason of the structure of this class is to cope with project graphs, especially diamond-shared projects graphs, where we need to avoid duplicates
        // of projects or validators inside projects. The opinion taken here is that it is cheaper by orders of magnitude to deduplicate projects than validators,
        // so we deduplicate whole collections instead of individual validators. However this makes the data structure more complex.

        this._extensionsFromProjectReferences =
            validatorsFromProjectReferences.SelectManyRecursiveDistinct( x => x._extensionsFromProjectReferences, includeRoots: true );

        this._allExtensions = this._ownExtensions.Merge( this._extensionsFromProjectReferences.SelectAsReadOnlyCollection( x => x._ownExtensions ) );

        this.Options = new ReferenceIndexerOptions(
            this._extensionsFromProjectReferences.SelectAsReadOnlyCollection( x => x.Options ).Concat( this._ownOptions ) );
    }

    public DesignTimeAspectPipelineResultExtensionCollection WithChildCollections(
        IEnumerable<DesignTimeAspectPipelineResultExtensionCollection> childCollections )
        => new( this._ownOptions, this._ownExtensions, childCollections );

    public IReadOnlyCollection<IDesignTimeAspectPipelineResultExtension> GetExtensionsForSymbol( ISymbol symbol )
    {
        var symbolKey = SymbolDictionaryKey.CreateLookupKey( symbol );

        var validators = this._allExtensions[symbolKey];

        if ( validators.IsDefault )
        {
            throw new AssertionFailedException();
        }

        return validators;
    }

    public Builder ToBuilder() => new( this._ownExtensions.ToBuilder() );

    public ImmutableArray<ITransitiveAspectsManifestExtension> ToTransitiveValidatorInstances()
    {
        var builder = ImmutableArray.CreateBuilder<ITransitiveAspectsManifestExtension>();

        foreach ( var key in this._ownExtensions.Keys )
        {
            foreach ( var validator in this._ownExtensions[key] )
            {
                builder.Add( validator.ToTransitiveAspectManifestExtension() );
            }
        }

        return builder.ToImmutable();
    }

    public sealed class Builder( ImmutableDictionaryOfArray<SymbolDictionaryKey, IDesignTimeAspectPipelineResultExtension>.Builder builder )
    {
        public void Remove( IDesignTimeAspectPipelineResultExtension validator )
        {
            if ( !builder.Remove( validator.ValidatedDeclaration, validator ) )
            {
#if DEBUG
                throw new AssertionFailedException( "Cannot remove validator." );
#endif
            }
        }

        public void Add( IDesignTimeAspectPipelineResultExtension validator ) => builder.Add( validator.ValidatedDeclaration, validator );

        public DesignTimeAspectPipelineResultExtensionCollection ToImmutable( IEnumerable<DesignTimeAspectPipelineResultExtensionCollection> childCollections )
        {
            var extensions = builder.ToImmutable();

            return new DesignTimeAspectPipelineResultExtensionCollection(
                new ReferenceIndexerOptions( extensions.SelectMany( x => x ).Select( x => x.ReferenceIndexerRequirements ) ),
                extensions,
                childCollections );
        }
    }
}