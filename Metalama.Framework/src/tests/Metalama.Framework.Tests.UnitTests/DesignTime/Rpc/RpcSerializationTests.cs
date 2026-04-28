// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using System.Reflection;
using Metalama.Framework.DesignTime.AspectExplorer;
using Metalama.Framework.DesignTime.CodeLens;
using Metalama.Framework.DesignTime.Diagnostics;
using Metalama.Framework.DesignTime.Preview;
using Metalama.Framework.DesignTime.Rpc;
using Metalama.Framework.DesignTime.Rpc.Notifications;
using Metalama.Framework.DesignTime.VisualStudio.AspectExplorer;
using Metalama.Framework.DesignTime.VisualStudio.CompileTimeCodeEditingStatus;
using Metalama.Framework.DesignTime.VisualStudio.ServiceProvider;
using Metalama.Framework.DesignTime.VisualStudio.SourceGenerating;
using Metalama.Framework.Engine.DesignTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Rpc;

/// <summary>
/// Roundtrip serialization tests for the RPC contract types — uses <see cref="MessagePackHelper"/>, which
/// goes through the same <c>MessagePackSerializerOptions</c> as <c>BaseEndpoint.CreateRpc</c> so these tests
/// validate the real wire path end-to-end.
/// </summary>
public sealed class RpcSerializationTests
{
    private static readonly MessagePackHelper _helper = new MessagePackHelper();

    private static T Roundtrip<T>( T value )
    {
        // Verify that the type is marked with [RpcContract] attribute.
        var type = typeof(T);
        var attribute = type.GetCustomAttribute<RpcContractAttribute>();
        Assert.True( attribute != null, $"Type {type.FullName} is missing the [RpcContract] attribute." );

        return _helper.Deserialize<T>( _helper.Serialize( value ) );
    }

    [Fact]
    public void NonGeneric_Roundtrip_MatchesGeneric()
    {
        // Pins that MessagePackHelper's non-generic Serialize(object, Type) / Deserialize(byte[], Type) overloads
        // produce wire bytes byte-equivalent to the generic ones for the same options + same concrete type.
        // Premium relies on this: its CodeActionDescriptor.ToDescriptor passes the runtime type of a leaf model
        // to the non-generic overload (the type discriminator rides out-of-band in the descriptor).
        var input = new RpcServiceInfo( "pipe-name", "FactoryType", "ExtensionName" );

        var bytesGeneric = _helper.Serialize( input );
        var bytesNonGeneric = _helper.Serialize( input, typeof(RpcServiceInfo) );

        Assert.Equal( bytesGeneric, bytesNonGeneric );

        var fromGeneric = _helper.Deserialize<RpcServiceInfo>( bytesNonGeneric );
        var fromNonGeneric = (RpcServiceInfo) _helper.Deserialize( bytesGeneric, typeof(RpcServiceInfo) )!;

        Assert.Equal( fromGeneric.PipeName, fromNonGeneric.PipeName );
        Assert.Equal( fromGeneric.FactoryTypeName, fromNonGeneric.FactoryTypeName );
        Assert.Equal( fromGeneric.ExtensionName, fromNonGeneric.ExtensionName );
    }

    [Fact]
    public void ProjectKey_Roundtrip()
    {
        var original = new ProjectKey( "MyAssembly", 12345UL, true );
        var result = Roundtrip( original );

        Assert.Equal( original.AssemblyName, result.AssemblyName );
        Assert.Equal( original.PreprocessorSymbolHashCode, result.PreprocessorSymbolHashCode );
        Assert.Equal( original.IsMetalamaEnabled, result.IsMetalamaEnabled );
    }

    [Fact]
    public void ProjectKey_WithZeroHashCode_Roundtrip()
    {
        var original = new ProjectKey( "TestAssembly", 0UL, false );
        var result = Roundtrip( original );

        Assert.Equal( original.AssemblyName, result.AssemblyName );
        Assert.Equal( original.PreprocessorSymbolHashCode, result.PreprocessorSymbolHashCode );
        Assert.False( result.HasHashCode );
    }

