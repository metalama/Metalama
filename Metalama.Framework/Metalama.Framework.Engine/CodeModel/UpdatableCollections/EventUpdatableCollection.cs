// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal sealed class EventUpdatableCollection : UniquelyNamedUpdatableCollection<IEvent>
{
    public EventUpdatableCollection( CompilationModel compilation, IFullRef<INamedType> declaringType ) : base(
        compilation,
        declaringType.As<INamespaceOrNamedType>() ) { }

    protected override DeclarationKind ItemsDeclarationKind => DeclarationKind.Event;
}