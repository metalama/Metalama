// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Serialization;

public sealed class ManifestSerializationTests : JsonSerializationTestsBase
{
    public ManifestSerializationTests( ITestOutputHelper output ) : base( output ) { }

    [Fact]
    public void CompileTimeProjectManifest_Serialization()
    {
        var input = new CompileTimeProjectManifest(
            runTimeAssemblyIdentity: "MyAssembly, Version=1.0.0.0",
            targetFramework: ".NETCoreApp,Version=v8.0",
            aspectTypes: ["MyNamespace.MyAspect"],
            plugInTypes: [],
            fabricTypes: ["MyNamespace.MyFabric"],
            transitiveFabricTypes: [],
            otherTemplateTypes: [],
            optionTypes: [],
            references: ["System.Runtime, Version=8.0.0.0"],
            templates: null,
            sourceHash: 0x123456789ABCDEF0,
            files:
            [
                new CompileTimeFileManifest { SourcePath = "MyFile.cs", TransformedPath = "MyFile_Transformed.cs" }
            ],
            diagnostics: [],
            referencesMetalamaSdk: false,
            languageVersion: LanguageVersion.CSharp12 );

        // Test round-trip serialization
        var json = input.ToJson();
        this.Output.WriteLine( "CompileTimeProjectManifest JSON:" );
        this.Output.WriteLine( json );

        var deserialized = CompileTimeProjectManifest.FromJson( json );

        Assert.NotNull( deserialized );
        Assert.Equal( input.RunTimeAssemblyIdentity, deserialized.RunTimeAssemblyIdentity );
        Assert.Equal( input.TargetFramework, deserialized.TargetFramework );
        Assert.Equal( input.AspectTypes, deserialized.AspectTypes );
        Assert.Equal( input.FabricTypes, deserialized.FabricTypes );
        Assert.Equal( input.SourceHash, deserialized.SourceHash );
        Assert.Single( deserialized.Files );
        Assert.Equal( input.Files[0].SourcePath, deserialized.Files[0].SourcePath );
        Assert.Equal( input.Files[0].TransformedPath, deserialized.Files[0].TransformedPath );
        Assert.Equal( input.ReferencesMetalamaSdk, deserialized.ReferencesMetalamaSdk );
        Assert.Equal( input.LanguageVersion, deserialized.LanguageVersion );

        // Verify re-serialization produces same output
        var reserializedJson = deserialized.ToJson();
        Assert.Equal( NormalizeJson( json ), NormalizeJson( reserializedJson ) );
    }

