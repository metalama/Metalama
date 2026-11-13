// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using SharpCrafters.Aspects.Introspection;
using SharpCrafters.Aspects.Introspection.V2;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;

namespace Metalama.Framework.Engine.Introspection;

/// <summary>
/// Writes the introspection file of a project (the <c>.mlsym</c> file, schema version 2 of the weaving symbol file) from the
/// introspection model that <see cref="IntrospectionPipelineListener"/> built during the compile-time pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The file is written through <see cref="IntrospectionV2Writer"/> of <c>SharpCrafters.Aspects.Introspection</c>, which
/// specifies the format. The <c>dotnet metalama introspect</c> commands read it. The writer is called only when the
/// <c>MetalamaWriteIntrospectionFile</c> MSBuild property is <c>True</c>, so a normal build does not pay for it.
/// </para>
/// <para>
/// A failure to write the file is reported as the warning <see cref="GeneralDiagnosticDescriptors.CannotWriteIntrospectionFile"/>.
/// It never fails the build. The file is written to a temporary file first, then copied over the previous file, so that a
/// reader never sees a partial file.
/// </para>
/// </remarks>
internal static class IntrospectionFileWriter
{
    private const string _productName = "Metalama";

    /// <summary>
    /// Writes the introspection file of a compilation, at the path given by <see cref="IProjectOptions.IntrospectionFilePath"/>.
    /// </summary>
    public static void Write(
        IProjectOptions projectOptions,
        CompilationModel compilation,
        AspectPipelineResult pipelineResult,
        IntrospectionFactory factory,
        IDiagnosticAdder diagnosticAdder,
        CancellationToken cancellationToken )
    {
        var path = projectOptions.IntrospectionFilePath;

        if ( string.IsNullOrEmpty( path ) )
        {
            diagnosticAdder.Report(
                GeneralDiagnosticDescriptors.CannotWriteIntrospectionFile.CreateRoslynDiagnostic(
                    null,
                    ("", "the MetalamaIntrospectionFile property is not set.") ) );

            return;
        }

        var temporaryPath = path + "." + Guid.NewGuid().ToString( "N" ) + ".tmp";

        try
        {
            var directory = Path.GetDirectoryName( path );

            if ( !string.IsNullOrEmpty( directory ) )
            {
                Directory.CreateDirectory( directory );
            }

            var result = new IntrospectionCompilationResultModel(
                compilation.Name ?? "",
                new DefaultIntrospectionOptionsProvider(),
                true,
                compilation,
                ImmutableArray<IIntrospectionDiagnostic>.Empty,
                factory,
                pipelineResult );

            using ( var stream = File.Create( temporaryPath ) )
            using ( var jsonWriter = new Utf8JsonWriter(
                       stream,
                       new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping } ) )
            {
                WriteContent( jsonWriter, result, compilation, projectOptions, pipelineResult, cancellationToken );
            }

            File.Copy( temporaryPath, path, true );
        }
        catch ( Exception e ) when ( e is not OperationCanceledException )
        {
            diagnosticAdder.Report( GeneralDiagnosticDescriptors.CannotWriteIntrospectionFile.CreateRoslynDiagnostic( null, (path, e.Message) ) );
        }
        finally
        {
            try
            {
                File.Delete( temporaryPath );
            }
            catch ( Exception e ) when ( e is IOException or UnauthorizedAccessException )
            {
                // A temporary file that cannot be deleted is left behind. It does not affect the readers.
            }
        }
    }

    private static void WriteContent(
        Utf8JsonWriter jsonWriter,
        IntrospectionCompilationResultModel result,
        CompilationModel compilation,
        IProjectOptions projectOptions,
        AspectPipelineResult pipelineResult,
        CancellationToken cancellationToken )
    {
        var header = new V2HeaderData
        {
            Product = _productName,
            ProductVersion = EngineAssemblyMetadataReader.Instance.PackageVersion,
            Assembly = compilation.RoslynCompilation.AssemblyName,
            TargetFramework = projectOptions.TargetFramework,
            CompilationSucceeded = !pipelineResult.Diagnostics.ReportedDiagnostics.Any( d => d.Severity == DiagnosticSeverity.Error )
        };

        using var writer = new IntrospectionV2Writer( jsonWriter, header );
        var identifiers = new Identifiers( writer, compilation );

        var aspectClasses = result.AspectClasses;

        // The identifiers of the aspect instances in the file, which the predecessors of the child aspects refer to.
        var aspectInstanceIds = new Dictionary<IIntrospectionAspectInstance, int>();

        foreach ( var aspectInstance in aspectClasses.SelectMany( c => c.Instances ) )
        {
            aspectInstanceIds.Add( aspectInstance, aspectInstanceIds.Count + 1 );
        }

        var layers = result.AspectLayers.ToLookup( l => l.AspectClass.FullName, StringComparer.Ordinal );

        foreach ( var aspectClass in aspectClasses )
        {
            cancellationToken.ThrowIfCancellationRequested();

            writer.WriteStartAspectClass(
                new V2AspectClassData
                {
                    AspectClass = identifiers.GetAspectClass( aspectClass ), DisplayName = aspectClass.DisplayName, Description = aspectClass.Description
                } );

            foreach ( var layer in layers[aspectClass.FullName] )
            {
                writer.WriteAspectLayer( new V2AspectLayerData { Name = layer.LayerName, Order = layer.Order ?? 0 } );
            }

            foreach ( var aspectInstance in aspectClass.Instances )
            {
                WriteAspectInstance( writer, identifiers, aspectInstance, aspectInstanceIds );
            }

            writer.WriteEndAspectClass();
        }
    }

    private static void WriteAspectInstance(
        IntrospectionV2Writer writer,
        Identifiers identifiers,
        IIntrospectionAspectInstance aspectInstance,
        Dictionary<IIntrospectionAspectInstance, int> aspectInstanceIds )
    {
        var targetDeclaration = identifiers.Get( aspectInstance.TargetDeclaration );

        if ( targetDeclaration.IsNil )
        {
            // A declaration that has no serializable identifier cannot be found by a query.
            return;
        }

        writer.WriteStartAspectInstance(
            new V2AspectInstanceData
            {
                Id = aspectInstanceIds[aspectInstance],
                TargetDeclaration = targetDeclaration,
                Location = GetLocation( aspectInstance.TargetDeclaration ),
                IsSkipped = aspectInstance.IsSkipped
            } );

        foreach ( var relationship in aspectInstance.Predecessors )
        {
            if ( GetPredecessor( identifiers, relationship, aspectInstanceIds ) is { } predecessor )
            {
                writer.WritePredecessor( predecessor );
            }
        }

        foreach ( var advice in aspectInstance.Advice )
        {
            var layer = AspectLayerId.FromString( advice.AspectLayerId ).LayerName;

            foreach ( var transformation in advice.Transformations.OrderBy( t => t.Order ) )
            {
                var transformationTarget = identifiers.Get( transformation.TargetDeclaration );

                if ( transformationTarget.IsNil )
                {
                    continue;
                }

                writer.WriteTransformation(
                    new V2TransformationData
                    {
                        Kind = transformation.TransformationKind.ToString(),
                        AdviceKind = advice.AdviceKind.ToString(),
                        Layer = layer,
                        Order = transformation.Order,
                        TargetDeclaration = transformationTarget,
                        IntroducedDeclaration = identifiers.Get( transformation.IntroducedDeclaration ),
                        Description = transformation.Description.ToString( MetalamaStringFormatter.Instance )
                    } );
            }
        }

        foreach ( var diagnostic in aspectInstance.Diagnostics )
        {
            writer.WriteDiagnostic(
                new V2DiagnosticData
                {
                    Id = diagnostic.Id,
                    Severity = diagnostic.Severity switch
                    {
                        Severity.Error => V2DiagnosticSeverity.Error,
                        Severity.Warning => V2DiagnosticSeverity.Warning,
                        Severity.Info => V2DiagnosticSeverity.Info,
                        _ => V2DiagnosticSeverity.Hidden
                    },
                    Message = diagnostic.Message,
                    Declaration = identifiers.Get( diagnostic.Declaration ),
                    Location = diagnostic is IntrospectionDiagnostic { RoslynDiagnostic.Location: var location } ? GetLocation( location ) : null
                } );
        }

        writer.WriteEndAspectInstance();
    }

    private static V2PredecessorData? GetPredecessor(
        Identifiers identifiers,
        IntrospectionAspectRelationship relationship,
        Dictionary<IIntrospectionAspectInstance, int> aspectInstanceIds )
    {
        V2PredecessorKind kind;

        switch ( relationship.Kind )
        {
            case AspectPredecessorKind.Attribute:
                kind = V2PredecessorKind.Attribute;

                break;

            case AspectPredecessorKind.ChildAspect:
                kind = V2PredecessorKind.ChildAspect;

                break;

            case AspectPredecessorKind.RequiredAspect:
                kind = V2PredecessorKind.RequiredAspect;

                break;

            case AspectPredecessorKind.Inherited:
                kind = V2PredecessorKind.Inherited;

                break;

            case AspectPredecessorKind.Fabric:
                kind = V2PredecessorKind.Fabric;

                break;

            case AspectPredecessorKind.Interactive:
                kind = V2PredecessorKind.Interactive;

                break;

            default:
                return null;
        }

        var degree = relationship.Instance.PredecessorDegree;

        switch ( relationship.Instance )
        {
            case IIntrospectionAttributeAsPredecessor attribute:
                return new V2PredecessorData
                {
                    Kind = kind,
                    Degree = degree,
                    Declaration = identifiers.Get( attribute.Attribute.ContainingDeclaration ),
                    AttributeClass = identifiers.Get( attribute.Attribute.Type ),
                    Location = GetLocation( attribute.Attribute )
                };

            case IIntrospectionFabric fabric:
                return new V2PredecessorData { Kind = kind, Degree = degree, Declaration = identifiers.GetFabric( fabric.FullName ) };

            case IIntrospectionAspectInstance other when aspectInstanceIds.TryGetValue( other, out var otherId ):
                return new V2PredecessorData { Kind = kind, Degree = degree, AspectInstance = otherId };

            default:
                // An aspect instance that is not in the file, such as the aspect instance of a base declaration of another
                // assembly, from which this one is inherited: its target declaration identifies it.
                return new V2PredecessorData { Kind = kind, Degree = degree, Declaration = identifiers.Get( relationship.Instance.TargetDeclaration ) };
        }
    }

    private static V2SourceLocation? GetLocation( IDeclaration declaration ) => GetLocation( declaration.GetDiagnosticLocation() );

    private static V2SourceLocation? GetLocation( Location? location )
    {
        if ( location is not { IsInSource: true } )
        {
            return null;
        }

        var span = location.GetLineSpan();

        return new V2SourceLocation { File = span.Path, Line = span.StartLinePosition.Line + 1, Column = span.StartLinePosition.Character + 1 };
    }

    /// <summary>
    /// Gets the handles of the declarations of the file, by the identifiers of version 2: a <c>SerializableDeclarationId</c>, with
    /// the name of its assembly in brackets for a declaration of another assembly than the compilation.
    /// </summary>
    /// <remarks>
    /// Each identifier is formatted once per declaration, and the handles are cached by identifier, because several objects of
    /// the code model can stand for the same declaration.
    /// </remarks>
    private sealed class Identifiers
    {
        private static readonly Func<string, string> _identity = static s => s;

        private readonly IntrospectionV2Writer _writer;
        private readonly CompilationModel _compilation;
        private readonly string? _assemblyName;
        private readonly Dictionary<IDeclaration, DeclarationHandle> _byDeclaration = new();

        public Identifiers( IntrospectionV2Writer writer, CompilationModel compilation )
        {
            this._writer = writer;
            this._compilation = compilation;
            this._assemblyName = compilation.RoslynCompilation.AssemblyName;
        }

        /// <summary>
        /// Gets the handle of a declaration, or a nil handle when the declaration has no serializable identifier, as an attribute.
        /// </summary>
        public DeclarationHandle Get( IDeclaration? declaration )
        {
            if ( declaration == null )
            {
                return default;
            }

            if ( this._byDeclaration.TryGetValue( declaration, out var handle ) )
            {
                return handle;
            }

            string identifier;

            try
            {
                identifier = declaration.ToSerializableId().Id;
            }
            catch ( NotSupportedException )
            {
                identifier = "";
            }

            handle = this.GetHandle( identifier, declaration.DeclaringAssembly.Identity.Name );
            this._byDeclaration.Add( declaration, handle );

            return handle;
        }

        /// <summary>
        /// Gets the handle of the type of an aspect class, from the type symbol of the compilation when it resolves its name.
        /// </summary>
        public DeclarationHandle GetAspectClass( IAspectClass aspectClass )
        {
            var symbol = this._compilation.RoslynCompilation.GetTypeByMetadataName( aspectClass.FullName );

            if ( symbol != null && DocumentationCommentId.CreateDeclarationId( symbol ) is { } documentationId )
            {
                return this.GetHandle( documentationId, symbol.ContainingAssembly.Name );
            }

            return this.GetHandle( "T:" + aspectClass.FullName.Replace( '+', '.' ), this._assemblyName );
        }

        /// <summary>
        /// Gets the handle of the type of a fabric, from its full name. A fabric is declared in the project.
        /// </summary>
        public DeclarationHandle GetFabric( string fullName ) => this.GetHandle( "T:" + fullName.Replace( '+', '.' ), this._assemblyName );

        private DeclarationHandle GetHandle( string identifier, string? assemblyName )
        {
            if ( string.IsNullOrEmpty( identifier ) )
            {
                return default;
            }

            if ( assemblyName != null && !string.Equals( assemblyName, this._assemblyName, StringComparison.Ordinal ) )
            {
                identifier = "[" + assemblyName + "]" + identifier;
            }

            return this._writer.GetDeclarationHandle( identifier, _identity );
        }
    }
}
