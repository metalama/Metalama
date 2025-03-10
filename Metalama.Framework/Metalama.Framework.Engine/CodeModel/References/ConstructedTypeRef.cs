// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Introductions.ConstructedTypes;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed class ConstructedTypeRef<T> : FullRef<T>
    where T : class, IType
{
    // This is the type in the canonical CompilationModel.
    private readonly ConstructedType _constructedType;

    public ConstructedTypeRef( RefFactory refFactory, ConstructedType constructedType ) : base( refFactory )
    {
        Invariant.Assert( constructedType is T );

        this._constructedType = constructedType;
    }

    protected override ICompilationElement Resolve( CompilationModel compilation, bool throwIfMissing, IGenericContext genericContext, Type interfaceType )
    {
        // TODO: Non-identity GenericContext?
        Invariant.Assert( genericContext.IsEmptyOrIdentity );

        return this._constructedType.ForCompilation( compilation );
    }

    public override bool Equals( IRef? other, RefComparison comparison )
    {
        if ( other is ConstructedTypeRef<T> otherConstructedType )
        {
            return this._constructedType.Equals( otherConstructedType._constructedType, comparison.ToTypeComparison() );
        }
        else
        {
            return false;
        }
    }

    public override int GetHashCode( RefComparison comparison ) => this._constructedType.GetHashCode( comparison.ToTypeComparison() );

    public override SyntaxTree? PrimarySyntaxTree => null;

    public override DeclarationKind DeclarationKind => DeclarationKind.Type;

    public override IFullRef? ContainingDeclaration => null;

    public override IFullRef<INamedType>? DeclaringType => null;

    public override string? Name => null;

    public override bool IsDefinition => true;

    public override IFullRef<T> DefinitionRef => this;

    protected override ISymbol GetSymbolIgnoringRefKind( CompilationContext compilationContext )
        => throw new NotSupportedException();

    public override void EnumerateAttributes( CompilationModel compilation, Action<AttributeRef> add ) => throw new NotSupportedException();

    public override void EnumerateAllImplementedInterfaces( Action<IFullRef<INamedType>> add )
        => throw new NotSupportedException();

    public override void EnumerateImplementedInterfaces( Action<IFullRef<INamedType>> add ) => throw new NotSupportedException();

    public override IEnumerable<IFullRef> GetMembersOfName( string name, DeclarationKind kind, CompilationModel compilation )
        => throw new NotSupportedException();

    public override IEnumerable<IFullRef> GetMembers( DeclarationKind kind, CompilationModel compilation ) => throw new NotSupportedException();

    protected override IFullRef<TOut> CastAsFullRef<TOut>() => (IFullRef<TOut>) (object) this;

    public override FullRef<T> WithGenericContext( GenericContext genericContext ) => throw new NotImplementedException();
}