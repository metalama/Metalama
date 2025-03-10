// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using System;
using System.Reflection;

namespace Metalama.Framework.Engine.CompileTime
{
    internal static class MetadataReferenceCache
    {
        private static readonly FileBasedCache<PortableExecutableReference> _metadataReferences = new( TimeSpan.FromMinutes( 10 ) );
        private static readonly FileBasedCache<AssemblyName> _assemblyNames = new( TimeSpan.FromMinutes( 10 ) );

        public static PortableExecutableReference GetMetadataReference( string path )
            => _metadataReferences.GetOrAdd( path, p => MetadataReference.CreateFromFile( p ) );

        public static AssemblyName GetAssemblyName( string path ) => _assemblyNames.GetOrAdd( path, AssemblyName.GetAssemblyName );
    }
}