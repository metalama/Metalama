// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Options;
using Metalama.Framework.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;

namespace Metalama.Framework.Engine.Aspects;

public sealed class TransitiveAspectsManifest : ITransitiveAspectsManifest
{
    public ImmutableDictionary<string, IReadOnlyList<InheritableAspectInstance>> InheritableAspects { get; private set; }

    public ImmutableArray<ITransitiveAspectsManifestExtension> Extensions { get; private set; }

    // To levels of mapping of options: first option types, then target declaration.
    public ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions> InheritableOptions { get; private set; }

    public ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation> Annotations { get; private set; }

    public bool ContainsInitializableTypes { get; private set; }

    // Deserializer constructor.
    private TransitiveAspectsManifest()
    {
        this.InheritableAspects = null!;
        this.InheritableOptions = null!;
        this.Annotations = null!;
    }

    private TransitiveAspectsManifest(
        ImmutableDictionary<string, IReadOnlyList<InheritableAspectInstance>> inheritableAspects,
        ImmutableArray<ITransitiveAspectsManifestExtension> extensions,
        ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions> options,
        ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation> annotations,
        bool containsInitializableTypes )
    {
        this.InheritableAspects = inheritableAspects;
        this.Extensions = extensions;
        this.InheritableOptions = options;
        this.Annotations = annotations;
        this.ContainsInitializableTypes = containsInitializableTypes;
    }

    public static TransitiveAspectsManifest Create(
        ImmutableArray<InheritableAspectInstance> inheritedAspects,
        ImmutableArray<ITransitiveAspectsManifestExtension> extensions,
        ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions> options,
        ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation> annotations,
        bool containsInitializableTypes )
        => new(
            inheritedAspects.GroupBy( a => a.AspectClass )
                .ToImmutableDictionary(
                    g => g.Key.FullName,
                    g => g.Select( i => i )
                        .ToReadOnlyList(),
                    StringComparer.Ordinal ),
            extensions,
            options,
            annotations,
            containsInitializableTypes );

    // Compression makes sense only when the manifest is embedded in a PE binary (a persisted artifact). For the
    // design-time in-process path, where the bytes are produced and consumed in the same process and thrown away
    // immediately, it is pure overhead, so that path serializes uncompressed. The two formats are distinguished on
    // read by a leading marker (see Deserialize); the compressed format is unchanged and markerless, so already-built
    // assemblies and cross-version peers keep working.
    private void Serialize( Stream stream, in ProjectServiceProvider serviceProvider, bool compress )
    {
        using ( UserCodeExecutionContext.WithContext(
                   UserCodeExecutionContext.CreateInstance( serviceProvider, UserCodeDescription.Create( "Serializing" ) ) ) )
        {
            var formatter = new CompileTimeSerializer( serviceProvider );

            if ( compress )
            {
                using var deflate = new DeflateStream( stream, CompressionLevel.Optimal, true );
                formatter.Serialize( this, deflate );
                deflate.Flush();
            }
            else
            {
                stream.WriteByte( SerializationProtocol.UncompressedStreamMarker );
                formatter.Serialize( this, stream );
            }

            stream.Flush();
        }
    }

    public byte[] ToBytes( in ProjectServiceProvider serviceProvider, bool compress )
    {
        var stream = new MemoryStream();
        this.Serialize( stream, serviceProvider, compress );

        return stream.ToArray();
    }

    public ImmutableArray<byte> ToImmutableBytes( in ProjectServiceProvider serviceProvider, bool compress )
        => ImmutableCollectionsMarshal.AsImmutableArray( this.ToBytes( serviceProvider, compress ) );

    internal ManagedResource ToResource( in ProjectServiceProvider serviceProvider )
    {
        // Embedded in the PE binary, so compressed.
        var bytes = this.ToBytes( serviceProvider, compress: true );

        return new ManagedResource(
            CompileTimeConstants.InheritableAspectManifestResourceName,
            bytes,
            true );
    }

