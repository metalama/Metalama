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
using Metalama.Framework.Engine.Testing;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Options;
using Metalama.Framework.Serialization;
using Metalama.Testing.Hooks;
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
        // Grouped by the aspect class NAME and not by the aspect class itself. Two references can each contribute
        // their own IAspectClass instance for one type name, which happens when two projects of a solution produce
        // the same assembly name: grouping by the instance then yields two groups that map to a single key, and
        // ToImmutableDictionary throws (issue #1749). Merging them is the only meaningful answer here, because the
        // manifest is keyed by name and a consumer resolves an inherited aspect by name.
        // The order of the instances of a group is defined by Serialize, which is where it matters (#2248).
        => new(
            inheritedAspects.GroupBy( a => a.AspectClass.FullName, StringComparer.Ordinal )
                .ToImmutableDictionary(
                    g => g.Key,
                    g => g.Select( i => i )
                        .ToReadOnlyList(),
                    StringComparer.Ordinal ),
            extensions,
            options,
            annotations,
            containsInitializableTypes );

    /// <summary>
    /// Serializes this manifest, optionally compressed. Compression makes sense only when the manifest is embedded
    /// in a PE binary (a persisted artifact). For the design-time in-process path, where the bytes are produced and
    /// consumed in the same process and thrown away immediately, it is pure overhead, so that path serializes
    /// uncompressed. The two formats are distinguished on read by a leading marker (see <see cref="Deserialize"/>);
    /// the compressed format is unchanged and markerless, so already-built assemblies and cross-version peers keep
    /// working.
    /// </summary>
    private void Serialize( Stream stream, in ProjectServiceProvider serviceProvider, bool compress )
    {
        using ( UserCodeExecutionContext.WithContext(
                   UserCodeExecutionContext.CreateInstance( serviceProvider, UserCodeDescription.Create( "Serializing" ) ) ) )
        {
            var formatter = new CompileTimeSerializer( serviceProvider );
            var manifest = this.WithOrderedInheritableAspects( formatter );

            if ( compress )
            {
                using var deflate = new DeflateStream( stream, CompressionLevel.Optimal, true );
                formatter.Serialize( manifest, deflate );
                deflate.Flush();
            }
            else
            {
                stream.WriteByte( SerializationProtocol.UncompressedStreamMarker );
                formatter.Serialize( manifest, stream );
            }

            stream.Flush();
        }
    }

    /// <summary>
    /// Returns a copy of this manifest in which the instances of each aspect class are in a deterministic order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pipeline produces the instances in an undefined order, and the serialized manifest must be identical for identical
    /// source (#2248). The instances are sorted by the identifier of their target declaration.
    /// </para>
    /// <para>
    /// Several instances can share a target declaration. For example, a type can inherit two instances of one aspect class
    /// with different values, one from each of two base interfaces. These instances are sorted by their serialized content,
    /// because no other property distinguishes them in every case. Two instances with the same content can be written in any
    /// order. The content is serialized only for the instances that share a target declaration.
    /// </para>
    /// </remarks>
    private TransitiveAspectsManifest WithOrderedInheritableAspects( CompileTimeSerializer formatter )
    {
        var inheritableAspects = this.InheritableAspects.ToImmutableDictionary(
            x => x.Key,
            x => OrderInstances( x.Value, formatter ),
            this.InheritableAspects.KeyComparer );

        return new TransitiveAspectsManifest(
            inheritableAspects,
            this.Extensions,
            this.InheritableOptions,
            this.Annotations,
            this.ContainsInitializableTypes );
    }

    private static IReadOnlyList<InheritableAspectInstance> OrderInstances(
        IReadOnlyList<InheritableAspectInstance> instances,
        CompileTimeSerializer formatter )
    {
        if ( instances.Count < 2 )
        {
            return instances;
        }

        var instancesByTarget = instances.Select( i => (Instance: i, TargetId: i.TargetDeclaration.ToSerializableId().Id) )
            .OrderBy( x => x.TargetId, StringComparer.Ordinal )
            .ToList();

        var orderedInstances = new List<InheritableAspectInstance>( instancesByTarget.Count );

        for ( var start = 0; start < instancesByTarget.Count; )
        {
            var end = start + 1;

            while ( end < instancesByTarget.Count
                    && string.Equals( instancesByTarget[end].TargetId, instancesByTarget[start].TargetId, StringComparison.Ordinal ) )
            {
                end++;
            }

            if ( end - start == 1 )
            {
                orderedInstances.Add( instancesByTarget[start].Instance );
            }
            else
            {
                orderedInstances.AddRange(
                    instancesByTarget.Skip( start )
                        .Take( end - start )
                        .Select( x => (x.Instance, Content: SerializeInstance( x.Instance )) )
                        .OrderBy( x => x.Content, ByteArrayComparer.Instance )
                        .Select( x => x.Instance ) );
            }

            start = end;
        }

        return orderedInstances;

        byte[] SerializeInstance( InheritableAspectInstance instance )
        {
            var stream = new MemoryStream();
            formatter.Serialize( instance, stream );

            return stream.ToArray();
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
        // Test-only fault injection point, exercising the handling of a manifest that cannot be read. No-op in
        // production. Resolved untyped, because ITestFaultInjector is shared with the other layers and therefore cannot
        // derive from IGlobalService. See #2049.
        ((ITestFaultInjector?) serviceProvider.Global.Underlying.GetService( typeof(ITestFaultInjector) ))
            ?.InjectFault( FaultInjectionPoints.TransitiveManifestDeserialization );

        var description = assemblyName != null
            ? $"Deserializing transitive aspects from '{assemblyName}'."
            : "Deserializing transitive aspects from a referenced assembly.";

        using ( UserCodeExecutionContext.WithContext( UserCodeExecutionContext.CreateInstance( serviceProvider, UserCodeDescription.Create( description ) ) ) )
        {
            var formatter = new CompileTimeSerializer( serviceProvider );

            // Peek the first byte: the uncompressed format starts with a marker that can never begin a DEFLATE stream.
            // Consume it and read the raw graph if it is present; otherwise leave it in place and treat the whole stream
            // as a legacy DEFLATE stream. The callers pass a seekable MemoryStream, so the byte can be un-peeked.
            var firstByte = stream.ReadByte();

            if ( firstByte == SerializationProtocol.UncompressedStreamMarker )
            {
                return DeserializeStream( stream );
            }
            else
            {
                stream.Position -= 1;
                using var deflateStream = new DeflateStream( stream, CompressionMode.Decompress );

                return DeserializeStream( deflateStream );
            }

            TransitiveAspectsManifest DeserializeStream( Stream s ) => (TransitiveAspectsManifest) formatter.Deserialize( s, assemblyName ).AssertNotNull();
        }
    }

    public IEnumerable<string> InheritableAspectTypes => this.InheritableAspects.Keys;

    /// <summary>
    /// Compares two byte arrays in lexicographic order.
    /// </summary>
    private sealed class ByteArrayComparer : IComparer<byte[]>
    {
        public static ByteArrayComparer Instance { get; } = new();

        public int Compare( byte[]? x, byte[]? y )
        {
            if ( ReferenceEquals( x, y ) )
            {
                return 0;
            }

            if ( x == null )
            {
                return -1;
            }

            if ( y == null )
            {
                return 1;
            }

            var length = Math.Min( x.Length, y.Length );

            for ( var i = 0; i < length; i++ )
            {
                var comparison = x[i].CompareTo( y[i] );

                if ( comparison != 0 )
                {
                    return comparison;
                }
            }

            return x.Length.CompareTo( y.Length );
        }
    }

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