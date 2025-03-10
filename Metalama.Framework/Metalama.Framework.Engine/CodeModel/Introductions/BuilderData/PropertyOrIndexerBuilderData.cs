// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;

internal abstract class PropertyOrIndexerBuilderData : MemberBuilderData
{
    protected PropertyOrIndexerBuilderData( PropertyOrIndexerBuilder builder, IFullRef<INamedType> containingDeclaration ) : base(
        builder,
        containingDeclaration )
    {
        this.Type = builder.Type.ToRef();
        this.HasInitOnlySetter = builder.HasInitOnlySetter;
        this.RefKind = builder.RefKind;
        this.Writeability = builder.Writeability;
    }

    public IRef<IType> Type { get; }

    public bool HasInitOnlySetter { get; }

    public RefKind RefKind { get; }

    // Accessors are abstract because we can't initialize them from the constructor because we don't have a reference to ourselves yet.
    public abstract MethodBuilderData? GetMethod { get; }

    public abstract MethodBuilderData? SetMethod { get; }

    public Writeability Writeability { get; }

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