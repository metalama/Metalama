// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class TypedConstantTests : UnitTestClass
    {
        protected override void ConfigureServices( IAdditionalServiceCollection services ) => services.AddProjectService( SyntaxGenerationOptions.Formatted );

        [Fact]
        public void Unassigned()
        {
            TypedConstant c = default;
            Assert.False( c.IsInitialized );
            Assert.Throws<InvalidOperationException>( () => c.Type );
            Assert.Throws<InvalidOperationException>( () => c.Value );
            Assert.Throws<InvalidOperationException>( () => c.IsNullOrDefault );
        }

        [Fact]
        public void Assigned()
        {
            using var testContext = this.CreateTestContext();

            var emptyCompilation = testContext.CreateCompilationModel( "" );
            var c = TypedConstant.Create( 1, emptyCompilation.Factory.GetSpecialType( SpecialType.Int32 ) );
            Assert.True( c.IsInitialized );
            Assert.NotNull( c.Type );
            Assert.NotNull( c.Value );
            Assert.False( c.IsNullOrDefault );
        }

        [Fact]
        public void Null()
        {
            using var testContext = this.CreateTestContext();

            var emptyCompilation = testContext.CreateCompilationModel( "" );
            var c = TypedConstant.Create( null, emptyCompilation.Factory.GetSpecialType( SpecialType.String ) );
            Assert.True( c.IsInitialized );
            Assert.NotNull( c.Type );
            Assert.Null( c.Value );
            Assert.True( c.IsNullOrDefault );
        }

#pragma warning disable SA1139
        [Theory]
        [InlineData( (byte) 1 )]
        [InlineData( (sbyte) 1 )]
        [InlineData( (short) 1 )]
        [InlineData( (ushort) 1 )]
        [InlineData( 1 )]
        [InlineData( (uint) 1 )]
        [InlineData( (long) 1 )]
        [InlineData( (ulong) 1 )]
        [InlineData( "" )]
#pragma warning restore SA1139
        public void CreateFromValue( object value )
        {
            using var testContext = this.CreateTestContext();

            var emptyCompilation = testContext.CreateCompilationModel( "" );

            using var userCodeContext = testContext.WithExecutionContext( emptyCompilation );

            var c = TypedConstant.Create( value );

            Assert.Equal( value, c.Value );
            Assert.Equal( emptyCompilation.Factory.GetTypeByReflectionType( value.GetType() ), c.Type );
        }

#pragma warning disable SA1139
        [Theory]
        [InlineData( new[] { (byte) 1, (byte) 2 } )]
        [InlineData( new[] { (sbyte) 1 } )]
        [InlineData( new[] { (short) 1 } )]
        [InlineData( new[] { (ushort) 1 } )]
        [InlineData( new[] { 1 } )]
        [InlineData( new[] { (uint) 1 } )]
        [InlineData( new[] { (long) 1 } )]
        [InlineData( new[] { (ulong) 1 } )]
        [InlineData( new object[] { new[] { "" } } )]
        [InlineData( new object[] { new[] { typeof(int) } } )]
        [InlineData( new[] { ConsoleColor.Blue } )]
#pragma warning restore SA1139
        public void CreateFromValueArray( Array value )
        {
            using var testContext = this.CreateTestContext();

            var emptyCompilation = testContext.CreateCompilationModel( "" );

            using var userCodeContext = testContext.WithExecutionContext( emptyCompilation );

            var immutableValue = value.Cast<object>().Select( TypedConstant.Create ).ToImmutableArray();

            var c = TypedConstant.Create( value );

            Assert.Equal( immutableValue, c.RawValue );
            Assert.Equal( emptyCompilation.Factory.GetTypeByReflectionType( value.GetType() ), c.Type );

            var ci = TypedConstant.Create( immutableValue, value.GetType() );

            Assert.Equal( immutableValue, ci.RawValue );
            Assert.Equal( emptyCompilation.Factory.GetTypeByReflectionType( value.GetType() ), ci.Type );
        }

        [Fact]
        public void CreateFromValueEnum()
        {
            using var testContext = this.CreateTestContext();

            var emptyCompilation = testContext.CreateCompilationModel( "" );

            using var userCodeContext = testContext.WithExecutionContext( emptyCompilation );

            var c = TypedConstant.Create( ConsoleColor.Blue );

            Assert.Equal( (int) ConsoleColor.Blue, c.Value );
            Assert.Equal( emptyCompilation.Factory.GetTypeByReflectionType( typeof(ConsoleColor) ), c.Type );
        }

        [Fact]
        public void CreateFromValueType()
        {
            using var testContext = this.CreateTestContext();

            var emptyCompilation = testContext.CreateCompilationModel( "" );

            using var userCodeContext = testContext.WithExecutionContext( emptyCompilation );

            var c = TypedConstant.Create( typeof(int) );

            Assert.Equal( emptyCompilation.Factory.GetTypeByReflectionType( typeof(int) ), c.Value );
            Assert.Equal( emptyCompilation.Factory.GetTypeByReflectionType( typeof(Type) ), c.Type );
        }

#pragma warning disable SA1139
        [Theory]
        [InlineData( (byte) 1, typeof(byte) )]
        [InlineData( (sbyte) 1, typeof(sbyte) )]
        [InlineData( (short) 1, typeof(short) )]
        [InlineData( (ushort) 1, typeof(ushort) )]
        [InlineData( 1, typeof(int) )]
        [InlineData( (uint) 1, typeof(uint) )]
        [InlineData( (long) 1, typeof(long) )]
        [InlineData( (ulong) 1, typeof(ulong) )]
        [InlineData( "", typeof(string) )]
#pragma warning restore SA1139
        public void CreateFromValueTyped( object value, Type type )
        {
            using var testContext = this.CreateTestContext();

            var emptyCompilation = testContext.CreateCompilationModel( "" );

            using var userCodeContext = testContext.WithExecutionContext( emptyCompilation );

            var c = TypedConstant.Create( value, type );

            Assert.Equal( value, c.Value );
            Assert.Equal( emptyCompilation.Factory.GetTypeByReflectionType( type ), c.Type );
        }

#pragma warning disable SA1139
        [Theory]
        [InlineData( (byte) 1, typeof(byte) )]
        [InlineData( (sbyte) 1, typeof(sbyte) )]
        [InlineData( (short) 1, typeof(short) )]
        [InlineData( (ushort) 1, typeof(ushort) )]
        [InlineData( 1, typeof(int) )]
        [InlineData( (uint) 1, typeof(uint) )]
        [InlineData( (long) 1, typeof(long) )]
        [InlineData( (ulong) 1, typeof(ulong) )]
        [InlineData( "", typeof(string) )]
#pragma warning restore SA1139
        public void CreateFromValueSingleItemArray( object value, Type type )
        {
            using var testContext = this.CreateTestContext();

            var emptyCompilation = testContext.CreateCompilationModel( "" );

            using var userCodeContext = testContext.WithExecutionContext( emptyCompilation );

            var array = Array.CreateInstance( type, 1 );
            array.SetValue( value, 0 );
            var c = TypedConstant.Create( array );

            Assert.Equal( array.Cast<object>().Select( TypedConstant.Create ).ToImmutableArray(), c.RawValue );
            Assert.Equal( emptyCompilation.Factory.GetTypeByReflectionType( array.GetType() ), c.Type );
        }

#pragma warning disable SA1139
        [Theory]
        [InlineData( (byte) 1, typeof(byte?) )]
        [InlineData( (sbyte) 1, typeof(sbyte?) )]
        [InlineData( (short) 1, typeof(short?) )]
        [InlineData( (ushort) 1, typeof(ushort?) )]
        [InlineData( 1, typeof(int?) )]
        [InlineData( (uint) 1, typeof(uint?) )]
        [InlineData( (long) 1, typeof(long?) )]
        [InlineData( (ulong) 1, typeof(ulong?) )]
#pragma warning restore SA1139
        public void CreateFromValueTypedNullable( object value, Type type )
        {
            using var testContext = this.CreateTestContext();

            var emptyCompilation = testContext.CreateCompilationModel( "" );

            using var userCodeContext = testContext.WithExecutionContext( emptyCompilation );

            var c = TypedConstant.Create( value, type );

            Assert.Equal( value, c.Value );
            Assert.Equal( emptyCompilation.Factory.GetTypeByReflectionType( type ), c.Type );
        }

        [Fact]
        public void CreateFromValueDecimal()
        {
            using var testContext = this.CreateTestContext();
            var emptyCompilation = testContext.CreateCompilationModel( "" );
            using var userCodeContext = testContext.WithExecutionContext( emptyCompilation );

            const decimal value = 2013.0m;

            var c = TypedConstant.Create( value );

            Assert.Equal( value, c.Value );
            Assert.Equal( emptyCompilation.Factory.GetTypeByReflectionType( typeof(decimal) ), c.Type );
        }

        [Fact]
        public void CreateFromValueInvalid()
        {
            using var testContext = this.CreateTestContext();
            var emptyCompilation = testContext.CreateCompilationModel( "" );
            using var userCodeContext = testContext.WithExecutionContext( emptyCompilation );

            Assert.Throws<ArgumentException>( () => TypedConstant.Create( "", typeof(object) ) );
            Assert.Throws<ArgumentException>( () => TypedConstant.Create( new DateTime( 2023, 5, 3 ) ) );
            Assert.Throws<ArgumentException>( () => TypedConstant.Create( new StringBuilder( "http://metalama.net" ) ) );
        }

        [Theory]
        [InlineData( typeof(int) )]
        [InlineData( typeof(int?) )]
        [InlineData( typeof(int[]) )]
        [InlineData( typeof(ConsoleColor) )]
        [InlineData( typeof(Type) )]
        public void CreateFromValueMismatchedType( Type type )
        {
            using var testContext = this.CreateTestContext();
            var emptyCompilation = testContext.CreateCompilationModel( "" );
            using var userCodeContext = testContext.WithExecutionContext( emptyCompilation );

            var ex = Assert.Throws<ArgumentException>( () => TypedConstant.Create( new object(), type ) );
            Assert.Contains( "The value should be of type", ex.Message, StringComparison.Ordinal );
        }

        [Theory]
        [InlineData( (byte) 1 )]
        [InlineData( (sbyte) 1 )]
        [InlineData( (short) 1 )]
        [InlineData( (ushort) 1 )]
        [InlineData( 1 )]
        [InlineData( (uint) 1 )]
        [InlineData( (long) 1 )]
        [InlineData( (ulong) 1 )]
        [InlineData( (string) "1" )]
        [InlineData( new[] { 1, 2, 3 } )]
        [InlineData( [new object?[] { 4, 5, null }] )]
        public void RoundtripValue( object? value )
        {
            using var testContext = this.CreateTestContext();
            var emptyCompilation = testContext.CreateCompilationModel( "" );
            using var userCodeContext = testContext.WithExecutionContext( emptyCompilation );

            var typedConstant = TypedConstant.Create( value );
            var roundtrip = typedConstant.Value;

            Assert.Equal( value, roundtrip );
            Assert.Equal( value.GetType(), roundtrip.GetType() );
        }
    }
}