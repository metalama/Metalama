// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Serialization;
using Metalama.Framework.Tests.UnitTests.LamaSerialization;
using Xunit;

// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

// attribute added for testing purposes
[assembly: ImportSerializer( typeof(SerializerLocatorTests.TypeWoSerializer), typeof(SerializerLocatorTests.GenericSerializedClass<>.Serializer) )]

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization
{
    public sealed class SerializerLocatorTests : SerializationTestsBase
    {
        private readonly ISerializerFactoryProvider _customSerializerProvider;

        public SerializerLocatorTests()
        {
            var builtIn = this.ServiceProvider.GetRequiredService<BuiltInSerializerFactoryProvider>();
            Assert.NotNull( builtIn.NextProvider );
            this._customSerializerProvider = builtIn.NextProvider!;
        }

        [Fact]
        public void GetSerializerType_NonGenericCustomType()
        {
            var serializerType = this._customSerializerProvider.GetSerializerFactory( typeof(SerializedClass) )
                ?.CreateSerializer( typeof(SerializedClass) )
                .GetType();

            Assert.Equal( typeof(SerializedClass.Serializer), serializerType );
        }

        [Fact]
        public void GetSerializerType_GenericCustomTypeDefinition_NotNull()
        {
            var serializerType = this._customSerializerProvider.GetSerializerFactory( typeof(GenericSerializedClass<>) )
                ?.CreateSerializer( typeof(GenericSerializedClass<int>) )
                .GetType();

            Assert.NotNull( serializerType );
        }

        [Fact]
        public void GetSerializerType_GenericCustomTypeInstance_Null()
        {
            var serializerType = this._customSerializerProvider.GetSerializerFactory( typeof(GenericSerializedClass<string>) );

            Assert.Null( serializerType );
        }

        [Fact]
        public void GetSerializerType_AskedTwice_TheSameObject()
        {
            var serializerType1 = this._customSerializerProvider.GetSerializerFactory( typeof(GenericSerializedClass<>) )
                ?.CreateSerializer( typeof(GenericSerializedClass<string>) )
                .GetType();

            var serializerType2 = this._customSerializerProvider.GetSerializerFactory( typeof(GenericSerializedClass<>) )
                ?.CreateSerializer( typeof(GenericSerializedClass<string>) )
                .GetType();

            Assert.Same( serializerType1, serializerType2 );
        }

        [Fact]
        public void GetSerializerType_RespectsAssemblyImportAttributes()
        {
            var assemblyImportedSerializerType = this._customSerializerProvider.GetSerializerFactory( typeof(TypeWoSerializer) )
                ?.CreateSerializer( typeof(GenericSerializedClass<string>) )
                .GetType();

            // NOTE: import is just over the namespace opening bracket

            var classAttributeDerivedSerializerType = this._customSerializerProvider.GetSerializerFactory( typeof(GenericSerializedClass<>) )
                ?.CreateSerializer( typeof(GenericSerializedClass<string>) )
                .GetType();

            Assert.NotNull( classAttributeDerivedSerializerType );
            Assert.NotNull( assemblyImportedSerializerType );
            Assert.Same( classAttributeDerivedSerializerType, assemblyImportedSerializerType );
        }

        [Fact]
        public void GetSerializerType_HasManySerializers_Throws()
        {
            Assert.Throws<CompileTimeSerializationException>( () => this._customSerializerProvider.GetSerializerFactory( typeof(TypeWithManySerializers) ) );
        }

        public sealed class SerializedClass
        {
            public sealed class Serializer : ReferenceTypeSerializer<SerializedClass>
            {
                public override SerializedClass CreateInstance( IArgumentsReader constructorArguments )
                {
                    return new SerializedClass();
                }

                public override void SerializeObject(
                    SerializedClass obj,
                    IArgumentsWriter constructorArguments,
                    IArgumentsWriter initializationArguments ) { }

                public override void DeserializeFields( SerializedClass obj, IArgumentsReader initializationArguments ) { }
            }
        }

        public sealed class GenericSerializedClass<T>
        {
            public sealed class Serializer : ReferenceTypeSerializer<GenericSerializedClass<T>>
            {
                public override GenericSerializedClass<T> CreateInstance( IArgumentsReader constructorArguments )
                {
                    return new GenericSerializedClass<T>();
                }

                public override void SerializeObject(
                    GenericSerializedClass<T> obj,
                    IArgumentsWriter constructorArguments,
                    IArgumentsWriter initializationArguments ) { }

                public override void DeserializeFields( GenericSerializedClass<T> obj, IArgumentsReader initializationArguments ) { }
            }
        }

        internal sealed class TypeWoSerializer;

        [ImportSerializer( typeof(TypeWithManySerializers), typeof(SecondSerializer) )]
        public sealed class TypeWithManySerializers
        {
            public class Serializer : ReferenceTypeSerializer<TypeWithManySerializers>
            {
                public override TypeWithManySerializers CreateInstance( IArgumentsReader constructorArguments )
                {
                    return new TypeWithManySerializers();
                }

                public override void SerializeObject(
                    TypeWithManySerializers obj,
                    IArgumentsWriter constructorArguments,
                    IArgumentsWriter initializationArguments ) { }

                public override void DeserializeFields( TypeWithManySerializers obj, IArgumentsReader initializationArguments ) { }
            }

            public sealed class SecondSerializer : ReferenceTypeSerializer<TypeWithManySerializers>
            {
                public override TypeWithManySerializers CreateInstance( IArgumentsReader constructorArguments )
                {
                    return new TypeWithManySerializers();
                }

                public override void SerializeObject(
                    TypeWithManySerializers obj,
                    IArgumentsWriter constructorArguments,
                    IArgumentsWriter initializationArguments ) { }

                public override void DeserializeFields( TypeWithManySerializers obj, IArgumentsReader initializationArguments ) { }
            }
        }
    }
}