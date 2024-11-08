// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Validation;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.Pipeline;

internal sealed class DesignTimeReferenceValidatorCollection
{
    private readonly ReferenceIndexerOptions _ownOptions;
    private readonly ImmutableDictionaryOfArray<SymbolDictionaryKey, DesignTimeReferenceValidatorInstance> _ownValidators;
    private readonly ImmutableDictionaryOfArray<SymbolDictionaryKey, DesignTimeReferenceValidatorInstance> _allValidators;
    private readonly HashSet<DesignTimeReferenceValidatorCollection> _validatorCollectionsFromProjectReferences;

    public bool IsEmpty => this._allValidators.IsEmpty;

    public static DesignTimeReferenceValidatorCollection Empty { get; } =
        new(
            ReferenceIndexerOptions.Empty,
            ImmutableDictionaryOfArray<SymbolDictionaryKey, DesignTimeReferenceValidatorInstance>.Empty,
            ImmutableArray<DesignTimeReferenceValidatorCollection>.Empty );

    public ReferenceIndexerOptions Options { get; }

    private DesignTimeReferenceValidatorCollection(
        ReferenceIndexerOptions ownOptions,
        ImmutableDictionaryOfArray<SymbolDictionaryKey, DesignTimeReferenceValidatorInstance> ownValidators,
        IEnumerable<DesignTimeReferenceValidatorCollection> validatorsFromProjectReferences )
    {
        this._ownOptions = ownOptions;
        this._ownValidators = ownValidators;

        // The reason of the structure of this class is to cope with project graphs, especially diamond-shared projects graphs, where we need to avoid duplicates
        // of projects or validators inside projects. The opinion taken here is that it is cheaper by orders of magnitude to deduplicate projects than validators,
        // so we deduplicate whole collections instead of individual validators. However this makes the data structure more complex.

        this._validatorCollectionsFromProjectReferences =
            validatorsFromProjectReferences.SelectManyRecursiveDistinct( x => x._validatorCollectionsFromProjectReferences, includeRoots: true );

        this._allValidators = this._ownValidators.Merge( this._validatorCollectionsFromProjectReferences.Select( x => x._ownValidators ) );

        this.Options = new ReferenceIndexerOptions( this._validatorCollectionsFromProjectReferences.Select( x => x.Options ).Concat( this._ownOptions ) );
    }

    public DesignTimeReferenceValidatorCollection WithChildCollections( IEnumerable<DesignTimeReferenceValidatorCollection> childCollections )
        => new( this._ownOptions, this._ownValidators, childCollections );

    internal IReadOnlyCollection<DesignTimeReferenceValidatorInstance> GetValidatorsForSymbol( ISymbol symbol )
    {
        var symbolKey = SymbolDictionaryKey.CreateLookupKey( symbol );

        var validators = this._allValidators[symbolKey];

        if ( validators.IsDefault )
        {
            throw new AssertionFailedException();
        }

        return validators;
    }

    public Builder ToBuilder() => new( this._ownValidators.ToBuilder() );

    public ImmutableArray<TransitiveValidatorInstance> ToTransitiveValidatorInstances()
    {
        var builder = ImmutableArray.CreateBuilder<TransitiveValidatorInstance>();

        foreach ( var key in this._ownValidators.Keys )
        {
            foreach ( var validator in this._ownValidators[key] )
            {
                builder.Add( validator.ToTransitiveValidatorInstance() );
            }
        }

        return builder.ToImmutable();
    }

    public sealed class Builder( ImmutableDictionaryOfArray<SymbolDictionaryKey, DesignTimeReferenceValidatorInstance>.Builder builder )
    {
        public void Remove( DesignTimeReferenceValidatorInstance validator )
        {
            if ( !builder.Remove( validator.ValidatedDeclaration, validator ) )
            {
#if DEBUG
                throw new AssertionFailedException( "Cannot remove validator." );
#endif
            }
        }

        public void Add( DesignTimeReferenceValidatorInstance validator ) => builder.Add( validator.ValidatedDeclaration, validator );

        public DesignTimeReferenceValidatorCollection ToImmutable( IEnumerable<DesignTimeReferenceValidatorCollection> childCollections )
        {
            var ownValidators = builder.ToImmutable();

            return new DesignTimeReferenceValidatorCollection(
                new ReferenceIndexerOptions( ownValidators.SelectMany( x => x ) ),
                ownValidators,
                childCollections );
        }
    }
}