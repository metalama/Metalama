// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class TypeParameterBuilderList : List<TypeParameterBuilder>, ITypeParameterList
{
    public static TypeParameterBuilderList Empty { get; } = new();

    IEnumerator<ITypeParameter> IEnumerable<ITypeParameter>.GetEnumerator() => this.GetEnumerator();

    ITypeParameter IReadOnlyList<ITypeParameter>.this[ int index ] => this[index];

    // This is to avoid ambiguities in extension methods because this class implements several IEnumerable<>
    [PublicAPI]
    public IList<TypeParameterBuilder> AsBuilderList => this;

    public ImmutableArray<TypeParameterBuilderData> ToImmutable( IFullRef<IDeclaration> containingDeclaration )
    {
        if ( this.Count == 0 )
        {
            return ImmutableArray<TypeParameterBuilderData>.Empty;
        }
        else
        {
            return this.SelectAsImmutableArray<ITypeParameter, TypeParameterBuilderData>(
                t => new TypeParameterBuilderData( (TypeParameterBuilder) t, containingDeclaration ) );
        }
    }
}