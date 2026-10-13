// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NET5_0_OR_GREATER
using JetBrains.Annotations;
using Metalama.Backstage.Utilities;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// A non-collectible <see cref="AssemblyLoadContext"/> that loads each shared assembly (see <see cref="LoadAssemblyOptions.IsShared"/>)
    /// once and makes it available to several <see cref="UnloadableCompileTimeDomain"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Without this context, every <see cref="UnloadableCompileTimeDomain"/> loads its own copy of every extension assembly. Tests create
    /// a new domain for each test, so the same extension assemblies are loaded and compiled by the JIT compiler again for every test.
    /// </para>
    /// <para>
    /// An assembly is reused only when it is requested from the same path and the file has not changed since it was loaded. In any other
    /// case, <see cref="GetOrLoad"/> returns <c>null</c> and the caller loads the assembly into its own context, as it would without this
    /// class. This happens when the file has been rebuilt, or when an assembly of the same name has already been loaded from another path.
    /// </para>
    /// <para>
    /// Assemblies are loaded from a stream so that the files are not locked.
    /// </para>
    /// </remarks>
    [PublicAPI]
    public sealed class SharedAssemblyLoadContext : AssemblyLoadContext
    {
        /// <summary>
        /// The assemblies loaded by this context, indexed by full path.
        /// </summary>
        private readonly ConcurrentDictionary<string, Entry> _entriesByPath = new( StringComparer.OrdinalIgnoreCase );

        /// <summary>
        /// The assemblies loaded by this context, indexed by simple name, used to resolve references between them.
        /// </summary>
        private readonly ConcurrentDictionary<string, Assembly> _assembliesByName = new( StringComparer.OrdinalIgnoreCase );

        /// <summary>
        /// The lock that serializes loads into this context.
        /// </summary>
        /// <remarks>
        /// Each assembly is loaded only once per process, so contention on this lock is negligible. The lock prevents two threads from
        /// loading the same assembly concurrently, which would fail because an <see cref="AssemblyLoadContext"/> cannot contain two
        /// assemblies of the same name.
        /// </remarks>
        private readonly object _loadSync = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAssemblyLoadContext"/> class.
        /// </summary>
        public SharedAssemblyLoadContext() : base( "Metalama_Shared", isCollectible: false ) { }

        /// <summary>
        /// Gets the assembly loaded from a given path, loading it if necessary, or returns <c>null</c> when the assembly cannot be shared.
        /// </summary>
        /// <param name="path">The full path of the assembly.</param>
        /// <returns>The shared assembly, or <c>null</c> when the file has changed since it was loaded, or when an assembly of the same
        /// name has already been loaded from another path.</returns>
        public Assembly? GetOrLoad( string path )
        {
            var fileInfo = new FileInfo( path );

            if ( this._entriesByPath.TryGetValue( path, out var entry ) )
            {
                return entry.Matches( fileInfo ) ? entry.Assembly : null;
            }

            lock ( this._loadSync )
            {
                if ( this._entriesByPath.TryGetValue( path, out entry ) )
                {
                    return entry.Matches( fileInfo ) ? entry.Assembly : null;
                }

                var assemblyName = MetadataReferenceCache.GetAssemblyName( path );

                if ( this._assembliesByName.ContainsKey( assemblyName.Name! ) )
                {
                    // An assembly of the same name has been loaded from another path. This context cannot hold a second one.
                    return null;
                }

                var assembly = LoadFromFile( this, path );

                this._assembliesByName[assemblyName.Name!] = assembly;
                this._entriesByPath[path] = new Entry( assembly, fileInfo.LastWriteTimeUtc, fileInfo.Length );

                return assembly;
            }
        }

        /// <summary>
        /// Resolves a reference from one shared assembly to another one, for instance from <c>DiffEngine</c> to <c>EmptyFiles</c>.
        /// </summary>
        /// <remarks>
        /// When the assembly is not one of the shared assemblies, this method returns <c>null</c>, and the runtime resolves the reference
        /// from the default context, which contains Metalama and Roslyn.
        /// </remarks>
        protected override Assembly? Load( AssemblyName assemblyName )
        {
            if ( assemblyName.Name != null
                 && this._assembliesByName.TryGetValue( assemblyName.Name, out var assembly )
                 && (assemblyName.Version == null || assembly.GetName().Version >= assemblyName.Version) )
            {
                return assembly;
            }

            return null;
        }

        /// <summary>
        /// Loads an assembly and its symbol file, when it exists, from a stream into a given <see cref="AssemblyLoadContext"/>.
        /// </summary>
        /// <remarks>
        /// The assembly is loaded from a stream rather than with <see cref="AssemblyLoadContext.LoadFromAssemblyPath"/>. Loading from the
        /// path locks the file, even after the context is unloaded, and was observed to be slow in tests.
        /// </remarks>
        internal static Assembly LoadFromFile( AssemblyLoadContext context, string path )
        {
            using var peStream = RetryHelper.Retry( () => File.OpenRead( path ) );
            var pdbPath = Path.ChangeExtension( path, ".pdb" );
            using var pdbStream = File.Exists( pdbPath ) ? RetryHelper.Retry( () => File.OpenRead( pdbPath ) ) : null;

            return context.LoadFromStream( peStream, pdbStream );
        }

        /// <summary>
        /// A shared assembly and the state of its file when it was loaded.
        /// </summary>
        private sealed class Entry
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Entry"/> class.
            /// </summary>
            public Entry( Assembly assembly, DateTime lastWriteTimeUtc, long length )
            {
                this.Assembly = assembly;
                this.LastWriteTimeUtc = lastWriteTimeUtc;
                this.Length = length;
            }

            /// <summary>
            /// Gets the loaded assembly.
            /// </summary>
            public Assembly Assembly { get; }

            /// <summary>
            /// Gets the last write time of the file when the assembly was loaded.
            /// </summary>
            private DateTime LastWriteTimeUtc { get; }

            /// <summary>
            /// Gets the length of the file when the assembly was loaded.
            /// </summary>
            private long Length { get; }

            /// <summary>
            /// Determines whether the file is still the one from which the assembly was loaded.
            /// </summary>
            public bool Matches( FileInfo fileInfo ) => fileInfo.Exists && fileInfo.LastWriteTimeUtc == this.LastWriteTimeUtc && fileInfo.Length == this.Length;
        }
    }
}
#endif
