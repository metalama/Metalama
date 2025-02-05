// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Utilities.Roslyn;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Metalama.Framework.Engine.Utilities
{
    internal static class MetadataReader
    {
        private static readonly ConcurrentDictionary<string, MetadataInfo> _cache = new();

        public static bool TryGetMetadata(
            string path,
            [NotNullWhen( true )] out MetadataInfo? metadataInfo )
        {
            if ( !(_cache.TryGetValue( path, out metadataInfo ) && metadataInfo.LastFileWrite == File.GetLastWriteTime( path )) )
            {
                if ( !File.Exists( path ) )
                {
                    metadataInfo = null;

                    return false;
                }

                metadataInfo = GetMetadataCore( path );
                _cache.TryAdd( path, metadataInfo );
            }

            return true;
        }

        private static MetadataInfo GetMetadataCore( string path )
        {
            var timestamp = File.GetLastWriteTime( path );
            var assemblyIdentity = AssemblyName.GetAssemblyName( path ).ToAssemblyIdentity();

            using var stream = new FileStream( path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
            using var peReader = new PEReader( stream );

            var metadataReader = peReader.GetMetadataReader();

            // Read resources.
            ImmutableDictionary<string, byte[]>.Builder? resourcesDictionaryBuilder = null;

            foreach ( var resourceHandle in metadataReader.ManifestResources )
            {
                var resource = metadataReader.GetManifestResource( resourceHandle );

                if ( !resource.Implementation.IsNil )
                {
                    // Coverage: ignore
                    // (This happens in the case that the resource is stored in a different module of the assembly, but this is a very rare
                    // case that cannot be easily tested without creating custom IL.)

                    continue;
                }

                var resourceName = metadataReader.GetString( resource.Name );

                if ( resourceName is CompileTimeConstants.CompileTimeProjectResourceName or CompileTimeConstants.InheritableAspectManifestResourceName )
                {
                    unsafe
                    {
                        resourcesDictionaryBuilder ??= ImmutableDictionary.CreateBuilder<string, byte[]>();
                        var resourcesSection = peReader.GetSectionData( peReader.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress );
                        var size = *(int*) (resourcesSection.Pointer + resource.Offset);
                        var resourceBytes = new byte[size];

                        fixed ( byte* fixedResourceBytes = resourceBytes )
                        {
                            Buffer.MemoryCopy( resourcesSection.Pointer + resource.Offset + 4, fixedResourceBytes, size, size );
                        }

                        resourcesDictionaryBuilder.Add( resourceName, resourceBytes );
                    }
                }
            }

            // Read assembly custom attributes.
            var hasCompileTimeAttribute = false;
            var assemblyDefinition = metadataReader.GetAssemblyDefinition();

            foreach ( var attributeHandle in assemblyDefinition.GetCustomAttributes() )
            {
                var attribute = metadataReader.GetCustomAttribute( attributeHandle );

                if ( attribute.Constructor.Kind == HandleKind.MemberReference )
                {
                    var constructor = metadataReader.GetMemberReference( (MemberReferenceHandle) (Handle) attribute.Constructor );
                    var type = metadataReader.GetTypeReference( (TypeReferenceHandle) constructor.Parent );
                    var typeName = metadataReader.GetString( type.Name );

                    if ( typeName == nameof(CompileTimeAttribute) )
                    {
                        hasCompileTimeAttribute = true;

                        break;
                    }
                }
            }

            return new MetadataInfo(
                assemblyIdentity,
                timestamp,
                resourcesDictionaryBuilder?.ToImmutable() ?? ImmutableDictionary<string, byte[]>.Empty,
                hasCompileTimeAttribute );
        }
    }
}