// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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