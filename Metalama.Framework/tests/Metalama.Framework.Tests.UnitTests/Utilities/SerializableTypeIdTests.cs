// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using SymbolEqualityComparer = Microsoft.CodeAnalysis.SymbolEqualityComparer;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

public sealed class SerializableTypeIdTests : UnitTestClass
{
    public SerializableTypeIdTests( ITestOutputHelper? logger ) : base( logger ) { }

    [Theory]
    [InlineData( typeof(int) )]
    [InlineData( typeof(void) )]
    [InlineData( typeof(object) )]
    [InlineData( typeof(object[]) )]
    [InlineData( typeof(int*) )]
    [InlineData( typeof(int[]) )]
    [InlineData( typeof(decimal) )]
    [InlineData( typeof(List<decimal>) )]
    [InlineData( typeof(List<int[]>) )]
    [InlineData( typeof(List<>) )]
    [InlineData( typeof((int, string)) )]
    [InlineData( typeof(Dictionary<,>) )]
    [InlineData( typeof(Dictionary<List<string>, List<int>>) )]
    public void TestTypeOf( Type type )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( "" );

        var iType = compilation.Factory.GetTypeByReflectionType( type );

        foreach ( var bypassSymbols in new[] { false, true } )
        {
            var id = iType.GetSerializableTypeId( bypassSymbols );
            this.TestOutput.WriteLine( id.Id );

            var roundTripSymbol = compilation.CompilationContext.SerializableTypeIdResolver.ResolveId( id );
            Assert.Equal( iType.GetSymbol(), roundTripSymbol, SymbolEqualityComparer.Default );

            var roundTripType = compilation.SerializableTypeIdResolver.ResolveId( id );
            Assert.Same( iType, roundTripType );
        }
    }

    [Theory]
    [InlineData( "object" )]
    [InlineData( "object?" )]
    [InlineData( "Task<object>" )]
    [InlineData( "Task<object?>" )]
    public void TestNullableType( string type )
    {
        using var testContext = this.CreateTestContext();

        var code = $"using System.Threading.Tasks;"
                   + $"class C {{ {type} f; }}";

        var compilation = testContext.CreateCompilationModel( code );

        var iType = compilation.Types.Single().Fields.Single().Type;

        foreach ( var bypassSymbols in new[] { false, true } )
        {
            var typeId = iType.GetSerializableTypeId( bypassSymbols );

            var roundTripSymbol = compilation.CompilationContext.SerializableTypeIdResolver.ResolveId( typeId );
            Assert.Equal( iType.GetSymbol(), roundTripSymbol, SymbolEqualityComparer.IncludeNullability );

            var roundTripType = compilation.SerializableTypeIdResolver.ResolveId( typeId );
            Assert.Same( iType, roundTripType );
        }
    }

    [Theory]
    [InlineData( "Y:x" )]
    [InlineData( "Y:+" )]
    [InlineData( "Y:List<x>" )]
    public void TestInvalidString( string s )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCompilationModel( "" );

        // We are testing that the method gracefully fails.
        Assert.False( compilation.CompilationContext.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( s ), out _ ) );
        Assert.False( compilation.SerializableTypeIdResolver.TryResolveId( new SerializableTypeId( s ), out _ ) );
    }
}