    [Fact]
    public void CompileTimeFileManifest_Serialization()
    {
        var input = new CompileTimeFileManifest { SourcePath = "/src/MyFile.cs", TransformedPath = "/obj/compile-time/MyFile.cs" };

        const string expectedJson = """
                                    {
                                      "sourcePath": "/src/MyFile.cs",
                                      "transformedPath": "/obj/compile-time/MyFile.cs"
                                    }
                                    """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CompileTimeDiagnosticManifest_Serialization()
    {
        var input = new CompileTimeDiagnosticManifest
        {
            Id = "META0001",
            Category = "Metalama.Test",
            Message = "Test diagnostic message",
            Severity = DiagnosticSeverity.Warning,
            DefaultSeverity = DiagnosticSeverity.Warning,
            IsEnabledByDefault = true,
            WarningLevel = 1,
            IsSuppressed = false,
            Title = "Test Diagnostic",
            Description = "Test diagnostic description",
            HelpLinkUri = "https://doc.postsharp.net/metalama/META0001",
            Location = new CompileTimeDiagnosticLocationManifest { FileIndex = 0, TextSpan = new TextSpan( 10, 20 ) },
            AdditionalLocations = [],
            CustomTags = ["CustomTag1"],
            Properties = ImmutableDictionary<string, string?>.Empty.Add( "Key1", "Value1" )
        };

        // Test round-trip serialization
        var json = ManifestSerializer.Serialize( input );
        this.Output.WriteLine( "CompileTimeDiagnosticManifest JSON:" );
        this.Output.WriteLine( json );

        var deserialized = ManifestSerializer.Deserialize<CompileTimeDiagnosticManifest>( json );

        Assert.NotNull( deserialized );
        Assert.Equal( input.Id, deserialized.Id );
        Assert.Equal( input.Category, deserialized.Category );
        Assert.Equal( input.Message, deserialized.Message );
        Assert.Equal( input.Severity, deserialized.Severity );
        Assert.Equal( input.DefaultSeverity, deserialized.DefaultSeverity );
        Assert.Equal( input.IsEnabledByDefault, deserialized.IsEnabledByDefault );
        Assert.Equal( input.WarningLevel, deserialized.WarningLevel );
        Assert.Equal( input.IsSuppressed, deserialized.IsSuppressed );
        Assert.Equal( input.Title, deserialized.Title );
        Assert.Equal( input.Description, deserialized.Description );
        Assert.Equal( input.HelpLinkUri, deserialized.HelpLinkUri );
    }

    [Fact]
    public void CompileTimeDiagnosticLocationManifest_WithFileIndex_Serialization()
    {
        var input = new CompileTimeDiagnosticLocationManifest { FileIndex = 5, TextSpan = new TextSpan( 100, 50 ) };

        const string expectedJson = """
                                    {
                                      "fileIndex": 5,
                                      "textSpan": {
                                        "start": 100,
                                        "length": 50
                                      }
                                    }
                                    """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CompileTimeDiagnosticLocationManifest_WithFilePath_Serialization()
    {
        var input = new CompileTimeDiagnosticLocationManifest
        {
            FilePath = "/src/External.cs",
            TextSpan = new TextSpan( 200, 30 ),
            LineSpan = new LinePositionSpan( new LinePosition( 10, 5 ), new LinePosition( 10, 35 ) )
        };

        const string expectedJson = """
                                    {
                                      "filePath": "/src/External.cs",
                                      "textSpan": {
                                        "start": 200,
                                        "length": 30
                                      },
                                      "lineSpan": {
                                        "start": {
                                          "line": 10,
                                          "character": 5
                                        },
                                        "end": {
                                          "line": 10,
                                          "character": 35
                                        }
                                      }
                                    }
                                    """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CompileTimeDiagnosticLocationManifest_None_Serialization()
    {
        var input = new CompileTimeDiagnosticLocationManifest { FileIndex = -1 };

        const string expectedJson = """
                                    {
                                      "fileIndex": -1,
                                      "textSpan": {
                                        "start": 0,
                                        "length": 0
                                      }
                                    }
                                    """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void TemplateProjectManifest_Empty_Serialization()
    {
        var input = TemplateProjectManifest.Empty;

        var json = ManifestSerializer.Serialize( input );
        this.Output.WriteLine( "TemplateProjectManifest.Empty JSON:" );
        this.Output.WriteLine( json );

        var deserialized = ManifestSerializer.Deserialize<TemplateProjectManifest>( json );
        Assert.NotNull( deserialized );
        Assert.NotNull( deserialized.RootSymbol );
    }

    [Fact]
    public void TemplateProjectManifest_WithChildren_Serialization()
    {
        var childSymbol = new TemplateSymbolManifest(
            id: "MyNamespace.MyClass.MyMethod()",
            scope: ExecutionScope.CompileTime,
            templateInfo: new TemplateInfoManifest( TemplateAttributeType.Template, isAbstract: false, hasNoBody: false ),
            usedApiVersion: null,
            children: null );

        var rootChildren = new Dictionary<string, IReadOnlyList<TemplateSymbolManifest>> { ["MyMethod"] = [childSymbol] };

        var rootSymbol = new TemplateSymbolManifest(
            id: "",
            scope: ExecutionScope.RunTime,
            templateInfo: null,
            usedApiVersion: null,
            children: rootChildren );

        var input = new TemplateProjectManifest( rootSymbol );

        var json = ManifestSerializer.Serialize( input );
        this.Output.WriteLine( "TemplateProjectManifest JSON:" );
        this.Output.WriteLine( json );

        var deserialized = ManifestSerializer.Deserialize<TemplateProjectManifest>( json );
        Assert.NotNull( deserialized );
        Assert.NotNull( deserialized.RootSymbol );
        Assert.NotNull( deserialized.RootSymbol.Children );
        Assert.True( deserialized.RootSymbol.Children.ContainsKey( "MyMethod" ) );
    }

    [Fact]
    public void TemplateSymbolManifest_Simple_Serialization()
    {
        var input = new TemplateSymbolManifest(
            id: "MyNamespace.MyClass",
            scope: ExecutionScope.CompileTime,
            templateInfo: null,
            usedApiVersion: null,
            children: null );

        const string expectedJson = """
                                    {
                                      "id": "MyNamespace.MyClass",
                                      "scope": "CompileTime"
                                    }
                                    """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void TemplateSymbolManifest_WithTemplateInfo_Serialization()
    {
        var input = new TemplateSymbolManifest(
            id: "MyNamespace.MyClass.MyTemplate()",
            scope: ExecutionScope.RunTimeOrCompileTime,
            templateInfo: new TemplateInfoManifest( TemplateAttributeType.Template, isAbstract: false, hasNoBody: false ),
            usedApiVersion: null,
            children: null );

        const string expectedJson = """
                                    {
                                      "id": "MyNamespace.MyClass.MyTemplate()",
                                      "scope": "RunTimeOrCompileTime",
                                      "templateInfo": {
                                        "attributeType": "Template",
                                        "isAbstract": false,
                                        "hasNoBody": false
                                      }
                                    }
                                    """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void TemplateInfoManifest_Serialization()
    {
        var input = new TemplateInfoManifest( TemplateAttributeType.DeclarativeAdvice, isAbstract: true, hasNoBody: true );

        const string expectedJson = """
                                    {
                                      "attributeType": "DeclarativeAdvice",
                                      "isAbstract": true,
                                      "hasNoBody": true
                                    }
                                    """;

        this.TestSerialization( input, expectedJson );
    }
}