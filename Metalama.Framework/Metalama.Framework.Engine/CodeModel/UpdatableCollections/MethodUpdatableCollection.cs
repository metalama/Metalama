// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.UpdatableCollections;

internal sealed class MethodUpdatableCollection : NonUniquelyNamedUpdatableCollection<IMethod>
{
    public MethodUpdatableCollection( CompilationModel compilation, IRef<INamedType> declaringType ) : base(
        compilation,
        declaringType ) { }

    protected override IEqualityComparer<IRef<IMethod>> MemberRefComparer => RefEqualityComparer<IMethod>.Default;

    protected override DeclarationKind ItemsDeclarationKind => DeclarationKind.Method;
}