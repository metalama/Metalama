// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.AspectExplorer;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.Engine.DesignTime;
using Metalama.Framework.Engine.DesignTime.CodeFixes;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Serialization;

public sealed class DesignTimeSerializationTests : JsonSerializationTestsBase
{
    private static readonly JsonConverter[] _converters = [new StringEnumConverter()];

    public DesignTimeSerializationTests( ITestOutputHelper output ) : base( output ) { }

    [Fact]
    public void DiagnosticData_Error_Serialization()
    {
        var input = new DiagnosticData(
            DiagnosticSeverity.Error,
            "/src/MyFile.cs",
            "Compilation error: missing semicolon",
            10,
            5,
            10,
            15 );

        const string expectedJson = """
            {
              "Severity": "Error",
              "FilePath": "/src/MyFile.cs",
              "Message": "Compilation error: missing semicolon",
              "StartLine": 10,
              "StartColumn": 5,
              "EndLine": 10,
              "EndColumn": 15
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void DiagnosticData_Warning_Serialization()
    {
        var input = new DiagnosticData(
            DiagnosticSeverity.Warning,
            "/src/OtherFile.cs",
            "Warning: unused variable",
            25,
            0,
            25,
            10 );

        const string expectedJson = """
            {
              "Severity": "Warning",
              "FilePath": "/src/OtherFile.cs",
              "Message": "Warning: unused variable",
              "StartLine": 25,
              "StartColumn": 0,
              "EndLine": 25,
              "EndColumn": 10
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void AspectDatabaseAspectTransformation_Serialization()
    {
        var input = new AspectDatabaseAspectTransformation(
            "MyNamespace.MyClass.MyMethod()",
            "Adds logging before and after the method",
            "MyNamespace.MyClass.MyMethod_Transformed()",
            "/src/Generated/MyClass.g.cs" );

        const string expectedJson = """
            {
              "TargetDeclarationId": "MyNamespace.MyClass.MyMethod()",
              "Description": "Adds logging before and after the method",
              "TransformedDeclarationId": "MyNamespace.MyClass.MyMethod_Transformed()",
              "FilePath": "/src/Generated/MyClass.g.cs"
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void AspectDatabaseAspectTransformation_Minimal_Serialization()
    {
        var input = new AspectDatabaseAspectTransformation(
            "MyNamespace.MyProperty",
            "Validates property value" );

        const string expectedJson = """
            {
              "TargetDeclarationId": "MyNamespace.MyProperty",
              "Description": "Validates property value",
              "TransformedDeclarationId": null,
              "FilePath": null
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void AspectDatabaseAspectInstance_Serialization()
    {
        var transformations = ImmutableArray.Create(
            new AspectDatabaseAspectTransformation( "Method1", "Description1" ),
            new AspectDatabaseAspectTransformation( "Method2", "Description2", "Method2_T" ) );

        var input = new AspectDatabaseAspectInstance( "MyNamespace.MyClass", transformations );

        const string expectedJson = """
            {
              "TargetDeclarationId": "MyNamespace.MyClass",
              "Transformations": [
                {
                  "TargetDeclarationId": "Method1",
                  "Description": "Description1",
                  "TransformedDeclarationId": null,
                  "FilePath": null
                },
                {
                  "TargetDeclarationId": "Method2",
                  "Description": "Description2",
                  "TransformedDeclarationId": "Method2_T",
                  "FilePath": null
                }
              ]
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void AspectDatabaseAspectInstance_Empty_Serialization()
    {
        var input = new AspectDatabaseAspectInstance( "EmptyClass", ImmutableArray<AspectDatabaseAspectTransformation>.Empty );

        const string expectedJson = """
            {
              "TargetDeclarationId": "EmptyClass",
              "Transformations": []
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void SerializableAnnotation_Serialization()
    {
        var input = new SerializableAnnotation(
            SerializableAnnotationTargetKind.Node,
            100,
            50,
            SerializableAnnotationKind.GeneratedCode,
            "additional data" );

        const string expectedJson = """
            {
              "SpanStart": 100,
              "SpanLength": 50,
              "Kind": "GeneratedCode",
              "TargetKind": "Node",
              "Data": "additional data"
            }
            """;

        // SerializableAnnotation is a struct, so use direct JSON serialization
        var serializedJson = JsonConvert.SerializeObject( input, Formatting.Indented, _converters );
        this.Output.WriteLine( "Serialized JSON:" );
        this.Output.WriteLine( serializedJson );

        Assert.Equal( NormalizeJson( expectedJson ), NormalizeJson( serializedJson ) );

        var deserialized = JsonConvert.DeserializeObject<SerializableAnnotation>( expectedJson, _converters );
        Assert.Equal( input.SpanStart, deserialized.SpanStart );
        Assert.Equal( input.SpanLength, deserialized.SpanLength );
        Assert.Equal( input.Kind, deserialized.Kind );
        Assert.Equal( input.TargetKind, deserialized.TargetKind );
        Assert.Equal( input.Data, deserialized.Data );
    }

    [Fact]
    public void SerializableAnnotation_NoData_Serialization()
    {
        var input = new SerializableAnnotation(
            SerializableAnnotationTargetKind.Token,
            0,
            10,
            SerializableAnnotationKind.Formatter,
            null );

        const string expectedJson = """
            {
              "SpanStart": 0,
              "SpanLength": 10,
              "Kind": "Formatter",
              "TargetKind": "Token",
              "Data": null
            }
            """;

        // SerializableAnnotation is a struct, so use direct JSON serialization
        var serializedJson = JsonConvert.SerializeObject( input, Formatting.Indented, _converters );
        this.Output.WriteLine( "Serialized JSON:" );
        this.Output.WriteLine( serializedJson );

        Assert.Equal( NormalizeJson( expectedJson ), NormalizeJson( serializedJson ) );

        var deserialized = JsonConvert.DeserializeObject<SerializableAnnotation>( expectedJson, _converters );
        Assert.Equal( input.SpanStart, deserialized.SpanStart );
        Assert.Equal( input.SpanLength, deserialized.SpanLength );
        Assert.Equal( input.Kind, deserialized.Kind );
        Assert.Equal( input.TargetKind, deserialized.TargetKind );
        Assert.Equal( input.Data, deserialized.Data );
    }

    private static string NormalizeJson( string json )
    {
        return JsonConvert.SerializeObject(
            JsonConvert.DeserializeObject( json ),
            Formatting.Indented );
    }

    [Fact]
    public void SerializableSyntaxTree_Serialization()
    {
        var annotations = ImmutableArray.Create(
            new SerializableAnnotation( SerializableAnnotationTargetKind.Node, 0, 100, SerializableAnnotationKind.GeneratedCode, null ) );

        var input = new SerializableSyntaxTree(
            "/src/MyFile.cs",
            "public class MyClass { }",
            annotations );

        const string expectedJson = """
            {
              "Text": "public class MyClass { }",
              "Annotations": [
                {
                  "SpanStart": 0,
                  "SpanLength": 100,
                  "Kind": "GeneratedCode",
                  "TargetKind": "Node",
                  "Data": null
                }
              ],
              "FilePath": "/src/MyFile.cs"
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void SerializableSyntaxTree_NoAnnotations_Serialization()
    {
        var input = new SerializableSyntaxTree(
            "/src/SimpleFile.cs",
            "namespace Simple { }",
            ImmutableArray<SerializableAnnotation>.Empty );

        const string expectedJson = """
            {
              "Text": "namespace Simple { }",
              "Annotations": [],
              "FilePath": "/src/SimpleFile.cs"
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void SerializablePreviewTransformationResult_Success_Serialization()
    {
        var syntaxTree = new SerializableSyntaxTree(
            "/src/Preview.cs",
            "transformed code",
            ImmutableArray<SerializableAnnotation>.Empty );

        var input = SerializablePreviewTransformationResult.Success( syntaxTree, null );

        const string expectedJson = """
            {
              "IsSuccessful": true,
              "TransformedSyntaxTree": {
                "Text": "transformed code",
                "Annotations": [],
                "FilePath": "/src/Preview.cs"
              },
              "ErrorMessages": null
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void SerializablePreviewTransformationResult_Failure_Serialization()
    {
        var input = SerializablePreviewTransformationResult.Failure( "Error 1", "Error 2" );

        const string expectedJson = """
            {
              "IsSuccessful": false,
              "TransformedSyntaxTree": null,
              "ErrorMessages": [
                "Error 1",
                "Error 2"
              ]
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeActionResult_Success_Serialization()
    {
        var changes = ImmutableArray.Create(
            new SerializableSyntaxTree( "/src/File1.cs", "modified content", ImmutableArray<SerializableAnnotation>.Empty ) );

        var input = CodeActionResult.Success( changes );

        const string expectedJson = """
            {
              "SyntaxTreeChanges": [
                {
                  "Text": "modified content",
                  "Annotations": [],
                  "FilePath": "/src/File1.cs"
                }
              ],
              "ErrorMessages": null,
              "IsSuccessful": true
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeActionResult_Error_Serialization()
    {
        var input = CodeActionResult.Error( "Failed to apply transformation" );

        const string expectedJson = """
            {
              "SyntaxTreeChanges": [],
              "ErrorMessages": [
                "Failed to apply transformation"
              ],
              "IsSuccessful": false
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeActionResult_Empty_Serialization()
    {
        var input = CodeActionResult.Empty;

        const string expectedJson = """
            {
              "SyntaxTreeChanges": [],
              "ErrorMessages": null,
              "IsSuccessful": true
            }
            """;

        this.TestSerialization( input, expectedJson );
    }
}
