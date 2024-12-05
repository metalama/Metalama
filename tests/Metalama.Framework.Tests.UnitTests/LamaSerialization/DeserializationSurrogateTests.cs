// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization;

public sealed class DeserializationSurrogateTests : SerializationTestsBase
{
    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        services.AddProjectService( new SurrogateProvider(), true );
    }

    [Fact]
    public void Test()
    {
        var deserialized = this.TestSerialization<IInterface>( new SerializationStruct( "id" ), testEquality: false );
        Assert.IsType<DeserializationClass>( deserialized );
        Assert.Equal( "id", deserialized.Id );
    }

    private sealed class SurrogateProvider : IDeserializationSurrogateProvider
    {
        public bool TryGetDeserializationSurrogate( string typeName, [NotNullWhen( true )] out Type? surrogateType )
        {
            if ( typeName == typeof(SerializationStruct).FullName )
            {
                surrogateType = typeof(DeserializationClass);

                return true;
            }
            else
            {
                surrogateType = null;

                return false;
            }
        }
    }

    private interface IInterface
    {
        string Id { get; }
    }

    private struct SerializationStruct : IInterface
    {
        public string Id { get; }

        public SerializationStruct( string id )
        {
            this.Id = id;
        }

        [UsedImplicitly]
        public class Serializer : ValueTypeSerializer<SerializationStruct>
        {
            public override void SerializeObject( SerializationStruct obj, IArgumentsWriter constructorArguments )
            {
                constructorArguments.SetValue( "id", obj.Id );
            }

            public override SerializationStruct DeserializeObject( IArgumentsReader constructorArguments ) => throw new NotImplementedException();
        }
    }

    private sealed class DeserializationClass : IInterface
    {
        public string Id { get; }

        private DeserializationClass( string id )
        {
            this.Id = id;
        }

        [UsedImplicitly]
        public class Serializer : ReferenceTypeSerializer<DeserializationClass>
        {
            public override DeserializationClass CreateInstance( IArgumentsReader constructorArguments )
                => new( constructorArguments.GetValue<string>( "id" ) );

            public override void SerializeObject( DeserializationClass obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
                => constructorArguments.SetValue( "id", obj.Id );

            public override void DeserializeFields( DeserializationClass obj, IArgumentsReader initializationArguments ) { }
        }
    }
}