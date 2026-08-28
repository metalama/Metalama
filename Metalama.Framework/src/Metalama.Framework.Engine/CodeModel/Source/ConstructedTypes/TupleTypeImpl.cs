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
using System.Collections.Immutable;
using System.Linq;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Source.ConstructedTypes;

internal sealed partial class TupleTypeImpl : SourceNamedTypeImpl
{
    private readonly ImmutableArray<string> _overridenElementNames;

    internal TupleTypeImpl(
        INamedTypeSymbol namedTypeSymbol,
        CompilationModel compilation,
        GenericContext? genericContextForSymbolMapping,
        ImmutableArray<string> overridenElementNames = default ) : base(
        namedTypeSymbol,
        compilation,
        genericContextForSymbolMapping )
    {
        this._overridenElementNames = overridenElementNames;
    }

    [Memo]
    public ImmutableArray<ITupleElement> TupleElements => this.NamedTypeSymbol.TupleElements.SelectAsImmutableArray( this.GetTupleElement );

    [Memo]
    internal ImmutableArray<string> TupleElementNames
        => this._overridenElementNames.IsDefault ? this.NamedTypeSymbol.TupleElements.SelectAsImmutableArray( e => e.Name ) : this._overridenElementNames;

    /// <summary>
    /// Gets the fields of the tuple, which are the fields of the underlying <c>ValueTuple</c>.
    /// </summary>
    /// <remarks>
    /// The symbol of a tuple whose elements are named exposes the named fields in addition to the positional ones of
    /// the underlying type, so the fields are taken from the underlying type, which reports each position once. See
    /// issue #1844.
    /// </remarks>
    [Memo]
    public override IFieldCollection Fields
        => new FieldCollection(
            this.Facade,
            this.Compilation.GetFieldCollection(
                (this.NamedTypeSymbol.TupleUnderlyingType ?? this.NamedTypeSymbol).ToRef( this.RefFactory ) ) );

    private ITupleElement GetTupleElement( IFieldSymbol symbol, int index )
    {
        var elementName = this._overridenElementNames.IsDefault ? symbol.Name : this._overridenElementNames[index];

        return new TupleElement( symbol, this.Compilation, this.GenericContextForSymbolMapping, elementName, index );
    }

    public int TupleLength => this.TupleElements.Length;

    protected override void CheckSymbol()
    {
        Invariant.Assert( this.NamedTypeSymbol.IsTupleType );
    }

    public override TypeKind TypeKind => TypeKind.Tuple;

    protected override IFullRef<INamedType> CreateFullRef() => this.RefFactory.FromSymbolBasedTupleTypeDeclaration( this );

    // A canonical ValueType`n with n > 0 is never represented as a TupleType.
    public override bool IsCanonicalGenericInstance => false;
}