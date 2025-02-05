// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal sealed class ParameterUpdatableCollection : DeclarationUpdatableCollection<IParameter>
{
    private readonly IRef<IHasParameters> _parent;

    public ParameterUpdatableCollection( CompilationModel compilation, IRef<IHasParameters> parent ) : base( compilation )
    {
        this._parent = parent;
    }

    protected override void PopulateAllItems( Action<IFullRef<IParameter>> action )
    {
        // TODO: Move to IRefCollectionStrategy.

        switch ( this._parent )
        {
            case ISymbolRef { Symbol: IMethodSymbol method }:
                foreach ( var p in method.Parameters )
                {
                    action( this.RefFactory.FromSymbol<IParameter>( p ) );
                }

                break;

            case ISymbolRef { Symbol: IPropertySymbol { Parameters.IsEmpty: false } indexer }:
                foreach ( var p in indexer.Parameters )
                {
                    action( this.RefFactory.FromSymbol<IParameter>( p ) );
                }

                break;

            case IIntroducedRef { BuilderData: MethodBuilderData builder }:
                foreach ( var p in builder.Parameters )
                {
                    action( this.RefFactory.FromBuilderData<IParameter>( p ) );
                }

                break;

            case IIntroducedRef { BuilderData: ConstructorBuilderData builder }:
                foreach ( var p in builder.Parameters )
                {
                    action( this.RefFactory.FromBuilderData<IParameter>( p ) );
                }

                break;

            case IIntroducedRef { BuilderData: IndexerBuilderData indexerBuilder }:
                foreach ( var p in indexerBuilder.Parameters )
                {
                    action( this.RefFactory.FromBuilderData<IParameter>( p ) );
                }

                break;

            default:
                throw new AssertionFailedException( $"Unexpected parent type: '{this._parent}'." );
        }
    }

    public void Add( ParameterBuilderData parameterBuilder )
    {
        this.EnsureComplete();

        var lastParam =
            this.Count > 0
                ? (IRef<IParameter>?) this[this.Count - 1]
                : null;

        if ( lastParam is ISymbolRef { Symbol: IParameterSymbol { IsParams: true } } )
        {
            this.InsertItem( this.Count - 1, parameterBuilder.ToRef() );
        }
        else
        {
            this.AddItem( parameterBuilder.ToRef() );
        }
    }
}