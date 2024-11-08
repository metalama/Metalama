// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal abstract class PropertyOrIndexerBuilderData( PropertyOrIndexerBuilder builder, IFullRef<INamedType> containingDeclaration )
    : MemberBuilderData( builder, containingDeclaration )
{
    public IRef<IType> Type { get; } = builder.Type.ToRef();

    public bool HasInitOnlySetter { get; } = builder.HasInitOnlySetter;

    public RefKind RefKind { get; } = builder.RefKind;

    // Accessors are abstract because we can't initialize them from the constructor because we don't have a reference to ourselves yet.
    public abstract MethodBuilderData? GetMethod { get; }

    public abstract MethodBuilderData? SetMethod { get; }

    public Writeability Writeability { get; } = builder.Writeability;

    public override IEnumerable<DeclarationBuilderData> GetOwnedDeclarations()
    {
        var owned = base.GetOwnedDeclarations();

        return (this.GetMethod, this.SetMethod) switch
        {
            (null, null) => owned,
            (null, { } setMethod) => owned.Concat( setMethod ),
            ({ } getMethod, null) => owned.Concat( getMethod ),
            ({ } getMethod, { } setMethod) => owned.Concat( [getMethod, setMethod] )
        };
    }
}