// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class CompileTimeTypeTests : UnitTestClass
    {
        [Theory]
        [InlineData( typeof(Task) )]
        [InlineData( typeof(Task<>) )]
        [InlineData( typeof(Task<int>) )]
        [InlineData( typeof(Task[]) )]
        [InlineData( typeof(Task<int>[]) )]
        [InlineData( typeof(Task<int[]>) )]
        public void TestResolution( Type type )
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "/* Intentionally empty */" );
            var compilationServices = compilation.CompilationContext;

            var reflectionMapper = new ReflectionMapper( compilation.RoslynCompilation );
            var typeSymbol = reflectionMapper.GetTypeSymbol( type );

            var compileTimeType = compilationServices.CompileTimeTypeFactory.Get( typeSymbol );

            var expectedTypeName = type.FullName.AssertNotNull()
#if NET5_0_OR_GREATER
                .ReplaceOrdinal( ", System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", "" )
#else
                .ReplaceOrdinal( ", mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "" )
#endif
                .ReplaceOrdinal( "[[", "[" )
                .ReplaceOrdinal( "]]", "]" );

            Assert.Equal( expectedTypeName, compileTimeType.FullName );

            var resolvedType = compileTimeType.Target.GetTarget( compilation );

            Assert.NotNull( resolvedType );
        }

        [Theory]
        [InlineData( typeof(int), typeof(int), true )]
        [InlineData( typeof(int), typeof(long), false )]
        [InlineData( typeof(Task<>), typeof(Task<>), true )]
        [InlineData( typeof(Task<>), typeof(Task<int>), false )]
        public void TestEquality( Type a, Type b, bool expectedEqual )
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCompilationModel( "/* Intentionally empty */" );
            var compilationServices = compilation.CompilationContext;

            var reflectionMapper = new ReflectionMapper( compilation.RoslynCompilation );

            var compileTimeTypeA = compilationServices.CompileTimeTypeFactory.Get( reflectionMapper.GetTypeSymbol( a ) );
            var compileTimeTypeB = compilationServices.CompileTimeTypeFactory.Get( reflectionMapper.GetTypeSymbol( b ) );

            var typesAreEqual = compileTimeTypeA.Equals( compileTimeTypeB );

            Assert.Equal( expectedEqual, typesAreEqual );

            if ( typesAreEqual )
            {
                // Hash codes must be equal too.
                Assert.Equal( compileTimeTypeA.GetHashCode(), compileTimeTypeB.GetHashCode() );
            }
        }
    }
}