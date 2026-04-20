// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Utilities;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.AssemblyLoaders;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

// Resharper disable ClassWithVirtualMembersNeverInherited.Global

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// Tracks compile-time assemblies belonging to the same domain and implements CLR assembly resolution.
    /// Each <see cref="CompileTimeDomain"/> has a single <see cref="AssemblyLoader"/> (and, on .NET 5+, a single <c>AssemblyLoadContext</c>).
    /// Domains are managed by <see cref="ICompileTimeDomainFactory"/>, which decides whether to reuse an existing domain
    /// or create a new one based on assembly compatibility.
    /// </summary>
    /// <remarks>
    /// <para><b>Lifecycle:</b> Domains are created by <see cref="ICompileTimeDomainFactory"/> and are not disposed explicitly
    /// during normal operation. When a domain becomes incompatible with new assembly versions, the factory creates a new domain
    /// and the old domain becomes eligible for garbage collection once no compilation holds a reference to it. In tests,
    /// domains are disposed explicitly via <c>TestContext.Dispose</c>.</para>
    /// </remarks>
    public class CompileTimeDomain : IDisposable
    {
        private static readonly ConcurrentDictionary<string, object> _locksByPath = new();
        private static int _nextDomainId;
        private readonly ConcurrentDictionary<AssemblyIdentity, Assembly> _assemblyCache = new();
        private readonly int _domainId = Interlocked.Increment( ref _nextDomainId );

        /// <summary>
        /// Gets the unique identifier for this domain instance, used by <see cref="ICompileTimeDomainFactory"/>
        /// to track domains via weak references.
        /// </summary>
        public Guid Guid { get; } = Guid.NewGuid();

        private readonly ILogger _logger;
        private readonly object _sync = new();
        private readonly ConcurrentDictionary<string, (Assembly Assembly, AssemblyIdentity Identity)> _assembliesByName = new();

        private AssemblyLoader? _assemblyLoader;

        private ImmutableDictionaryOfArray<string, (string Path, Lazy<AssemblyName> AssemblyName)> _assemblyPathsByName =
            ImmutableDictionaryOfArray<string, (string Path, Lazy<AssemblyName> AssemblyName)>.Empty;

        [UsedImplicitly]
        protected ICompileTimeDomainObserver? Observer { get; }

        public CompileTimeDomain( GlobalServiceProvider serviceProvider, string? debugName = null )
        {
            this.Observer = serviceProvider.GetService<ICompileTimeDomainObserver>();
            this.Observer?.OnDomainCreated( this );

            this._assemblyLoader = AssemblyLoaderFactory.CreateAssemblyLoader(
                this.ResolveAssembly,
                debugName: $"CompileTimeDomain {debugName}".TrimEnd() );

            this._logger = Logger.Domain;

            this.AddAssembly( this.GetType().Assembly );
        }

        private Assembly? ResolveAssembly( string name )
        {
            this._logger.Trace?.Log( $"Resolving the assembly '{name}'." );

            var assemblyName = new AssemblyName( name );

            if ( this._assembliesByName.TryGetValue( assemblyName.Name.AssertNotNull(), out var candidateAssembly )
                 && AssemblyName.ReferenceMatchesDefinition( assemblyName, candidateAssembly.Assembly.GetName() ) )
            {
                this._logger.Trace?.Log( $"Found the assembly '{candidateAssembly.Assembly.Location}'." );

                return candidateAssembly.Assembly;
            }
            else
            {
                var matchingAssemblies = this._assemblyPathsByName[assemblyName.Name.AssertNotNull()]
                    .Where( x => AssemblyName.ReferenceMatchesDefinition( assemblyName, x.AssemblyName.Value ) )
                    .ToOrderedList( x => x.AssemblyName.Value.Version, descending: true );

                if ( matchingAssemblies.Count >= 1 )
                {
                    this._logger.Trace?.Log( $"Found the assembly '{matchingAssemblies[0].Path}'." );

                    return this.LoadAssembly( matchingAssemblies[0].Path, matchingAssemblies[0].AssemblyName.Value );
                }
            }

            this._logger.Warning?.Log( $"Could not find the assembly '{name}'." );

            return null;
        }

        // ReSharper disable once VirtualMemberNeverOverridden.Global, 

        /// <summary>
        /// Loads an assembly in the CLR.
        /// </summary>
        /// <param name="path">The assembly path.</param>
        /// <param name="assemblyName">The <see cref="AssemblyName"/> if known; otherwise <c>null</c>. The objective is to minimize calls
        /// to <see cref="AssemblyName.GetAssemblyName"/> because they are expensive.</param>
        public Assembly LoadAssembly( string path, AssemblyName? assemblyName = null, LoadAssemblyOptions options = default )
        {
            this._logger.Trace?.Log( $"Loading '{path}'." );

            // We intentionally do not cache assemblies by path because files can be rewritten. We can only cache based on the full identity.

            assemblyName ??= MetadataReferenceCache.GetAssemblyName( path );

            // Verify that the assembly is not already in the current CompileTimeDomain.
            if ( this.TryGetLoadedAssembly( assemblyName, out var existingAssembly ) )
            {
                return existingAssembly;
            }

            // The assembly might already be loaded in the AppDomain or AssemblyLoadContext.
            if ( this._assemblyLoader!.TryGetLoadedAssembly( assemblyName, out existingAssembly ) )
            {
                this.AddAssembly( existingAssembly! );
            }

            // Take a lock to avoid concurrently loading the same assembly twice, which may corrupts the CLR.
            var @lock = _locksByPath.GetOrAdd( path, _ => new object() );

            Assembly assembly;

            lock ( @lock )
            {
                // Loads the assembly.
                assembly = this.LoadAssemblyCore( path, options );

                // Adds the assembly to our collections, including _assembliesByPath.
                this.AddAssembly( assembly );
            }

            // Removing the lock.
            _locksByPath.TryRemove( path, out _ );

            return assembly;
        }

        protected virtual Assembly LoadAssemblyCore( string path, LoadAssemblyOptions options )
        {
            var assemblyLoader = this._assemblyLoader ?? throw new ObjectDisposedException( this.ToString() );

            try
            {
                if ( options.AvoidLocking )
                {
                    // We use LoadFromStream to avoid to lock the file in the long-running compiler process.

                    using var peStream = RetryHelper.Retry( () => File.OpenRead( path ) );
                    var pdbPath = Path.ChangeExtension( path, ".pdb" );
                    using var pdbStream = File.Exists( pdbPath ) ? RetryHelper.Retry( () => File.OpenRead( pdbPath ) ) : null;

                    return assemblyLoader.LoadFromStream( peStream, pdbStream );
                }
                else
                {
                    return assemblyLoader.LoadFromPath( path );
                }
            }
            catch ( Exception e )
            {
                throw new FileLoadException( $"Cannot load '{path}': {e.Message}", e );
            }
        }

        public bool TryGetLoadedAssembly( string assemblyQualifiedName, [NotNullWhen( true )] out Assembly? assembly )
            => this.TryGetLoadedAssembly( new AssemblyName( assemblyQualifiedName ), out assembly );

        public bool TryGetLoadedAssembly( AssemblyName assemblyName, [NotNullWhen( true )] out Assembly? assembly )
        {
            if ( !this._assembliesByName.TryGetValue( assemblyName.Name.AssertNotNull(), out var assemblyInfo ) )
            {
                assembly = null;

                return false;
            }

            if ( assemblyName.Version != null && assemblyInfo.Identity.Version != assemblyName.Version )
            {
                this._logger.Warning?.Log(
                    $"We could find an assembly named '{assemblyName.Name}', but it has version '{assemblyInfo.Identity.Version}' instead of '{assemblyName.Version}'." );

                assembly = null;

                return false;
            }

            assembly = assemblyInfo.Assembly;

            return true;
        }

        internal void AddAssembly( Assembly assembly )
        {
            var assemblyIdentity = assembly.GetName().ToAssemblyIdentity();

            if ( this._assemblyCache.TryAdd( assemblyIdentity, assembly ) )
            {
                if ( !this._assembliesByName.TryAdd( assemblyIdentity.Name.AssertNotNull(), (assembly, assemblyIdentity) ) )
                {
                    this._assembliesByName.TryGetValue( assemblyIdentity.Name, out var existingAssembly );

                    throw new AssertionFailedException(
                        $"Cannot add '{assemblyIdentity}': A different assembly of the same name ('{existingAssembly.Identity}') was already added." );
                }
            }
        }

        /// <summary>
        /// Gets an assembly given its <see cref="AssemblyIdentity"/> and image, or loads it.
        /// </summary>
        internal Assembly GetOrLoadAssembly( AssemblyIdentity compileTimeIdentity, string path )
        {
            var assembly = this._assemblyCache.GetOrAdd(
                compileTimeIdentity,
                static ( _, ctx ) =>
                {
                    ctx.me._logger.Trace?.Log( $"Loading assembly '{ctx.path}'." );

                    var assembly = ctx.me.LoadAssembly( ctx.path, ctx.compileTimeIdentity.ToAssemblyName() );

                    if ( assembly.FullName != ctx.compileTimeIdentity.ToString() )
                    {
                        throw new AssertionFailedException(
                            $"Assembly identify mismatch: the expected identity is '{ctx.compileTimeIdentity}', but the identity of the loaded assembly is '{assembly.FullName}'." );
                    }

                    return assembly;
                },
                (me: this, compileTimeIdentity, path) );

            // CompileTimeDomain is used only for compile-time assemblies, which always have a unique name, so we can have safely
            // index assemblies by name only.
            this._assembliesByName.AddOrUpdate(
                compileTimeIdentity.Name,
                _ => (assembly, compileTimeIdentity),
                ( name, value ) =>
                {
                    if ( !value.Identity.Equals( compileTimeIdentity ) )
                    {
                        throw new AssertionFailedException( $"Cannot load two assemblies of the same name '{name}'." );
                    }

                    return value;
                } );

            return assembly;
        }

        public override string ToString() => this._domainId.ToString( CultureInfo.InvariantCulture );

        protected virtual void Dispose( bool disposing )
        {
            if ( disposing )
            {
                // Clear all collections to make sure that we don't hold references to assemblies, as a derived class
                // may want to collect them.
                this._assemblyCache.Clear();
                this._assembliesByName.Clear();

                this._assemblyLoader?.Dispose();
                this._assemblyLoader = null;
            }
        }

        public void Dispose() => this.Dispose( true );

        internal void RegisterAssemblyPaths( ImmutableArray<string> systemAssemblyPaths )
        {
            lock ( this._sync )
            {
                this._assemblyPathsByName = this._assemblyPathsByName.AddRange(
                    systemAssemblyPaths,
                    Path.GetFileNameWithoutExtension,
                    x => (x, new Lazy<AssemblyName>( () => MetadataReferenceCache.GetAssemblyName( x ) )) );
            }
        }

        public bool IsCollectible( Assembly assembly )
            => this._assemblyLoader?.IsCollectible( assembly ) ?? throw new ObjectDisposedException( this.ToString() );

        /// <summary>
        /// Determines whether this domain is compatible with the given assembly paths, i.e. whether loading these
        /// assemblies would not cause a conflict with already-loaded assemblies. A conflict occurs when an assembly
        /// with the same simple name but a different version or public key token is already loaded.
        /// </summary>
        public bool IsCompatibleWithAssemblies( IEnumerable<string> assemblyPaths )
        {
            foreach ( var path in assemblyPaths )
            {
                AssemblyName assemblyName;

                try
                {
                    assemblyName = MetadataReferenceCache.GetAssemblyName( path );
                }
                catch ( Exception e ) when ( e is FileNotFoundException or BadImageFormatException or IOException )
                {
                    // If we cannot read the assembly metadata, treat the domain as compatible and let the
                    // normal extension loader report a diagnostic when it attempts to load the assembly.
                    this._logger.Trace?.Log( $"Domain {this._domainId}: cannot read assembly metadata for '{path}': {e.Message}. Treating as compatible." );

                    continue;
                }

                if ( assemblyName.Name != null
                     && this._assembliesByName.TryGetValue( assemblyName.Name, out var existingEntry ) )
                {
                    var existingName = existingEntry.Assembly.GetName();

                    if ( existingName.Version != assemblyName.Version
                         || !(existingName.GetPublicKeyToken() ?? []).SequenceEqual( assemblyName.GetPublicKeyToken() ?? [] ) )
                    {
                        this._logger.Trace?.Log(
                            $"Domain {this._domainId} is incompatible with assembly '{assemblyName}' at '{path}': " +
                            $"already-loaded assembly has identity '{existingEntry.Identity}'." );

                        return false;
                    }
                }
            }

            return true;
        }
    }
}