// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Helpers;
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

    public IEnumerable<IAttribute> OfAttributeType( IType type ) => this.OfAttributeType( type, ConversionKind.Default );

    public IEnumerable<IAttribute> OfAttributeType( IType type, ConversionKind conversionKind )
        => ((IEnumerable<AttributeBuilder>) this).Where( a => a.Type.IsConvertibleTo( type, conversionKind ) );

    public IEnumerable<IAttribute> OfAttributeType( Type type ) => this.OfAttributeType( type, ConversionKind.Default );

    public IEnumerable<IAttribute> OfAttributeType( Type type, ConversionKind conversionKind )
    {
        if ( this.Count == 0 )
        {
            return [];
        }

        var compilation = this[0].ContainingDeclaration.GetCompilationModel();
        var namedType = (INamedType) compilation.Factory.GetTypeByReflectionType( type );

        return this.OfAttributeType( namedType, conversionKind );
    }

    public IEnumerable<IAttribute> OfAttributeType( Func<IType, bool> predicate ) => ((IEnumerable<AttributeBuilder>) this).Where( a => predicate( a.Type ) );

    public IEnumerable<T> GetConstructedAttributesOfType<T>()
        where T : Attribute
        => this.OfAttributeType( typeof(T) ).Select( a => a.Construct<T>() );

    public bool Any( IType type ) => this.Any( type, ConversionKind.Default );

    public bool Any( IType type, ConversionKind conversionKind ) => this.OfAttributeType( type, conversionKind ).Any();

    public bool Any( Type type ) => this.Any( type, ConversionKind.Default );

    public bool Any( Type type, ConversionKind conversionKind ) => this.OfAttributeType( type, conversionKind ).Any();

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