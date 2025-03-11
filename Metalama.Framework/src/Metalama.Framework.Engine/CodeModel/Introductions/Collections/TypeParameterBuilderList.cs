// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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