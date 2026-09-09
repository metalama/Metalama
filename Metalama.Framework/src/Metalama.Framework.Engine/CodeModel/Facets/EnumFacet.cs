// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Facets;

/// <summary>
/// Implementation of <see cref="IEnumFacet"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class stores the type only. Every other member is resolved on first read and memoized, so constructing the
/// facet of a type whose structure is never read costs one allocation and no resolution.
/// </para>
/// </remarks>
internal sealed class EnumFacet : IEnumFacet
{
    public EnumFacet( INamedType type )
    {
        this.Type = type;
    }

    public TypeFacetKind FacetKind => TypeFacetKind.Enum;

    public INamedType Type { get; }

    public INamedType UnderlyingType => this.Type.UnderlyingType;

    [Memo]
    public IReadOnlyList<IField> Members => this.GetMembersCore();

    private IReadOnlyList<IField> GetMembersCore()
    {
        // The order of INamedType.Fields depends on which fields a previous consumer resolved by name, so the order
        // of declaration is taken from the symbol. Each field is then resolved through the code model, so that a
        // member of this list is the same object as the one that INamedType.Fields returns for that name.
        var symbol = (INamedTypeSymbol) ((ISymbolBasedCompilationElement) this.Type).Symbol;

        var builder = ImmutableArray.CreateBuilder<IField>();

        foreach ( var member in symbol.GetMembers() )
        {
            // The members of an enum are its constants. The instance field named value__, which carries the
            // underlying value, is not one of them.
            if ( member.Kind == SymbolKind.Field && member is IFieldSymbol { IsConst: true } )
            {
                builder.Add( this.Type.Fields.OfName( member.Name ).Single() );
            }
        }

        return builder.ToImmutable();
    }

    [Memo]
    public bool IsFlags => this.Type.Attributes.Any( typeof(FlagsAttribute) );
}
