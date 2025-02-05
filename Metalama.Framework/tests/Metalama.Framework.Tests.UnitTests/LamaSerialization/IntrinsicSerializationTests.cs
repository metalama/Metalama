// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Serialization;
using System;
using Xunit;

// Resharper disable MemberCanBePrivate.Global

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization
{
    public sealed class IntrinsicSerializationTests : SerializationTestsBase
    {
        [Fact]
        public void TestInt32_0()
        {
            this.TestSerialization( 0 );
        }

        [Fact]
        public void TestInt32_1()
        {
            this.TestSerialization( 1 );
        }

        [Fact]
        public void TestInt32_m1()
        {
            this.TestSerialization( -1 );
        }

        [Fact]
        public void TestInt32_1000()
        {
            this.TestSerialization( 1000 );
        }

        [Fact]
        public void TestInt32_m1000()
        {
            this.TestSerialization( -1000 );
        }

        [Fact]
        public void TestInt32_100000()
        {
            this.TestSerialization( 100000 );
        }

        [Fact]
        public void TestInt32_m100000()
        {
            this.TestSerialization( -100000 );
        }

        [Fact]
        public void TestInt32_10000000()
        {
            this.TestSerialization( 10000000 );
        }

        [Fact]
        public void TestInt32_m10000000()
        {
            this.TestSerialization( -10000000 );
        }

        [Fact]
        public void TestInt32_Max()
        {
            this.TestSerialization( int.MaxValue );
        }

        [Fact]
        public void TestInt32_Min()
        {
            this.TestSerialization( int.MinValue );
        }

        [Fact]
        public void TestString_SimpleWords()
        {
            this.TestSerialization( "SimpleWords" );
        }

        [Fact]
        public void TestDottedString_SimpleDottedWords()
        {
            this.TestSerialization( (DottedString) "Simple.Dotted.Words" );
            this.TestSerialization( new DottedString[] { "A", "A.B", "A.B.C", "A", "A.B" } );
        }

        [Fact]
        public void TestDottedString_DottedWordsWithReservedNames()
        {
            this.TestSerialization( (DottedString) "Simple.Dotted.Words, mscorlib" );
        }

        [Fact]
        public void TestDottedString_NullAndEmpty()
        {
            this.TestSerialization( (DottedString?) null );
            this.TestSerialization( (DottedString) "" );
        }

        [Fact]
        public void TestStruct_DateTime()
        {
            this.TestSerialization( DateTime.Now );
        }

        [Fact]
        public void TestBoolean_True()
        {
            this.TestSerialization( true );
        }

        [Fact]
        public void TestBoolean_False()
        {
            this.TestSerialization( false );
        }

        [Fact]
        public void TestBoxedBoolean_False()
        {
            this.TestSerialization( (object) false );
        }

        [Fact]
        public void TestByte()
        {
            this.TestSerialization( (byte) 0 );
            this.TestSerialization( (byte) 1 );
            this.TestSerialization( (byte) 255 );
        }

        [Fact]
        public void TestChar()
        {
            this.TestSerialization( 'a' );
            this.TestSerialization( (char) 0 );
            this.TestSerialization( (char) 255 );
            this.TestSerialization( (char) 65511 );
        }

        [Fact]
        public void TestDateTime()
        {
            this.TestSerialization( DateTime.Now );
            this.TestSerialization( DateTime.MinValue );
            this.TestSerialization( DateTime.MaxValue );
        }

        [Fact]
        public void TestDecimal()
        {
            this.TestSerialization( decimal.Zero );
            this.TestSerialization( -99999999m );
            this.TestSerialization( 999999m );
            this.TestSerialization( decimal.MaxValue );
            this.TestSerialization( decimal.MinValue );
            this.TestSerialization( decimal.One );
            this.TestSerialization( decimal.MinusOne );
        }

        [Fact]
        public void TestString()
        {
            this.TestSerialization( (string?) null );
            this.TestSerialization( "test" );
            this.TestSerialization( string.Empty );
        }

        [Fact]
        public void TestDouble()
        {
            this.TestSerialization( 0.0 );
            this.TestSerialization( -1.0 );
            this.TestSerialization( 1.0 );
            this.TestSerialization( double.MinValue );
            this.TestSerialization( double.MaxValue );
            this.TestSerialization( double.NaN );
            this.TestSerialization( double.NegativeInfinity );
            this.TestSerialization( double.PositiveInfinity );
        }

        [Fact]
        public void TestSingle()
        {
            this.TestSerialization( 0.0f );
            this.TestSerialization( -1.0f );
            this.TestSerialization( 1.0f );
            this.TestSerialization( float.MinValue );
            this.TestSerialization( float.MaxValue );
            this.TestSerialization( float.NaN );
            this.TestSerialization( float.NegativeInfinity );
            this.TestSerialization( float.PositiveInfinity );
        }

        [Fact]
        public void TestGuid()
        {
            this.TestSerialization( Guid.Empty );
            this.TestSerialization( Guid.NewGuid() );
        }

        [Fact]
        public void TestInt16()
        {
            this.TestSerialization( short.MinValue );
            this.TestSerialization( short.MaxValue );
            this.TestSerialization( (short) 0 );
        }

        [Fact]
        public void TestInt64_223372036854775807()
        {
            this.TestSerialization( 223372036854775807 );
        }

        [Fact]
        public void TestInt64_m223372036854775807()
        {
            this.TestSerialization( -223372036854775807 );
        }

        [Fact]
        public void TestInt64()
        {
            this.TestSerialization( long.MinValue );
            this.TestSerialization( long.MaxValue );
            this.TestSerialization( 0L );
        }

        [Fact]
        public void TestUInt16()
        {
            this.TestSerialization( ushort.MinValue );
            this.TestSerialization( ushort.MaxValue );
            this.TestSerialization( (ushort) 0 );
        }

        [Fact]
        public void TestUInt64()
        {
            this.TestSerialization( ulong.MinValue );
            this.TestSerialization( ulong.MaxValue );
            this.TestSerialization( 0UL );
        }

        [Fact]
        public void TestUInt32()
        {
            this.TestSerialization( uint.MinValue );
            this.TestSerialization( uint.MaxValue );
            this.TestSerialization( 0U );
        }

        [Fact]
        public void TestNullableInt()
        {
            this.TestSerialization( (int?) 0 );
            this.TestSerialization( (int?) int.MaxValue );
            this.TestSerialization( (int?) int.MinValue );
            this.TestSerialization( (int?) null );
        }

        [Fact]
        public void TestNullableEnum()
        {
            this.TestSerialization( (TypeCode?) TypeCode.Boolean );
            this.TestSerialization( (TypeCode?) null );
        }

        [Fact]
        public void TestArray_Ints()
        {
            this.TestSerialization( new[] { 1, int.MinValue, 9, int.MaxValue } );
        }

        [Fact]
        public void TestArray_Objects()
        {
            this.TestSerialization( new[] { new SimpleType(), new SimpleType(), new SimpleType(), new SimpleType() } );
        }

        [Fact]
        public void TestArray_Structs()
        {
            this.TestSerialization( new[] { DateTime.Now, DateTime.Today, DateTime.MinValue, DateTime.MaxValue } );
        }

#if !SILVERLIGHT
        [Fact]
        public void TestArray_WithLowerBoundNEqZero()
        {
            var array = Array.CreateInstance( typeof(int), new[] { 5 }, new[] { 5 } ); // int[5..10]

            array.SetValue( 0, 5 );
            array.SetValue( 1, 6 );
            array.SetValue( 2, 7 );
            array.SetValue( 3, 8 );
            array.SetValue( 4, 9 );

            this.TestSerialization( array );
        }
#endif

        // [Fact]
        //        public void TestArray_Generics()
        //        {
        //            TestValue( new[] {new Nullable<int>(4), new Nullable<int>(int.MinValue), new Nullable<int>(int.MaxValue)} );
        //        }

        [Fact]
        public void TestArray_BoxedStructs()
        {
            this.TestSerialization( new object[] { DateTime.Now, 11, false, "test", 0.0 } );
        }

        [Fact]
        public void TestArray_WithNulls()
        {
            this.TestSerialization( new object[2] );
        }

        [Fact]
        public void TestBoxedIntrinsics()
        {
            this.TestSerialization( (object) 5 );
            this.TestSerialization( (object) -13.0 );
            this.TestSerialization( (object) "test" );
            this.TestSerialization( (object) DateTime.Now );
        }

        public sealed class SimpleType : IEquatable<SimpleType>
        {
            public string? Name { get; set; }

            public bool Equals( SimpleType? other )
            {
                if ( ReferenceEquals( null, other ) )
                {
                    return false;
                }

                if ( ReferenceEquals( this, other ) )
                {
                    return true;
                }

                return StringComparer.Ordinal.Equals( this.Name, other.Name );
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

                return this.Equals( (SimpleType) obj );
            }

            public override int GetHashCode()
            {
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                var name = this.Name;

                return name != null ? StringComparer.Ordinal.GetHashCode( name ) : 0;
            }

            [UsedImplicitly]
            public class Serializer : ReferenceTypeSerializer
            {
                public override object CreateInstance( Type type, IArgumentsReader constructorArguments )
                {
                    return new SimpleType();
                }

                public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
                {
                    initializationArguments.SetValue( "_", ((SimpleType) obj).Name );
                }

                public override void DeserializeFields( object obj, IArgumentsReader initializationArguments )
                {
                    ((SimpleType) obj).Name = initializationArguments.GetValue<string>( "_" );
                }
            }
        }
    }
}