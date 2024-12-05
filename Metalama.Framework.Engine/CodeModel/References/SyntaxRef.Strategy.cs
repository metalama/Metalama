// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.References;

internal sealed partial class SyntaxRef<T>
{
    public override string Name => throw new NotImplementedException();

    public override void EnumerateAttributes( CompilationModel compilation, Action<AttributeRef> add ) => throw new NotImplementedException();

    public override void EnumerateAllImplementedInterfaces( Action<IFullRef<INamedType>> add )
        => throw new NotImplementedException();

    public override void EnumerateImplementedInterfaces( Action<IFullRef<INamedType>> add )
        => throw new NotImplementedException();

    public override IEnumerable<IFullRef> GetMembersOfName( string name, DeclarationKind kind, CompilationModel compilation )
        => throw new NotImplementedException();

    public override IEnumerable<IFullRef> GetMembers( DeclarationKind kind, CompilationModel compilation ) => throw new NotImplementedException();
}