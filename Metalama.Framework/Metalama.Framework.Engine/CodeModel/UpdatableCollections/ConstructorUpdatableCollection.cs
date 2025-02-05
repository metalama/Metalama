// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal sealed class ConstructorUpdatableCollection : NonUniquelyNamedUpdatableCollection<IConstructor>
{
    public ConstructorUpdatableCollection( CompilationModel compilation, IFullRef<INamedType> declaringType ) : base(
        compilation,
        declaringType.As<INamespaceOrNamedType>() ) { }

    // TODO: define implicit constructor
    protected override IEqualityComparer<IRef<IConstructor>> MemberRefComparer => RefEqualityComparer<IConstructor>.Default;

    protected override DeclarationKind ItemsDeclarationKind => DeclarationKind.Constructor;
}