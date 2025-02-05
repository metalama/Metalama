// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Project;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Aspects;

internal sealed class IncrementalAspectRepository : AspectRepository
{
    private readonly ImmutableDictionaryOfArray<IRef<IDeclaration>, IAspectInstance> _aspects;
    private readonly CompilationModel _compilation;

    private IncrementalAspectRepository( ImmutableDictionaryOfArray<IRef<IDeclaration>, IAspectInstance> aspects, CompilationModel compilation )
    {
        Invariant.Assert( aspects.KeyComparer is IRefEqualityComparer );

        this._aspects = aspects;
        this._compilation = compilation;
    }

    public IncrementalAspectRepository( CompilationModel compilation ) : this(
        ImmutableDictionaryOfArray<IRef<IDeclaration>, IAspectInstance>.Empty.WithKeyComparer( RefEqualityComparer<IDeclaration>.Default ),
        compilation ) { }

    private void VerifyDeclaration( IDeclaration declaration )
    {
        var type = declaration.GetTopmostNamedType()?.GetSymbol()
                   ??
                   throw new InvalidOperationException(
                       MetalamaStringFormatter.Format( $"This method cannot be used for declarations of kind '{declaration.DeclarationKind}'." ) );

        if ( type.IsGenericType )
        {
            type = type.ConstructedFrom;
        }

        if ( !this._compilation.PartialCompilation.Types.Contains( type ) )
        {
            if ( MetalamaExecutionContext.Current.ExecutionScenario.IsDesignTime )
            {
                throw new InvalidOperationException(
                    MetalamaStringFormatter.Format(
                        $"Cannot call this method with the {declaration.DeclarationKind} '{declaration}' because it not a part of the current partial compilation." )
                    +
                    $"At design time, you can only use this method within the current type and its ancestors. " +
                    "Use the `MetalamaExecutionContext.Current.ExecutionScenario.IsDesignTime` expression to check if your code is running at design time. " +
                    "Also check the IDeclaration.BelongsToCurrentProject property." );
            }
            else
            {
                throw new InvalidOperationException(
                    MetalamaStringFormatter.Format(
                        $"Cannot call this method with the {declaration.DeclarationKind} '{declaration}' because it not a part of the current project. Check the IDeclaration.BelongsToCurrentProject property." ) );
            }
        }
    }

    // TODO: throw exception if the aspect of type T is not processed yet.
    // TODO: refactor the aspect lookup service so that the implementation can be injected from the CodeRefactoringService, so
    // that we don't have to run the pipeline to have a model including aspects.
    public override AspectRepository WithAspectInstances( IEnumerable<IAspectInstance> aspectInstances, CompilationModel compilation )
    {
        var newDictionary = this._aspects.AddRange( aspectInstances, instance => ((IAspectInstanceInternal) instance).TargetDeclaration );

        return new IncrementalAspectRepository( newDictionary, compilation );
    }

    // TODO: return null if the aspect of type T is not processed yet.
    public override bool HasAspect( IDeclaration declaration, Type aspectType )
    {
        this.VerifyDeclaration( declaration );

        return this._aspects[declaration.ToRef()].Any( a => aspectType.IsAssignableFrom( a.AspectClass.Type ) );
    }

    public override IEnumerable<IAspectInstance> GetAspectInstances( IDeclaration declaration )
    {
        this.VerifyDeclaration( declaration );

        return this._aspects[declaration.ToRef()];
    }
}