// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Facets;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Source;

/// <summary>
/// The facet of an enum read from source or from a referenced assembly.
/// </summary>
internal sealed class SourceEnumFacet : EnumFacet
{
    public SourceEnumFacet( INamedType type ) : base( type ) { }

    /// <summary>
    /// Gets the members of the enum, in the order of declaration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order of <see cref="INamedType.Fields"/> depends on which fields a previous consumer resolved by name,
    /// so the order is taken from the symbol. Each field is then resolved through the code model, so that a member
    /// of this list is the same object as the one that <see cref="INamedType.Fields"/> returns for that name.
    /// </para>
    /// </remarks>
    [Memo]
    public override IReadOnlyList<IField> Members => this.GetMembersCore();

    private IReadOnlyList<IField> GetMembersCore()
    {
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
}
