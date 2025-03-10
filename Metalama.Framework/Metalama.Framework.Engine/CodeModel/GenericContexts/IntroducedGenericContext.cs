// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

/// <summary>
/// Implements a <see cref="GenericContext"/> that may map parameters to introduced types (instead of just symbols).
/// </summary>
internal sealed class IntroducedGenericContext : GenericContext
{
    private readonly ImmutableArray<IFullRef<IType>> _typeArguments;
    private readonly IntroducedGenericContext? _parentContext;
    private readonly IFullRef<IDeclaration> _definition;

    public IntroducedGenericContext(
        ImmutableArray<IFullRef<IType>> typeArguments,
        IFullRef<IDeclaration> definition,
        IntroducedGenericContext? parentContext )
    {
        this._typeArguments = typeArguments;
        this._parentContext = parentContext;
        this._definition = definition;
    }

    internal override GenericContextKind Kind => GenericContextKind.Introduced;

    internal override ImmutableArray<IFullRef<IType>> TypeArguments => this._typeArguments;

    internal override IType Map( ITypeParameter typeParameter )
    {
        if ( typeParameter.ContainingDeclaration.AssertNotNull().GetDefinition().Equals( this._definition.Definition ) )
        {
            return this._typeArguments[typeParameter.Index].GetTarget( typeParameter.Compilation );
        }
        else if ( this._parentContext != null )
        {
            return this._parentContext.Map( typeParameter );
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    protected override IType Map( ITypeParameterSymbol typeParameterSymbol, CompilationModel compilation )
    {
        // We must be called only in the context of the symbol-based declaration, otherwise there could be no ITypeParameterSymbol.
        var ourDefinitionSymbol = this._definition.Definition.GetSymbol().AssertSymbolNotNull();

        if ( typeParameterSymbol.ContainingSymbol.OriginalDefinition.Equals( ourDefinitionSymbol ) )
        {
            return this._typeArguments[typeParameterSymbol.Ordinal].GetTarget( compilation );
        }
        else if ( this._parentContext != null )
        {
            return this._parentContext.Map( typeParameterSymbol, compilation );
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    internal override GenericContext Map( GenericContext genericContext, RefFactory refFactory )
    {
        var parentContext = (IntroducedGenericContext?) this._parentContext?.Map( genericContext, refFactory );

        var mappedTypeArguments = ImmutableArray.CreateBuilder<IFullRef<IType>>( this.TypeArguments.Length );

        foreach ( var typeArgument in this.TypeArguments )
        {
            mappedTypeArguments.Add( genericContext.Map( typeArgument.ConstructedDeclaration ).ToFullRef() );
        }

        var mappedGenericContext = new IntroducedGenericContext(
            mappedTypeArguments.MoveToImmutable(),
            this._definition,
            parentContext );

        return mappedGenericContext;
    }

    public override bool Equals( GenericContext? other )
    {
        if ( other is not IntroducedGenericContext otherIntroducedGenericContext )
        {
            return false;
        }

        if ( !this._definition.Equals( otherIntroducedGenericContext._definition ) )
        {
            return false;
        }

        if ( this._parentContext != null && !this._parentContext.Equals( otherIntroducedGenericContext._parentContext ) )
        {
            return false;
        }

        for ( var i = 0; i < this._typeArguments.Length; i++ )
        {
            if ( !this._typeArguments[i].Equals( otherIntroducedGenericContext._typeArguments[i] ) )
            {
                return false;
            }
        }

        return true;
    }

    protected override int GetHashCodeCore()
    {
        var hashCode = HashCode.Combine( this._parentContext, this._definition.GetHashCode( RefComparison.Default ) );

        foreach ( var arg in this._typeArguments )
        {
            hashCode = HashCode.Combine( hashCode, arg.GetHashCode( RefComparison.Default ) );
        }

        return hashCode;
    }

    public override string ToString()
    {
        string s;

        if ( this._parentContext != null )
        {
            s = this._parentContext + ".";
        }
        else
        {
            s = "";
        }

        s += "<" + string.Join( ", ", this._typeArguments ) + ">";

        return s;
    }
}