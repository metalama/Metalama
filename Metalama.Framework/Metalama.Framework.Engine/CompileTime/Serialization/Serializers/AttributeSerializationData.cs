// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Serialization;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

internal sealed class AttributeSerializationData
{
    public IRef<IDeclaration> ContainingDeclaration { get; }

    public IRef<INamedType> Type { get; }

    public IRef<IConstructor> Constructor { get; }

    public ImmutableArray<TypedConstantRef> ConstructorArguments { get; }

    public ImmutableArray<KeyValuePair<string, TypedConstantRef>> NamedArguments { get; }

    public AttributeSerializationData( ISymbol symbol, AttributeData attributeData, RefFactory refFactory )
    {
        this.ContainingDeclaration = refFactory.FromDeclarationSymbol( symbol );
        this.Constructor = refFactory.FromSymbol<IConstructor>( attributeData.AttributeConstructor.AssertSymbolNotNull() );
        this.Type = refFactory.FromSymbol<INamedType>( attributeData.AttributeClass.AssertSymbolNotNull() );
        this.ConstructorArguments = attributeData.ConstructorArguments.SelectAsImmutableArray( c => c.ToOurTypedConstantRef( refFactory ) );

        this.NamedArguments =
            attributeData.NamedArguments.SelectAsImmutableArray(
                x => new KeyValuePair<string, TypedConstantRef>( x.Key, x.Value.ToOurTypedConstantRef( refFactory ) ) );
    }

    public AttributeSerializationData( AttributeBuilderData builder )
    {
        this.ContainingDeclaration = builder.ContainingDeclaration;
        this.Constructor = builder.Constructor;
        this.Type = builder.Type;
        this.ConstructorArguments = builder.ConstructorArguments;
        this.NamedArguments = builder.NamedArguments.SelectAsImmutableArray( x => new KeyValuePair<string, TypedConstantRef>( x.Key, x.Value.ToRef() ) );
    }

    public AttributeSerializationData( IArgumentsReader reader )
    {
        this.ContainingDeclaration = reader.GetValue<IRef<IDeclaration>>( nameof(this.ContainingDeclaration) ).AssertNotNull();
        this.Type = reader.GetValue<IRef<INamedType>>( nameof(this.Type) ).AssertNotNull();
        this.Constructor = reader.GetValue<IRef<IConstructor>>( nameof(this.Constructor) ).AssertNotNull();
        this.ConstructorArguments = reader.GetValue<ImmutableArray<TypedConstantRef>>( nameof(this.ConstructorArguments) );

        if ( this.ConstructorArguments.IsDefault )
        {
            this.ConstructorArguments = ImmutableArray<TypedConstantRef>.Empty;
        }

        var namedArgumentKeys = reader.GetValue<string[]>( "NamedArgumentKeys" );
        var namedArgumentValues = reader.GetValue<TypedConstantRef[]>( "NamedArgumentValues" );

        if ( namedArgumentKeys != null && namedArgumentValues != null )
        {
            var namedArguments = ImmutableArray.CreateBuilder<KeyValuePair<string, TypedConstantRef>>( namedArgumentKeys.Length );

            for ( var i = 0; i < namedArgumentKeys.Length; i++ )
            {
                namedArguments.Add( new KeyValuePair<string, TypedConstantRef>( namedArgumentKeys[i], namedArgumentValues[i] ) );
            }

            this.NamedArguments = namedArguments.MoveToImmutable();
        }
        else
        {
            this.NamedArguments = ImmutableArray<KeyValuePair<string, TypedConstantRef>>.Empty;
        }
    }

    public void Serialize( IArgumentsWriter writer )
    {
        writer.SetValue( nameof(this.ContainingDeclaration), this.ContainingDeclaration );
        writer.SetValue( nameof(this.Constructor), this.Constructor );
        writer.SetValue( nameof(this.Type), this.Type );

        if ( this.ConstructorArguments.Length > 0 )
        {
            writer.SetValue( nameof(this.ConstructorArguments), this.ConstructorArguments );
        }

        if ( this.NamedArguments.Length > 0 )
        {
            writer.SetValue( "NamedArgumentKeys", this.NamedArguments.SelectAsArray( n => n.Key ) );
            writer.SetValue( "NamedArgumentValues", this.NamedArguments.SelectAsArray( n => n.Value ) );
        }
    }
}