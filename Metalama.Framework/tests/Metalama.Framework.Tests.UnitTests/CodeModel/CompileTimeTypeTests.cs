// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        public void Test( Type type )
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
    }
}