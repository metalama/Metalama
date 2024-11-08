// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed partial class IntroducedRef<T>
{
    public override void EnumerateAttributes( CompilationModel compilation, Action<AttributeRef> add )
    {
        foreach ( var attribute in this.BuilderData.Attributes )
        {
            add( attribute.ToRef() );
        }
    }

    public override void EnumerateAllImplementedInterfaces( Action<IFullRef<INamedType>> add )
    {
        Invariant.Assert( this is IRef<INamedType> );

        var resolvedNameType = (NamedTypeBuilderData) this.BuilderData;

        foreach ( var i in resolvedNameType.ImplementedInterfaces )
        {
            add( i );
        }
    }

    public override void EnumerateImplementedInterfaces( Action<IFullRef<INamedType>> add )
    {
        Invariant.Assert( this is IRef<INamedType> );

        // BUG: EnumerateAllImplementedInterfaces and EnumerateImplementedInterfaces should not have the same implementation.

        var resolvedNameType = (NamedTypeBuilderData) this.BuilderData;

        foreach ( var i in resolvedNameType.ImplementedInterfaces )
        {
            add( i );
        }
    }

    public override IEnumerable<IFullRef> GetMembersOfName(
        string name,
        DeclarationKind kind,
        CompilationModel compilation )
        => Enumerable.Empty<IFullRef>();

    public override IEnumerable<IFullRef> GetMembers( DeclarationKind kind, CompilationModel compilation ) => Enumerable.Empty<IFullRef>();
}