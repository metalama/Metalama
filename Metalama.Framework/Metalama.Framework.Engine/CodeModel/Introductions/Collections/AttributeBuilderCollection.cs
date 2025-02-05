// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Collections;

internal sealed class AttributeBuilderCollection : List<AttributeBuilder>, IAttributeCollection
{
    IEnumerator<IAttribute> IEnumerable<IAttribute>.GetEnumerator() => this.GetEnumerator();

    public IEnumerable<IAttribute> OfAttributeType( IType type ) => throw new NotImplementedException();

    public IEnumerable<IAttribute> OfAttributeType( IType type, ConversionKind conversionKind ) => throw new NotImplementedException();

    public IEnumerable<IAttribute> OfAttributeType( Type type ) => throw new NotImplementedException();

    public IEnumerable<IAttribute> OfAttributeType( Type type, ConversionKind conversionKind ) => throw new NotImplementedException();

    public IEnumerable<IAttribute> OfAttributeType( Func<IType, bool> predicate ) => throw new NotImplementedException();

    public IEnumerable<T> GetConstructedAttributesOfType<T>()
        where T : Attribute
        => throw new NotImplementedException();

    public bool Any( IType type ) => throw new NotImplementedException();

    public bool Any( IType type, ConversionKind conversionKind ) => throw new NotImplementedException();

    public bool Any( Type type ) => throw new NotImplementedException();

    public bool Any( Type type, ConversionKind conversionKind ) => throw new NotImplementedException();

    public ImmutableArray<AttributeBuilderData> ToImmutable( IFullRef<IDeclaration> containingDeclaration )
    {
        if ( this.Count == 0 )
        {
            return ImmutableArray<AttributeBuilderData>.Empty;
        }
        else
        {
            return this.SelectAsImmutableArray( a => new AttributeBuilderData( a, containingDeclaration ) );
        }
    }
}