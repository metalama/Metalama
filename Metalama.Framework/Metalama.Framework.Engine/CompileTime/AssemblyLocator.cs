// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Framework.Engine.Collections;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// An implementation of <see cref="IAssemblyLocator"/> that looks in metadata references of a <see cref="Compilation"/>.
    /// </summary>
    [ExcludeFromCodeCoverage] // Not used in tests.
    internal sealed class AssemblyLocator : IAssemblyLocator
    {
        private const string _unknownAssemblyName = "*";

        private readonly ImmutableDictionaryOfArray<string, PortableExecutableReference> _referencesByName;
        private readonly ILogger _logger;

        public AssemblyLocator( IServiceProvider serviceProvider, IEnumerable<PortableExecutableReference> references )
        {
            this._referencesByName = references.ToMultiValueDictionary(
                x => GetAssemblyShortName( x ) ?? _unknownAssemblyName,
                x => x,
                StringComparer.OrdinalIgnoreCase );

            this._logger = serviceProvider.GetLoggerFactory().GetLogger( "AssemblyLocator" );
        }

        private static string? GetAssemblyShortName( MetadataReference r )
            => r switch
            {
                PortableExecutableReference pe => Path.GetFileNameWithoutExtension( pe.FilePath ),
                CompilationReference c => c.Compilation.AssemblyName ?? "*",
                _ => null
            };

        private static AssemblyName? GetAssemblyName( MetadataReference r )
            => r switch
            {
                PortableExecutableReference { FilePath: { } } pe => MetadataReferenceCache.GetAssemblyName( pe.FilePath ),
                CompilationReference c => new AssemblyName( c.Compilation.Assembly.Identity.ToString() ),
                _ => null
            };

        public bool TryFindAssembly( AssemblyIdentity assemblyIdentity, [NotNullWhen( true )] out MetadataReference? reference )
        {
            var assemblyName = new AssemblyName( assemblyIdentity.ToString() );

            this._logger.Trace?.Log( $"Finding the location of '{assemblyIdentity}'." );

            var referencesOfRequestedName = this._referencesByName[assemblyIdentity.Name]
                .Concat( this._referencesByName[_unknownAssemblyName] );

            var candidates = referencesOfRequestedName
                .SelectAsReadOnlyList( metadataReference => (MetadataReference: metadataReference, AssemblyName: GetAssemblyName( metadataReference )) )
                .Where( x => x.AssemblyName != null && AssemblyName.ReferenceMatchesDefinition( x.AssemblyName, assemblyName ) )
                .ToOrderedList( x => x.AssemblyName!.Version, descending: true );

            this._logger.Trace?.Log(
                $"Found {candidates.Count} candidates: {string.Join( ", ", candidates.SelectAsImmutableArray( x => (object) x.MetadataReference ) )}." );

            if ( candidates.Count == 0 )
            {
                this._logger.Error?.Log(
                    $"Could not find '{assemblyIdentity}'. The following references were found but did not match the required reference '{assemblyIdentity}': {referencesOfRequestedName.SelectAsImmutableArray( x => $"'{x.Display}'" )}." );

                reference = null;

                return false;
            }
            else
            {
                reference = candidates[0].MetadataReference;

                this._logger.Trace?.Log( $"Selecting '{reference}'." );

                return true;
            }
        }
    }
}