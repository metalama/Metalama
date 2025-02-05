// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

public sealed class DerivedTypesTests : UnitTestClass
{
    private const string _code = """
                                 
                                     using System;
                                     using System.IO;
                                 
                                     class NotDerived { }
                                 
                                     class DirectlyDerivedClass : IDisposable { public void Dispose() {} }
                                 
                                     interface IDerivedInterface : IDisposable {}
                                 
                                     class IndirectlyDerivedThroughBaseClass : DirectlyDerivedClass {}
                                 
                                     class IndirectlyDerivedThroughInterface : IDerivedInterface { public void Dispose() {} }
                                 
                                     class MyTextReader : TextReader { }

                                 """;

    [InlineData( DerivedTypesOptions.DirectOnly, "NotDerived", false )]
    [InlineData( DerivedTypesOptions.All, "NotDerived", false )]
    [InlineData( DerivedTypesOptions.FirstLevelWithinCompilationOnly, "NotDerived", false )]
    [InlineData( DerivedTypesOptions.DirectOnly, "DirectlyDerivedClass", true )]
    [InlineData( DerivedTypesOptions.All, "DirectlyDerivedClass", true )]
    [InlineData( DerivedTypesOptions.FirstLevelWithinCompilationOnly, "DirectlyDerivedClass", true )]
    [InlineData( DerivedTypesOptions.DirectOnly, "IDerivedInterface", true )]
    [InlineData( DerivedTypesOptions.All, "IDerivedInterface", true )]
    [InlineData( DerivedTypesOptions.FirstLevelWithinCompilationOnly, "IDerivedInterface", true )]
    [InlineData( DerivedTypesOptions.DirectOnly, "IndirectlyDerivedThroughBaseClass", false )]
    [InlineData( DerivedTypesOptions.All, "IndirectlyDerivedThroughBaseClass", true )]
    [InlineData( DerivedTypesOptions.FirstLevelWithinCompilationOnly, "IndirectlyDerivedThroughBaseClass", false )]
    [InlineData( DerivedTypesOptions.DirectOnly, "IndirectlyDerivedThroughInterface", false )]
    [InlineData( DerivedTypesOptions.All, "IndirectlyDerivedThroughInterface", true )]
    [InlineData( DerivedTypesOptions.FirstLevelWithinCompilationOnly, "IndirectlyDerivedThroughInterface", false )]
    [InlineData( DerivedTypesOptions.DirectOnly, "MyTextReader", false )]
    [InlineData( DerivedTypesOptions.All, "MyTextReader", true )]
    [InlineData( DerivedTypesOptions.FirstLevelWithinCompilationOnly, "MyTextReader", true )]
    [Theory]
    public void Test( DerivedTypesOptions options, string typeName, bool shouldBeIncluded )
    {
        using var testContext = this.CreateTestContext();
        var compilation = testContext.CreateCompilationModel( _code );

        // Test using GetDerivedTypes.
        var derivedTypes = compilation.GetDerivedTypes( typeof(IDisposable), options );

        var isIncluded = derivedTypes.Any( t => t.Name == typeName );
        Assert.Equal( shouldBeIncluded, isIncluded );

        // Test using IsDerivedFrom.
        var type = compilation.Types.OfName( typeName ).Single();
        var matches = type.DerivesFrom( typeof(IDisposable), options );

        Assert.Equal( shouldBeIncluded, matches );
    }
}