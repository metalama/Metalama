// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.DesignTime.VisualStudio.AspectExplorer;
using Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Serialization;

public sealed class RpcEventSerializationTests : JsonSerializationTestsBase
{
    private static readonly JsonConverter[] _converters = [new StringEnumConverter()];

    public RpcEventSerializationTests( ITestOutputHelper output ) : base( output ) { }

    [Fact]
    public void ProjectKey_Serialization()
    {
        var input = new ProjectKey( "MyAssembly", 0x123456789ABCDEF0, isMetalamaEnabled: true );

        const string expectedJson = """
            {
              "PreprocessorSymbolHashCode": 1311768467463790320,
              "AssemblyName": "MyAssembly",
              "IsMetalamaEnabled": true
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void ProjectKey_MetalamaDisabled_Serialization()
    {
        var input = new ProjectKey( "OtherAssembly", 0, isMetalamaEnabled: false );

        const string expectedJson = """
            {
              "PreprocessorSymbolHashCode": 0,
              "AssemblyName": "OtherAssembly",
              "IsMetalamaEnabled": false
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void RpcEventEnvelope_Serialization()
    {
        var eventData = new CompilationResultChangedEventData(
            new ProjectKey( "TestProject", 12345, true ),
            isPartialCompilation: false,
            ImmutableArray<string>.Empty );

        var input = new RpcEventEnvelope( "IEventHubRpcApi", eventData );

        // Test round-trip serialization with TypeNameHandling for abstract class
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = _converters
        };

        var json = JsonConvert.SerializeObject( input, Formatting.Indented, settings );
        this.Output.WriteLine( "RpcEventEnvelope JSON:" );
        this.Output.WriteLine( json );

        var deserialized = JsonConvert.DeserializeObject<RpcEventEnvelope>( json, settings );

        Assert.NotNull( deserialized );
        Assert.Equal( input.OriginatingApi, deserialized.OriginatingApi );
        Assert.NotNull( deserialized.Data );
        Assert.IsType<CompilationResultChangedEventData>( deserialized.Data );
    }

    [Fact]
    public void CompilationResultChangedEventData_Serialization()
    {
        var projectKey = new ProjectKey( "MyProject", 0xABCDEF, true );
        var input = new CompilationResultChangedEventData(
            projectKey,
            isPartialCompilation: true,
            ImmutableArray.Create( "/src/File1.cs", "/src/File2.cs" ) );

        const string expectedJson = """
            {
              "ProjectKey": {
                "PreprocessorSymbolHashCode": 11259375,
                "AssemblyName": "MyProject",
                "IsMetalamaEnabled": true
              },
              "IsPartialCompilation": true,
              "SyntaxTreePaths": [
                "/src/File1.cs",
                "/src/File2.cs"
              ],
              "Category": ""
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CompilationResultChangedEventData_Empty_Serialization()
    {
        var projectKey = new ProjectKey( "EmptyProject", 0, true );
        var input = new CompilationResultChangedEventData(
            projectKey,
            isPartialCompilation: false,
            ImmutableArray<string>.Empty );

        const string expectedJson = """
            {
              "ProjectKey": {
                "PreprocessorSymbolHashCode": 0,
                "AssemblyName": "EmptyProject",
                "IsMetalamaEnabled": true
              },
              "IsPartialCompilation": false,
              "SyntaxTreePaths": [],
              "Category": ""
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void EndpointChangedEventData_Serialization()
    {
        var input = new EndpointChangedEventData( Guid.Parse( "a1b2c3d4-e5f6-7890-abcd-ef1234567890" ) );

        const string expectedJson = """
            {
              "ProjectGuid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
              "Category": ""
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void RpcServiceInfo_Serialization()
    {
        var input = new RpcServiceInfo( "MyPipe_1", "MyNamespace.MyServiceFactory", "MyExtension" );

        const string expectedJson = """
            {
              "PipeName": "MyPipe_1",
              "FactoryTypeName": "MyNamespace.MyServiceFactory",
              "ExtensionName": "MyExtension"
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void RpcServiceInfo_NoExtension_Serialization()
    {
        var input = new RpcServiceInfo( "MyPipe_2", "MyNamespace.AnotherFactory", null );

        const string expectedJson = """
            {
              "PipeName": "MyPipe_2",
              "FactoryTypeName": "MyNamespace.AnotherFactory",
              "ExtensionName": null
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void ServicesAddedEventData_Serialization()
    {
        var services = ImmutableArray.Create(
            new RpcServiceInfo( "Pipe1", "Factory1", "Ext1" ),
            new RpcServiceInfo( "Pipe2", "Factory2", null ) );

        var input = new ServicesAddedEventData( services );

        const string expectedJson = """
            {
              "Services": [
                {
                  "PipeName": "Pipe1",
                  "FactoryTypeName": "Factory1",
                  "ExtensionName": "Ext1"
                },
                {
                  "PipeName": "Pipe2",
                  "FactoryTypeName": "Factory2",
                  "ExtensionName": null
                }
              ],
              "Category": "IRpcServiceProviderApi"
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void AspectClassesChangedEventData_Serialization()
    {
        var projectKey = new ProjectKey( "AspectProject", 12345, true );
        var input = new AspectClassesChangedEventData( projectKey );

        const string expectedJson = """
            {
              "Category": "IAspectExplorerRpcApi",
              "ProjectKey": {
                "PreprocessorSymbolHashCode": 12345,
                "AssemblyName": "AspectProject",
                "IsMetalamaEnabled": true
              }
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void AspectInstancesChangedEventData_Serialization()
    {
        var projectKey = new ProjectKey( "InstanceProject", 54321, true );
        var input = new AspectInstancesChangedEventData( projectKey );

        const string expectedJson = """
            {
              "Category": "IAspectExplorerRpcApi",
              "ProjectKey": {
                "PreprocessorSymbolHashCode": 54321,
                "AssemblyName": "InstanceProject",
                "IsMetalamaEnabled": true
              }
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CompileTimeEditingStatusChangedEventData_Editing_Serialization()
    {
        var input = new CompileTimeEditingStatusChangedEventData( isEditing: true );

        const string expectedJson = """
            {
              "Category": "ICompileTimeCodeEditingStatusRpcApi",
              "IsEditing": true
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CompileTimeEditingStatusChangedEventData_NotEditing_Serialization()
    {
        var input = new CompileTimeEditingStatusChangedEventData( isEditing: false );

        const string expectedJson = """
            {
              "Category": "ICompileTimeCodeEditingStatusRpcApi",
              "IsEditing": false
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CompileTimeErrorsChangedEventData_Serialization()
    {
        var projectKey = new ProjectKey( "ErrorProject", 99999, true );
        var errors = ImmutableArray.Create(
            new DiagnosticData( DiagnosticSeverity.Error, "/src/Error.cs", "Error message", 10, 5, 10, 25 ) );

        var input = new CompileTimeErrorsChangedEventData( projectKey, errors );

        const string expectedJson = """
            {
              "ProjectKey": {
                "PreprocessorSymbolHashCode": 99999,
                "AssemblyName": "ErrorProject",
                "IsMetalamaEnabled": true
              },
              "Errors": [
                {
                  "Severity": "Error",
                  "FilePath": "/src/Error.cs",
                  "Message": "Error message",
                  "StartLine": 10,
                  "StartColumn": 5,
                  "EndLine": 10,
                  "EndColumn": 25
                }
              ],
              "Category": "ICompileTimeCodeEditingStatusRpcApi"
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void GeneratedSourceChangedEventData_Serialization()
    {
        var projectKey = new ProjectKey( "GenProject", 11111, true );
        var sources = ImmutableDictionary<string, string>.Empty
            .Add( "Generated1.cs", "// Generated code 1" );

        var input = new GeneratedSourceChangedEventData( projectKey, sources );

        const string expectedJson = """
            {
              "ProjectKey": {
                "PreprocessorSymbolHashCode": 11111,
                "AssemblyName": "GenProject",
                "IsMetalamaEnabled": true
              },
              "Sources": {
                "Generated1.cs": "// Generated code 1"
              },
              "Category": "ISourceGeneratorRpcApi"
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void DiagnosticData_Serialization()
    {
        var input = new DiagnosticData(
            DiagnosticSeverity.Warning,
            "/src/Warning.cs",
            "Warning: something is deprecated",
            20,
            0,
            20,
            30 );

        const string expectedJson = """
            {
              "Severity": "Warning",
              "FilePath": "/src/Warning.cs",
              "Message": "Warning: something is deprecated",
              "StartLine": 20,
              "StartColumn": 0,
              "EndLine": 20,
              "EndColumn": 30
            }
            """;

        this.TestSerialization( input, expectedJson );
    }
}