    [Fact]
    public void RpcEventEnvelope_Roundtrip()
    {
        var original = new RpcEventEnvelope(
            "TestApi",
            new CompilationResultChangedEventData(
                new ProjectKey( "Test", 123UL, true ),
                false,
                ImmutableArray.Create( "file1.cs", "file2.cs" ) ) );

        var result = Roundtrip( original );

        Assert.Equal( original.OriginatingApi, result.OriginatingApi );
        Assert.IsType<CompilationResultChangedEventData>( result.Data );

        var originalData = (CompilationResultChangedEventData) original.Data;
        var resultData = (CompilationResultChangedEventData) result.Data;

        Assert.Equal( originalData.ProjectKey.AssemblyName, resultData.ProjectKey.AssemblyName );
        Assert.Equal( originalData.IsPartialCompilation, resultData.IsPartialCompilation );
        Assert.True( originalData.SyntaxTreePaths.SequenceEqual( resultData.SyntaxTreePaths ) );
    }

    [Fact]
    public void CompilationResultChangedEventData_Roundtrip()
    {
        var original = new CompilationResultChangedEventData(
            new ProjectKey( "MyProject", 999UL, true ),
            true,
            ImmutableArray.Create( "path/to/file.cs" ) );

        var result = Roundtrip( original );

        Assert.Equal( original.ProjectKey.AssemblyName, result.ProjectKey.AssemblyName );
        Assert.Equal( original.IsPartialCompilation, result.IsPartialCompilation );
        Assert.True( original.SyntaxTreePaths.SequenceEqual( result.SyntaxTreePaths ) );
    }

    [Fact]
    public void CompilationResultChangedEventData_EmptySyntaxTreePaths_Roundtrip()
    {
        var original = new CompilationResultChangedEventData(
            new ProjectKey( "MyProject", 0UL, false ),
            false,
            ImmutableArray<string>.Empty );

        var result = Roundtrip( original );

        Assert.True( original.SyntaxTreePaths.SequenceEqual( result.SyntaxTreePaths ) );
    }

    [Fact]
    public void EndpointChangedEventData_Roundtrip()
    {
        var guid = Guid.NewGuid();
        var original = new EndpointChangedEventData( guid );

        var result = Roundtrip( original );

        Assert.Equal( guid, result.ProjectGuid );
    }

    [Fact]
    public void CodeLensSummary_Roundtrip()
    {
        var original = new CodeLensSummary( "3 aspects applied", "tooltip text" );

        var result = Roundtrip( original );

        Assert.Equal( original.Description, result.Description );
        Assert.Equal( original.TooltipText, result.TooltipText );
    }

    [Fact]
    public void CodeLensSummary_WithNullTooltip_Roundtrip()
    {
        var original = new CodeLensSummary( "description only" );

        var result = Roundtrip( original );

        Assert.Equal( original.Description, result.Description );
        Assert.Null( result.TooltipText );
    }

    [Fact]
    public void CodeLensDetailsTable_Roundtrip()
    {
        var original = new CodeLensDetailsTable(
            ImmutableArray.Create(
                new CodeLensDetailsHeader( "Aspect", "aspect", true, 100 ),
                new CodeLensDetailsHeader( "Target", "target", true, 200 ) ),
            ImmutableArray.Create(
                new CodeLensDetailsEntry(
                    ImmutableArray.Create(
                        new CodeLensDetailsField( "LoggingAspect" ),
                        new CodeLensDetailsField( "MyMethod" ) ),
                    "Row 1 tooltip" ) ) );

        var result = Roundtrip( original );

        Assert.Equal( original.Headers.Length, result.Headers.Length );
        Assert.Equal( original.Headers[0].DisplayName, result.Headers[0].DisplayName );
        Assert.Equal( original.Headers[0].UniqueName, result.Headers[0].UniqueName );
        Assert.Equal( original.Headers[0].IsVisible, result.Headers[0].IsVisible );
        Assert.Equal( original.Headers[0].Width, result.Headers[0].Width );

        Assert.Equal( original.Entries.Length, result.Entries.Length );
        Assert.Equal( original.Entries[0].Fields.Length, result.Entries[0].Fields.Length );
        Assert.Equal( original.Entries[0].Fields[0].Text, result.Entries[0].Fields[0].Text );
        Assert.Equal( original.Entries[0].Tooltip, result.Entries[0].Tooltip );
    }

