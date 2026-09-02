// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Utilities;
using Metalama.Testing.UnitTesting;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class CompileTimeTypeTests : UnitTestClass
    {
        /// <summary>
        /// Matches the assembly qualification that <see cref="Type.FullName"/> appends to each generic type
        /// argument, for instance
        /// <c>, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e</c>.
        /// </summary>
        /// <remarks>
        /// The name and the version of the assembly are part of the pattern rather than of a literal, because both
        /// follow the runtime that executes the test: <c>mscorlib</c> on .NET Framework, <c>System.Private.CoreLib</c>
        /// on .NET, and a version that changes with every major release.
        /// </remarks>
        private static readonly Regex _assemblyQualification =
            new( @", [\w.]+, Version=[\d.]+, Culture=\w+, PublicKeyToken=\w+" );

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

            var expectedTypeName = _assemblyQualification.Replace( type.FullName.AssertNotNull(), "" )
                .ReplaceOrdinal( "[[", "[" )
                .ReplaceOrdinal( "]]", "]" );

            Assert.Equal( expectedTypeName, compileTimeType.FullName );

            var resolvedType = compileTimeType.ToRef().GetTarget( compilation );

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