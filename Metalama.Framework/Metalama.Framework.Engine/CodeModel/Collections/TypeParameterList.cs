// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Collections
{
    internal sealed class TypeParameterList : DeclarationCollection<ITypeParameter>, ITypeParameterList
    {
        public static TypeParameterList Empty { get; } = new();

        private TypeParameterList() { }

        public TypeParameterList( INamedType declaringType, IReadOnlyList<IFullRef<ITypeParameter>> sourceItems ) : base(
            declaringType,
            sourceItems ) { }

        public TypeParameterList( IMethod declaringType, IReadOnlyList<IFullRef<ITypeParameter>> sourceItems ) : base(
            declaringType,
            sourceItems ) { }

        public ITypeParameter this[ int index ] => this.Source[index].GetTarget( this.Compilation );
    }
}