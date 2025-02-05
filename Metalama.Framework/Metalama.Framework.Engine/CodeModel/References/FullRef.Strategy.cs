// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.References;

internal abstract partial class FullRef<T>
{
    public abstract DeclarationKind DeclarationKind { get; }

    public abstract IFullRef? ContainingDeclaration { get; }

    public abstract IFullRef<INamedType>? DeclaringType { get; }

    public abstract string? Name { get; }

    public abstract void EnumerateAttributes( CompilationModel compilation, Action<AttributeRef> add );

    public abstract void EnumerateAllImplementedInterfaces( Action<IFullRef<INamedType>> add );

    public abstract void EnumerateImplementedInterfaces( Action<IFullRef<INamedType>> add );

    public abstract IEnumerable<IFullRef> GetMembersOfName( string name, DeclarationKind kind, CompilationModel compilation );

    public abstract IEnumerable<IFullRef> GetMembers( DeclarationKind kind, CompilationModel compilation );
}