    [Fact]
    public void CodeLensDetailsTable_Empty_Roundtrip()
    {
        var original = CodeLensDetailsTable.Empty;

        var result = Roundtrip( original );

        Assert.Empty( result.Headers );
        Assert.Empty( result.Entries );
    }

    [Fact]
    public void DiagnosticData_Roundtrip()
    {
        var original = new DiagnosticData(
            DiagnosticSeverity.Error,
            "C:/src/file.cs",
            "Error CS0001: Something went wrong",
            10,
            5,
            10,
            25 );

        var result = Roundtrip( original );

        Assert.Equal( original.Severity, result.Severity );
        Assert.Equal( original.FilePath, result.FilePath );
        Assert.Equal( original.Message, result.Message );
        Assert.Equal( original.StartLine, result.StartLine );
        Assert.Equal( original.StartColumn, result.StartColumn );
        Assert.Equal( original.EndLine, result.EndLine );
        Assert.Equal( original.EndColumn, result.EndColumn );
    }

    [Fact]
    public void SerializablePreviewTransformationResult_Success_Roundtrip()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText( "class TransformedCode { }" );
        var serializableSyntaxTree = JsonSerializationHelper.CreateSerializableSyntaxTree( syntaxTree );

        var original = SerializablePreviewTransformationResult.Success( serializableSyntaxTree, null );

        var result = Roundtrip( original );

