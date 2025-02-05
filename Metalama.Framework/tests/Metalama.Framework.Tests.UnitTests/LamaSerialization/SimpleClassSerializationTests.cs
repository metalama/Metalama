// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Xunit;

// ReSharper disable MemberCanBeInternal
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization
{
    public sealed class SimpleClassSerializationTests : SerializationTestsBase
    {
        [Fact]
        public void TestClassWithString_SimpleWord()
        {
            this.TestSimpleExplicitlySerializedClass( "SimpleText" );
        }

        [Fact]
        public void TestClassWithString_SimpleDottedWords()
        {
            this.TestSimpleExplicitlySerializedClass( "Simple.Dotted.Words" );
        }

        [Fact]
        public void TestClassWithString_DottedWordsWithReservedNames()
        {
            this.TestSimpleExplicitlySerializedClass( "Simple.Dotted.Words, mscorlib" );
        }

        [Fact]
        public void TestClassWithBoxedInt()
        {
            this.TestSimpleExplicitlySerializedClass<object>( 1000 );
        }

        [Fact]
        public void TestClassWithBoxedBool()
        {
            this.TestSimpleExplicitlySerializedClass<object>( false );
        }

        [Fact]
        public void TestClassWithBoxedStruct()
        {
            this.TestSimpleExplicitlySerializedClass<object>( DateTime.Now );
        }

        [Fact]
        public void TestClassWithInt32_1000()
        {
            this.TestSimpleExplicitlySerializedClass( 1000 );
        }

        [Fact]
        public void TestClassWithInt32_m1000()
        {
            this.TestSimpleExplicitlySerializedClass( -1000 );
        }

        [Fact]
        public void TestClassWithInt32_1000000000()
        {
            this.TestSimpleExplicitlySerializedClass( 1000000000 );
        }

        [Fact]
        public void TestClassWithInt32_m1000000000()
        {
            this.TestSimpleExplicitlySerializedClass( -1000000000 );
        }

        [Fact]
        public void TestClassWithInt64_223372036854775807()
        {
            this.TestSimpleExplicitlySerializedClass( 223372036854775807 );
        }

        [Fact]
        public void TestClassWithInt64_m223372036854775807()
        {
            this.TestSimpleExplicitlySerializedClass( -223372036854775807 );
        }

        [Fact]
        public void TestClassWithDouble_1000()
        {
            this.TestSimpleExplicitlySerializedClass( 1000d );
        }

        [Fact]
        public void TestClassWithDouble_m1000()
        {
            this.TestSimpleExplicitlySerializedClass( -1000d );
        }

        [Fact]
        public void TestClassWithDouble_Max()
        {
            this.TestSimpleExplicitlySerializedClass( double.MaxValue );
        }

        [Fact]
        public void TestClassWithDouble_Min()
        {
            this.TestSimpleExplicitlySerializedClass( double.MinValue );
        }

        [Fact]
        public void TestClassWithStruct_DateTime()
        {
            this.TestSimpleExplicitlySerializedClass( DateTime.Now );
        }

        [Fact]
        public void TestClasWithObjectMember_Null()
        {
            this.TestSimpleExplicitlySerializedClass( (object?) null );
        }

        [Fact]
        public void TestClasWithStringMember_Null()
        {
            this.TestSimpleExplicitlySerializedClass( (string?) null );
        }

        [Fact]
        public void TestClasWithClassMember_Null()
        {
            this.TestSimpleExplicitlySerializedClass( (SimpleExplicitlySerializedClass<int>?) null );
        }

        [Fact]
        public void TestClasWithClassMember_NotNull()
        {
            this.TestSimpleExplicitlySerializedClass( new SimpleExplicitlySerializedClass<string>( "testing text" ) );
        }

        [Fact]
        public void TestClasWithEnum_SimpleValue()
        {
            this.TestSimpleExplicitlySerializedClass( TestEnum.Value1 );
        }

        [Fact]
        public void TestClasWithEnum_NonZeroValue()
        {
            this.TestSimpleExplicitlySerializedClass( TestEnum.Value2 );
        }

        [Fact]
        public void TestClasWithLongEnum_NonZeroValue()
        {
            this.TestSimpleExplicitlySerializedClass( TestEnumWithLong.Value2 );
        }

        [Fact]
        public void TestClasWithByteEnum_NonZeroValue()
        {
            this.TestSimpleExplicitlySerializedClass( TestEnumWithByte.Value2 );
        }

        [Fact]
        public void TestClasWithFlagsEnum_NonZeroValue()
        {
            this.TestSimpleExplicitlySerializedClass( TestEnumWithFlags.Value2 );
        }

        [Fact]
        public void TestClasWithFlagsEnum_MultiValue()
        {
            this.TestSimpleExplicitlySerializedClass( TestEnumWithFlags.Value1 | TestEnumWithFlags.Value2 );
        }

        [Fact]
        public void TestClassWithProperty_Int1()
        {
            this.TestExplicitlySerializedClass( (object?) null, 1 );
        }

        [Fact]
        public void TestClassWithArray_Int()
        {
            this.TestSimpleExplicitlySerializedClass( new[] { 1, 2, 3, 4 } );
        }

        [Fact]
        public void TestClassWithLargeArray_Int()
        {
            var array = new int[100000];

            for ( var i = 0; i < array.Length; i++ )
            {
                array[i] = i;
            }

            this.TestSimpleExplicitlySerializedClass( array );
        }

        [Fact]
        public void TestClassWithRank2Array()
        {
            this.TestSimpleExplicitlySerializedClass( new[,] { { 1, 2 }, { 3, 4 } } );
        }

        [Fact]
        public void TestClassWithRank3Array()
        {
            this.TestSimpleExplicitlySerializedClass( new[,,] { { { 1 }, { 2 } }, { { 3 }, { 4 } } } );
        }

        [Fact]
        public void TestClassWithMultidimensionalRank1Array()
        {
            // We're creating instance of type int[*], but actually get int[] -> and that's what were testing
            var array = (Array?) Activator.CreateInstance( typeof(int).MakeArrayType( 1 ), 2 );
            array!.SetValue( 1, 0 );
            array.SetValue( 2, 1 );

            this.TestSimpleExplicitlySerializedClass( (int[]) array );
        }

        [Fact]
        public void TestClassWithValueAndProperty_Int1AndInt1()
        {
            this.TestExplicitlySerializedClass( 1, 1 );
        }

        [Fact]
        public void TestClassWithValueAndProperty_StringAndString()
        {
            this.TestExplicitlySerializedClass( "a field", "a property" );
        }

        [Fact]
        public void TestClassWithValueAndProperty_BothNull()
        {
            this.TestExplicitlySerializedClass( (object?) null, (object?) null );
        }

        [Fact]
        public void TestClassWithNullableInt_NotNull()
        {
            this.TestSerialization( new ExplicitlySerializedClass<int, int>( 5 ) { Nullable = 3 } );
        }

        [Fact]
        public void TestHeterogeneousArrayOfReferenceTypeObjects()
        {
            var array = new SimpleExplicitlySerializedClass<int>[3];
            array[0] = new SimpleExplicitlySerializedClass<int>( 5 );
            array[1] = new ExplicitlySerializedClass<int, string>( 2 );

            this.TestSerialization( array );
        }

        [Fact]
        public void GenericTypes_InArray()
        {
            var serializedClass = new SimpleExplicitlySerializedClass<DateTime>( DateTime.Today.AddDays( 10 ) );
            var anotherSerializedClass = new SimpleExplicitlySerializedClass<DateTime>( DateTime.Today.AddMonths( -10 ) );
            var array = new[] { serializedClass, anotherSerializedClass };

            using var testContext = this.CreateTestContext();
            var memoryStream = new MemoryStream();
            testContext.Serializer.Serialize( array, memoryStream );
            memoryStream.Seek( 0, SeekOrigin.Begin );

            var deserializedObject =
                (SimpleExplicitlySerializedClass<DateTime>[]?) testContext.Serializer.Deserialize( memoryStream );

            Assert.NotNull( deserializedObject );
            Assert.Equal( 2, deserializedObject.Length );
            Assert.Equal( serializedClass, deserializedObject[0] );
            Assert.Equal( anotherSerializedClass, deserializedObject[1] );
        }

        private void TestSimpleExplicitlySerializedClass<T>( T value )
        {
            var initialObject = new SimpleExplicitlySerializedClass<T>( value );
            using var testContext = this.CreateTestContext();
            var memoryStream = new MemoryStream();
            testContext.Serializer.Serialize( initialObject, memoryStream );
            memoryStream.Seek( 0, SeekOrigin.Begin );
            var deserializedObject = (SimpleExplicitlySerializedClass<T>?) testContext.Serializer.Deserialize( memoryStream );

            if ( typeof(T).IsArray )
            {
                Assert.Equal( (ICollection?) initialObject.Value, (ICollection?) deserializedObject!.Value );
            }
            else
            {
                Assert.Equal( initialObject.Value, deserializedObject!.Value );
            }
        }

        private void TestExplicitlySerializedClass<TForCtor, TForField>( TForCtor value, TForField property )
        {
            var initialObject = new ExplicitlySerializedClass<TForCtor, TForField>( value ) { Field = property };
            using var testContext = this.CreateTestContext();
            var memoryStream = new MemoryStream();
            testContext.Serializer.Serialize( initialObject, memoryStream );
            memoryStream.Seek( 0, SeekOrigin.Begin );

            var deserializedObject =
                (ExplicitlySerializedClass<TForCtor, TForField>?) testContext.Serializer.Deserialize( memoryStream );

            Assert.Equal( initialObject.Value, deserializedObject!.Value );
            Assert.Equal( initialObject.Field, deserializedObject.Field );
        }

        public class SimpleExplicitlySerializedClass<T> : IEquatable<SimpleExplicitlySerializedClass<T>>
        {
#pragma warning disable SA1401 // Fields should be private
            [UsedImplicitly]
            public T Value;
#pragma warning restore SA1401 // Fields should be private

            public SimpleExplicitlySerializedClass( T value )
            {
                this.Value = value;
            }

            public bool Equals( SimpleExplicitlySerializedClass<T>? other )
            {
                if ( ReferenceEquals( null, other ) )
                {
                    return false;
                }

                if ( ReferenceEquals( this, other ) )
                {
                    return true;
                }

                return EqualityComparer<T>.Default.Equals( this.Value, other.Value );
            }

            public override bool Equals( object? obj )
            {
                if ( ReferenceEquals( null, obj ) )
                {
                    return false;
                }

                if ( ReferenceEquals( this, obj ) )
                {
                    return true;
                }

                if ( obj.GetType() != this.GetType() )
                {
                    return false;
                }

                return this.Equals( (SimpleExplicitlySerializedClass<T>) obj );
            }

            public override int GetHashCode()
            {
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                return EqualityComparer<T>.Default.GetHashCode( this.Value! );
            }

            public class Serializer : ReferenceTypeSerializer<SimpleExplicitlySerializedClass<T>>
            {
                public override SimpleExplicitlySerializedClass<T> CreateInstance( IArgumentsReader constructorArguments )
                    => new( constructorArguments.GetValue<T>( "_" )! );

                public override void SerializeObject(
                    SimpleExplicitlySerializedClass<T> obj,
                    IArgumentsWriter constructorArguments,
                    IArgumentsWriter initializationArguments )
                {
                    constructorArguments.SetValue( "_", obj.Value );
                }

                public override void DeserializeFields( SimpleExplicitlySerializedClass<T> obj, IArgumentsReader initializationArguments ) { }
            }
        }

        public sealed class ExplicitlySerializedClass<TForCtor, TForField> : SimpleExplicitlySerializedClass<TForCtor>,
                                                                             IEquatable<ExplicitlySerializedClass<TForCtor, TForField>>
        {
            public TForField? Field { get; set; }

            public int? Nullable { get; set; }

            public ExplicitlySerializedClass( TForCtor value )
                : base( value ) { }

            public bool Equals( ExplicitlySerializedClass<TForCtor, TForField>? other )
            {
                if ( ReferenceEquals( null, other ) )
                {
                    return false;
                }

                if ( ReferenceEquals( this, other ) )
                {
                    return true;
                }

                return base.Equals( other ) && EqualityComparer<TForField>.Default.Equals( this.Field!, other.Field! ) && this.Nullable == other.Nullable;
            }

            public override bool Equals( object? obj )
            {
                if ( ReferenceEquals( null, obj ) )
                {
                    return false;
                }

                if ( ReferenceEquals( this, obj ) )
                {
                    return true;
                }

                if ( obj.GetType() != this.GetType() )
                {
                    return false;
                }

                return this.Equals( (ExplicitlySerializedClass<TForCtor, TForField>) obj );
            }

            public override int GetHashCode()
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                return HashCode.Combine( base.GetHashCode(), this.Field, this.Nullable );

                // ReSharper restore NonReadonlyMemberInGetHashCode
            }

            public new class Serializer : ReferenceTypeSerializer<ExplicitlySerializedClass<TForCtor, TForField>>
            {
                private const string _valueKey = "_ov";
                private const string _fieldKey = "_fv";

                public override ExplicitlySerializedClass<TForCtor, TForField> CreateInstance( IArgumentsReader constructorArguments )
                {
                    return new ExplicitlySerializedClass<TForCtor, TForField>( constructorArguments.GetValue<TForCtor>( _valueKey )! );
                }

                public override void SerializeObject(
                    ExplicitlySerializedClass<TForCtor, TForField> obj,
                    IArgumentsWriter constructorArguments,
                    IArgumentsWriter initializationArguments )
                {
                    constructorArguments.SetValue( _valueKey, obj.Value );
                    initializationArguments.SetValue( _fieldKey, obj.Field );
                    initializationArguments.SetValue( "n", obj.Nullable );
                }

                public override void DeserializeFields( ExplicitlySerializedClass<TForCtor, TForField> obj, IArgumentsReader initializationArguments )
                {
                    obj.Field = initializationArguments.GetValue<TForField>( _fieldKey );
                    obj.Nullable = initializationArguments.GetValue<int?>( "n" );
                }
            }
        }

        public enum TestEnum
        {
            Value1,

            Value2,

            Value3
        }

        public enum TestEnumWithLong : long
        {
            Value1,

            Value2
        }

        public enum TestEnumWithByte : byte
        {
            Value1,

            Value2
        }

        [Flags]
        public enum TestEnumWithFlags
        {
            Value1 = 0x01,

            Value2 = 0x02
        }
    }
}