// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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