    public static TransitiveAspectsManifest Deserialize(
        Stream stream,
        in ProjectServiceProvider serviceProvider,
        string? assemblyName )
    {
        var description = assemblyName != null
            ? $"Deserializing transitive aspects from '{assemblyName}'."
            : "Deserializing transitive aspects from a referenced assembly.";

        using ( UserCodeExecutionContext.WithContext(
                   UserCodeExecutionContext.CreateInstance( serviceProvider, UserCodeDescription.Create( description ) ) ) )
        {
            var formatter = new CompileTimeSerializer( serviceProvider );

            // Peek the first byte: the uncompressed format starts with a marker that can never begin a DEFLATE stream.
            // Consume it and read the raw graph if it is present; otherwise leave it in place and treat the whole stream
            // as a legacy DEFLATE stream. The callers pass a seekable MemoryStream, so the byte can be un-peeked.
            var firstByte = stream.ReadByte();

            if ( firstByte == SerializationProtocol.UncompressedStreamMarker )
            {
                return (TransitiveAspectsManifest) formatter.Deserialize( stream, assemblyName ).AssertNotNull();
            }

            if ( firstByte >= 0 )
            {
                stream.Position -= 1;
            }

            using var deflate = new DeflateStream( stream, CompressionMode.Decompress );

            return (TransitiveAspectsManifest) formatter.Deserialize( deflate, assemblyName ).AssertNotNull();
        }
    }

    public IEnumerable<string> InheritableAspectTypes => this.InheritableAspects.Keys;

    public IEnumerable<InheritableAspectInstance> GetInheritableAspects( string aspectType ) => this.InheritableAspects[aspectType];

    // ReSharper disable once UnusedType.Local
    private class Serializer : ReferenceTypeSerializer
    {
        public override object CreateInstance( Type type, IArgumentsReader constructorArguments ) => new TransitiveAspectsManifest();

        public override void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
        {
            var instance = (TransitiveAspectsManifest) obj;
            initializationArguments.SetValue( nameof(instance.InheritableAspects), instance.InheritableAspects );
            initializationArguments.SetValue( nameof(instance.Extensions), instance.Extensions );
            initializationArguments.SetValue( nameof(instance.InheritableOptions), instance.InheritableOptions );
            initializationArguments.SetValue( nameof(instance.Annotations), instance.Annotations.ToImmutableDictionary() );
            initializationArguments.SetValue( nameof(instance.ContainsInitializableTypes), instance.ContainsInitializableTypes );
        }

        public override void DeserializeFields( object obj, IArgumentsReader initializationArguments )
        {
            var instance = (TransitiveAspectsManifest) obj;

            // Fields use null-coalescing to provide defaults for backward compatibility with manifests
            // serialized by older Metalama versions that may not have all fields. (#728)

            instance.InheritableAspects =
                initializationArguments.GetValue<ImmutableDictionary<string, IReadOnlyList<InheritableAspectInstance>>>( nameof(instance.InheritableAspects) )
                ?? ImmutableDictionary<string, IReadOnlyList<InheritableAspectInstance>>.Empty;

            instance.Extensions =
                initializationArguments.TryGetValue<ImmutableArray<ITransitiveAspectsManifestExtension>>( nameof(instance.Extensions), out var extensions )
                    ? extensions
                    : ImmutableArray<ITransitiveAspectsManifestExtension>.Empty;

            instance.InheritableOptions =
                initializationArguments.GetValue<ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions>>( nameof(instance.InheritableOptions) )
                ?? ImmutableDictionary<HierarchicalOptionsKey, IHierarchicalOptions>.Empty;

            if ( initializationArguments.TryGetValue<ImmutableDictionary<SerializableDeclarationId, ImmutableArray<IAnnotation>>>(
                     nameof(instance.Annotations),
                     out var annotations )
                 && annotations != null )
            {
                instance.Annotations = new ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation>( annotations );
            }
            else
            {
                instance.Annotations = ImmutableDictionaryOfArray<SerializableDeclarationId, IAnnotation>.Empty;
            }

            instance.ContainsInitializableTypes =
                initializationArguments.TryGetValue<bool>( nameof(instance.ContainsInitializableTypes), out var containsInitializableTypes )
                && containsInitializableTypes;
        }
    }
}