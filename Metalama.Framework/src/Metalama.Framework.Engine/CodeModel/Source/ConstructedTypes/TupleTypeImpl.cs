// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Source.ConstructedTypes;

internal sealed partial class TupleTypeImpl : SourceNamedTypeImpl
{
    private readonly IReadOnlyList<string?>? _overridenElementNames;

    internal TupleTypeImpl(
        INamedTypeSymbol namedTypeSymbol,
        CompilationModel compilation,
        GenericContext? genericContextForSymbolMapping,
        IReadOnlyList<string?>? overridenElementNames = null ) : base(
        namedTypeSymbol,
        compilation,
        genericContextForSymbolMapping )
    {
        this._overridenElementNames = overridenElementNames;
    }

    [Memo]
    public IReadOnlyList<ITupleElement> TupleElements => this.NamedTypeSymbol.TupleElements.SelectAsImmutableArray( this.GetTupleElement );

    [Memo]
    public override IFieldCollection Fields
        => new FieldCollection(
            this.Facade,
            this.Compilation.GetFieldCollection( this.NamedTypeSymbol.ToRef( this.RefFactory ) ) );

    private ITupleElement GetTupleElement( IFieldSymbol symbol, int index )
    {
        var name = this._overridenElementNames?[index] ?? symbol.Name;

        return new TupleElement( symbol, this.Compilation, this.GenericContextForSymbolMapping, name, index );
    }

    public int TupleLength => this.TupleElements.Count;

    protected override void CheckSymbol()
    {
        Invariant.Assert( this.NamedTypeSymbol.IsTupleType );
    }

    public override TypeKind TypeKind => TypeKind.Tuple;

    protected override IFullRef<INamedType> CreateFullRef() => this.RefFactory.FromSymbolBasedDeclaration<ITupleType>( this );

    // A canonical ValueType`n with n > 0 is never represented as a TupleType.
    public override bool IsCanonicalGenericInstance => false;
}