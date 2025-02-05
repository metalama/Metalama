// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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