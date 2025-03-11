// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime.Serialization;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization;

public sealed class AttributeSerializationTests : SerializationTestsBase
{
    [Fact]
    public void RoundtripTest()
    {
        const string code = """
                            [TheAttribute("constructorArgumentValue",
                                           "constructorArrayItem1",
                                           "constructorArrayItem2",
                                           typeof(TheAttribute),
                                           System.ConsoleColor.Black,
                                           NamedArgument = "namedArgumentValue",
                                           NamedArrayArgument = [
                                               "namedArrayArgumentItem1",
                                               "namedArrayArgumentItem2",
                                               typeof(TheAttribute),
                                               System.ConsoleColor.Red ] )]
                            public class C;
                            public class TheAttribute : System.Attribute
                            {
                                public TheAttribute(string constructorArgument, params object[] constructorArrayArgument) { }
                            
                                public string NamedArgument
                                {
                                    get;
                                    set;
                                }
                                public object[] NamedArrayArgument
                                {
                                    get;
                                    set;
                                }
                            }

                            """;

        using var testContext = this.CreateTestContextWithCode( code );

        var attribute = testContext.Compilation.Types.OfName( "C" ).Single().Attributes.Single();

        var roundtrip = SerializeDeserialize( attribute.ToRef(), testContext ).GetTarget( testContext.Compilation );

        Assert.Equal( attribute.Type, roundtrip.Type );
        Assert.Equal( attribute.Constructor, roundtrip.Constructor );
        Assert.Equal( attribute.ConstructorArguments, roundtrip.ConstructorArguments, ( x, y ) => x.SequenceEqual( y ) );
        Assert.Equal( attribute.NamedArguments, roundtrip.NamedArguments );

        // Non-ref serialization must fail.
        Assert.Throws<CompileTimeSerializationException>( () => SerializeDeserialize( attribute, testContext ) );
    }

    [Fact]
    public void AttributeDataFromDifferentCompilationModelsAreSame()
    {
        // This is to test that two models of the same compilation have identical attribute serialization keys.

        const string code = """
                            public class TheAttribute : System.Attribute;

                            [TheAttribute]
                            public class C;

                            """;

        using var testContext = this.CreateTestContextWithCode( code );

        var compilationModel1 = testContext.Compilation;
        var attribute1 = compilationModel1.Types.OfName( "C" ).Single().Attributes.Single();

        Assert.True( ((AttributeRef) attribute1.ToRef()).TryGetAttributeSerializationDataKey( out var attributeKey1 ) );

        var compilationModel2 = testContext.CreateCompilationModel( testContext.Compilation.RoslynCompilation );
        var attribute2 = compilationModel2.Types.OfName( "C" ).Single().Attributes.Single();

        Assert.True( ((AttributeRef) attribute2.ToRef()).TryGetAttributeSerializationDataKey( out var attributeKey2 ) );

        // Test that two serialization keys of the same attribute in two models resolve are identical. 
        Assert.Same( attributeKey1, attributeKey2 );

        // Test that two references to the same attribute resolve to the same IAttribute.
        var array = new[] { attribute1.ToRef(), attribute1.ToRef() };
        var roundtripArray = TestSerialization( testContext, array, testEquality: false );

        Assert.Same( roundtripArray[0].GetTarget( compilationModel1 ), roundtripArray[1].GetTarget( compilationModel1 ) );
    }
}