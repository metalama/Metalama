// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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