        Assert.True( result.IsSuccessful );
        Assert.NotNull( result.TransformedSyntaxTree );
        Assert.Equal( serializableSyntaxTree.Text, result.TransformedSyntaxTree.Text );
        Assert.Null( result.ErrorMessages );
    }

    [Fact]
    public void SerializablePreviewTransformationResult_Failure_Roundtrip()
    {
        var original = SerializablePreviewTransformationResult.Failure( "Error 1", "Error 2" );

        var result = Roundtrip( original );

        Assert.False( result.IsSuccessful );
        Assert.Null( result.TransformedSyntaxTree );
        Assert.NotNull( result.ErrorMessages );
        Assert.Equal( 2, result.ErrorMessages.Length );
        Assert.Equal( "Error 1", result.ErrorMessages[0] );
        Assert.Equal( "Error 2", result.ErrorMessages[1] );
    }

    [Fact]
    public void PolymorphicRpcEventData_CompilationResultChanged_Roundtrip()
    {
        RpcEventData original = new CompilationResultChangedEventData(
            new ProjectKey( "Test", 1UL, true ),
            false,
            ImmutableArray<string>.Empty );

        var result = Roundtrip( original );

        Assert.IsType<CompilationResultChangedEventData>( result );
    }

    [Fact]
    public void PolymorphicRpcEventData_EndpointChanged_Roundtrip()
    {
        RpcEventData original = new EndpointChangedEventData( Guid.NewGuid() );

        var result = Roundtrip( original );

        Assert.IsType<EndpointChangedEventData>( result );
    }

    [Fact]
    public void RpcServiceInfo_Roundtrip()
    {
        var original = new RpcServiceInfo( "pipe-name-123", "MyFactory.Type", "ExtensionName" );

        var result = Roundtrip( original );

        Assert.Equal( original.PipeName, result.PipeName );
        Assert.Equal( original.FactoryTypeName, result.FactoryTypeName );
        Assert.Equal( original.ExtensionName, result.ExtensionName );
    }

    [Fact]
    public void RpcServiceInfo_NullExtension_Roundtrip()
    {
        var original = new RpcServiceInfo( "pipe-name", "FactoryType", null );

        var result = Roundtrip( original );

        Assert.Null( result.ExtensionName );
    }

    [Fact]
    public void ServicesAddedEventData_Roundtrip()
    {
        var original = new ServicesAddedEventData(
            ImmutableArray.Create(
                new RpcServiceInfo( "pipe1", "Factory1", "Ext1" ),
                new RpcServiceInfo( "pipe2", "Factory2", null ) ) );

        var result = Roundtrip( original );

        Assert.Equal( original.Services.Length, result.Services.Length );
        Assert.Equal( original.Services[0].PipeName, result.Services[0].PipeName );
        Assert.Equal( original.Services[1].FactoryTypeName, result.Services[1].FactoryTypeName );
    }

    [Fact]
    public void GeneratedSourceChangedEventData_Roundtrip()
    {
        var original = new GeneratedSourceChangedEventData(
            new ProjectKey( "MyProject", 123UL, true ),
            ImmutableDictionary<string, string>.Empty
                .Add( "file1.g.cs", "// generated code 1" )
                .Add( "file2.g.cs", "// generated code 2" ) );

        var result = Roundtrip( original );

        Assert.Equal( original.ProjectKey.AssemblyName, result.ProjectKey.AssemblyName );
        Assert.Equal( original.Sources.Count, result.Sources.Count );
        Assert.Equal( original.Sources["file1.g.cs"], result.Sources["file1.g.cs"] );
    }

    [Fact]
    public void AspectInstancesChangedEventData_Roundtrip()
    {
        var original = new AspectInstancesChangedEventData( new ProjectKey( "TestProject", 456UL, true ) );

        var result = Roundtrip( original );

        Assert.Equal( original.ProjectKey.AssemblyName, result.ProjectKey.AssemblyName );
    }

    [Fact]
    public void AspectClassesChangedEventData_Roundtrip()
    {
        var original = new AspectClassesChangedEventData( new ProjectKey( "TestProject", 789UL, true ) );

        var result = Roundtrip( original );

        Assert.Equal( original.ProjectKey.AssemblyName, result.ProjectKey.AssemblyName );
    }

    [Fact]
    public void CompileTimeErrorsChangedEventData_Roundtrip()
    {
        var original = new CompileTimeErrorsChangedEventData(
            new ProjectKey( "ErrorProject", 111UL, true ),
            ImmutableArray.Create(
                new DiagnosticData(
                    DiagnosticSeverity.Error,
                    "file.cs",
                    "CS0001: Error message",
                    1,
                    0,
                    1,
                    10 ) ) );

        var result = Roundtrip( original );

        Assert.Equal( original.ProjectKey.AssemblyName, result.ProjectKey.AssemblyName );
        Assert.Equal( original.Errors.Length, result.Errors.Length );
        Assert.Equal( original.Errors[0].Message, result.Errors[0].Message );
    }

    [Fact]
    public void CompileTimeEditingStatusChangedEventData_Roundtrip()
    {
        var original = new CompileTimeEditingStatusChangedEventData( true );

        var result = Roundtrip( original );

        Assert.Equal( original.IsEditing, result.IsEditing );
    }

    [Fact]
    public void AspectDatabaseAspectInstance_Roundtrip()
    {
        var original = new AspectDatabaseAspectInstance(
            "M:MyNamespace.MyClass.MyMethod",
            ImmutableArray.Create(
                new AspectDatabaseAspectTransformation(
                    "M:MyNamespace.MyClass.MyMethod",
                    "Introduces logging",
                    "M:MyNamespace.MyClass.MyMethod$Logged",
                    "file.cs" ) ) );

        var result = Roundtrip( original );

        Assert.Equal( original.TargetDeclarationId, result.TargetDeclarationId );
        Assert.Equal( original.Transformations.Length, result.Transformations.Length );
        Assert.Equal( original.Transformations[0].Description, result.Transformations[0].Description );
    }
}